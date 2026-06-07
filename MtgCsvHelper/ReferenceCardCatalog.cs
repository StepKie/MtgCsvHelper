using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MtgCsvHelper;

/// <summary>
/// In-memory catalog built from a JSON bundle of <see cref="ReferenceCard"/>s. The bundle ships
/// with the app (gzipped under wwwroot for Blazor, alongside the exe for Console). Construction
/// loads everything into memory and builds secondary indexes; lookups are then dictionary reads.
/// </summary>
public sealed class ReferenceCardCatalog : IReferenceCardCatalog
{
	// "Double-faced" detection matches the existing CachedMtgApi behavior exactly: any printing
	// whose Scryfall `name` contains " // ". This catches transform, modal_dfc, split, meld
	// pairs, and double-faced tokens uniformly, while correctly EXCLUDING adventures (which
	// only carry the creature-face name in `name`) and other layouts where the back-face name
	// is hidden in `card_faces` rather than the top-level `name`.
	const string DoubleFacedSeparator = " // ";

	static readonly HashSet<string> TokenLayouts = new(StringComparer.OrdinalIgnoreCase)
	{
		"token", "double_faced_token", "emblem",
	};

	readonly IReadOnlyList<ReferenceCard> _all;
	readonly Dictionary<Guid, ReferenceCard> _byId;
	readonly Dictionary<int, ReferenceCard> _byCardmarketId;
	readonly Dictionary<(string Set, string CollectorNumber), ReferenceCard> _bySetAndCollector;
	readonly Dictionary<string, string> _sets;
	readonly Dictionary<string, string> _setCodeByName;     // "Innistrad" -> "ISD"
	readonly Dictionary<string, string> _setCodeByMtgoCode; // "MI" -> "MIR"
	readonly Dictionary<string, string> _frontFaceToFull;  // "Delver of Secrets" -> "Delver of Secrets // Insectile Aberration"
	readonly Dictionary<string, string> _layoutByName;     // "Commit // Memory" -> "split"
	readonly HashSet<string> _tokenNames;

	public int Count => _all.Count;

	public ReferenceCardCatalog(IReadOnlyList<ReferenceCard> cards)
	{
		_all = cards;

		_byId = new(cards.Count);
		_byCardmarketId = [];
		_bySetAndCollector = new(cards.Count);
		_sets = [];
		_setCodeByName = new(StringComparer.OrdinalIgnoreCase);  // human-typed names: stay forgiving
		_setCodeByMtgoCode = [];
		_frontFaceToFull = new(StringComparer.OrdinalIgnoreCase);
		_layoutByName = new(StringComparer.OrdinalIgnoreCase);
		_tokenNames = new(StringComparer.OrdinalIgnoreCase);

		// Set codes are uppercase by construction (normalized in ReferenceCard.CreateFromScryfall),
		// so the set-code dicts can use default comparers — no extra ToUpper/IgnoreCase needed.
		foreach (var c in cards)
		{
			_byId[c.Id] = c;
			if (c.CardmarketId is int cmid) { _byCardmarketId.TryAdd(cmid, c); }
			// First-write-wins on duplicate (set, collector_number); Scryfall keeps these unique
			// per English printing in default_cards but TryAdd makes the intent explicit.
			_bySetAndCollector.TryAdd((c.Set, c.CollectorNumber), c);
			_sets.TryAdd(c.Set, c.SetName);
			_setCodeByName.TryAdd(c.SetName, c.Set);
			if (!string.IsNullOrEmpty(c.MtgoCode)) { _setCodeByMtgoCode.TryAdd(c.MtgoCode, c.Set); }

			bool isToken = TokenLayouts.Contains(c.Layout);
			if (isToken) { _tokenNames.Add(c.Name); }

			// First-write-wins: every printing of the same name shares a layout in Scryfall.
			_layoutByName.TryAdd(c.Name, c.Layout);

			// Front-face → full-name indexing intentionally skips token/emblem layouts.
			// Tokens like "Bolt // Bolt" would otherwise shadow real transform pairs.
			if (!isToken && c.Name.Contains(DoubleFacedSeparator, StringComparison.Ordinal))
			{
				var split = c.Name.Split(DoubleFacedSeparator, 2);
				// Skip self-pairs ("X // X" — Scryfall's `reversible_card` layout): same oracle on
				// both faces, so the front face alone can't disambiguate.
				if (split.Length == 2 && !split[0].Equals(split[1], StringComparison.OrdinalIgnoreCase))
				{
					_frontFaceToFull.TryAdd(split[0], c.Name);
				}
			}
		}
	}

	public ReferenceCard? FindById(Guid scryfallId) => _byId.GetValueOrDefault(scryfallId);
	public ReferenceCard? FindByCardmarketId(int cardmarketId) => _byCardmarketId.GetValueOrDefault(cardmarketId);
	// Callers pass set codes as-typed (Moxfield uppercase, Topdecked lowercase, MTGO 2-letter…).
	// Storage is canonical uppercase, so input is normalized once at the entry point.
	public ReferenceCard? FindBySetAndCollectorNumber(string setCode, string collectorNumber)
	{
		var canonical = Canonicalize(setCode);
		return _bySetAndCollector.GetValueOrDefault((canonical, collectorNumber));
	}

	public IReadOnlyDictionary<string, string> GetSets() => _sets;
	public string? GetSetNameByCode(string setCode) => _sets.GetValueOrDefault(Canonicalize(setCode));

	// Returns the canonical Scryfall set code for an input that may be: an MTGO 2-letter code
	// (MI → MIR), a different-cased canonical code (mir → MIR), or already canonical (MIR → MIR).
	// Unknown codes pass through as-uppercased (lookups against them will simply miss).
	string Canonicalize(string setCode)
	{
		var upper = setCode.ToUpperInvariant();
		return _setCodeByMtgoCode.TryGetValue(upper, out var aliased) ? aliased : upper;
	}
	public string? GetSetCodeByName(string setName) => _setCodeByName.GetValueOrDefault(setName);
	public string? ExpandFrontFaceToFullName(string frontFaceName) => _frontFaceToFull.GetValueOrDefault(frontFaceName);
	public string? GetLayoutByName(string name) => _layoutByName.GetValueOrDefault(name);
	public bool IsTokenName(string name) => _tokenNames.Contains(name);

	/// <summary> Loads a gzip-compressed JSON bundle of <see cref="ReferenceCard"/>s. </summary>
	public static async Task<ReferenceCardCatalog> LoadGzipAsync(Stream gzipStream, CancellationToken ct = default)
	{
		using var gzip = new GZipStream(gzipStream, CompressionMode.Decompress);
		var cards = await JsonSerializer.DeserializeAsync<List<ReferenceCard>>(gzip, BundleSerializerOptions, ct)
			?? throw new InvalidDataException("Reference-card bundle is empty or malformed.");
		return new ReferenceCardCatalog(cards);
	}

	/// <summary> JSON options used both by the loader (here) and the bundle generator. </summary>
	public static readonly JsonSerializerOptions BundleSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
		// Enums (CardRarity) are stored as Scryfall's lowercase strings so the bundle stays greppable.
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) },
	};
}
