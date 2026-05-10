using System.IO.Compression;
using System.Text.Json;

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
	readonly Dictionary<string, string> _setCodeByName;     // "Innistrad" -> "ISD" (uppercase)
	readonly HashSet<string> _doubleFacedNames;
	readonly Dictionary<string, string> _frontFaceToFull;  // "Delver of Secrets" -> "Delver of Secrets // Insectile Aberration"
	readonly HashSet<string> _tokenNames;

	public int Count => _all.Count;

	public ReferenceCardCatalog(IReadOnlyList<ReferenceCard> cards)
	{
		_all = cards;

		_byId = new(cards.Count);
		_byCardmarketId = [];
		_bySetAndCollector = new(cards.Count);
		_sets = new(StringComparer.OrdinalIgnoreCase);
		_setCodeByName = new(StringComparer.OrdinalIgnoreCase);
		_doubleFacedNames = new(StringComparer.OrdinalIgnoreCase);
		_frontFaceToFull = new(StringComparer.OrdinalIgnoreCase);
		_tokenNames = new(StringComparer.OrdinalIgnoreCase);

		foreach (var c in cards)
		{
			_byId[c.Id] = c;
			if (c.CardmarketId is int cmid) { _byCardmarketId.TryAdd(cmid, c); }
			// First-write-wins on duplicate (set, collector_number); Scryfall keeps these unique
			// per English printing in default_cards but TryAdd makes the intent explicit.
			_bySetAndCollector.TryAdd((c.Set, c.CollectorNumber), c);
			// Both set indexes use uppercase keys so GetSets()/GetSetNameByCode/GetSetCodeByName
			// are consistent — Scryfall's raw set codes are lowercase (e.g. "isd"), but our
			// public contract is to expose them uppercase to match historical convention.
			_sets.TryAdd(c.Set.ToUpperInvariant(), c.SetName);
			_setCodeByName.TryAdd(c.SetName, c.Set.ToUpperInvariant());

			bool isToken = TokenLayouts.Contains(c.Layout);
			if (isToken) { _tokenNames.Add(c.Name); }

			// DFC indexing intentionally skips token/emblem layouts. Tokens like "Bolt // Bolt"
			// would otherwise pollute the front-face map and shadow real transform pairs because
			// of TryAdd's first-write-wins semantics.
			if (!isToken && c.Name.Contains(DoubleFacedSeparator, StringComparison.Ordinal))
			{
				_doubleFacedNames.Add(c.Name);
				var split = c.Name.Split(DoubleFacedSeparator, 2);
				// Skip self-pairs ("X // X" — Scryfall's `reversible_card` layout, where both
				// faces share the same oracle name). They can't disambiguate a front-face lookup.
				// Case-insensitive match catches hypothetical "Forest // forest" variants too —
				// defensive cost: zero.
				if (split.Length == 2 && !split[0].Equals(split[1], StringComparison.OrdinalIgnoreCase))
				{
					_frontFaceToFull.TryAdd(split[0], c.Name);
				}
			}
		}
	}

	public ReferenceCard? FindById(Guid scryfallId) => _byId.GetValueOrDefault(scryfallId);
	public ReferenceCard? FindByCardmarketId(int cardmarketId) => _byCardmarketId.GetValueOrDefault(cardmarketId);
	public ReferenceCard? FindBySetAndCollectorNumber(string setCode, string collectorNumber) =>
		_bySetAndCollector.GetValueOrDefault((setCode, collectorNumber));

	public IReadOnlyDictionary<string, string> GetSets() => _sets;
	public string? GetSetNameByCode(string setCode) => _sets.GetValueOrDefault(setCode);
	public string? GetSetCodeByName(string setName) => _setCodeByName.GetValueOrDefault(setName);
	public bool IsDoubleFacedName(string name) => _doubleFacedNames.Contains(name);
	public string? ExpandFrontFaceToFullName(string frontFaceName) => _frontFaceToFull.GetValueOrDefault(frontFaceName);
	public bool IsTokenName(string name) => _tokenNames.Contains(name);

	/// <summary> Loads a gzip-compressed JSON bundle of <see cref="ReferenceCard"/>s. </summary>
	public static async Task<ReferenceCardCatalog> LoadGzipAsync(Stream gzipStream)
	{
		using var gzip = new GZipStream(gzipStream, CompressionMode.Decompress);
		var cards = await JsonSerializer.DeserializeAsync<List<ReferenceCard>>(gzip, BundleSerializerOptions)
			?? throw new InvalidDataException("Reference-card bundle is empty or malformed.");
		return new ReferenceCardCatalog(cards);
	}

	/// <summary> JSON options used both by the loader (here) and the bundle generator. </summary>
	public static readonly JsonSerializerOptions BundleSerializerOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
		PropertyNameCaseInsensitive = true,
	};
}
