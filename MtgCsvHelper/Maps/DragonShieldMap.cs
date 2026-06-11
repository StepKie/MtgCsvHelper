using System.Text.Json;
using MtgCsvHelper.Converters;
using MtgCsvHelper.Models;

namespace MtgCsvHelper.Maps;

// Bidirectional map for the DRAGONSHIELD format. Inherits the standard PhysicalCardMap shape, then
// patches the Set Code column where Dragon Shield diverges from Scryfall for Ravnica Guild Kits:
//
//   - READ:  GK1_*/GK2_* guild-kit codes collapse to the canonical gk1/gk2 (DragonShieldCodeReadConverter).
//   - WRITE: gk1/gk2 cards re-emit Dragon Shield's proprietary GK<n>_<GUILD> codes. Dragon Shield's
//     CSV importer ignores canonical codes and name-matches reprints onto the wrong edition; the
//     native codes resolve correctly. The (set, collector#) -> native-code table lives in
//     `dragonshield-guildkit-codes.json` (generated from Scryfall watermarks); cards not in it
//     fall back to the canonical Scryfall code.
//
// The resource file is emitted by `tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit`.
public sealed class DragonShieldMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> GuildKitCodes = LoadEmbedded("dragonshield-guildkit-codes.json");

	public DragonShieldMap(FormatConfig cfg, IReferenceCardCatalog catalog)
		: base(cfg, catalog, new DragonShieldCodeReadConverter())
	{
		// Keep the read converter, add a whole-card write Convert — a per-cell converter can't see the collector number the lookup needs (same surgery as DeckboxMap).
		if (cfg.SetCode is null) { return; }

		var printingRef = ReferenceMaps.FirstOrDefault(r => r.Data.Member?.Name == nameof(PhysicalMtgCard.Printing))
			?? throw new InvalidOperationException($"Expected `Printing` ReferenceMap from base PhysicalCardMap for '{cfg.Name}'.");
		var existing = printingRef.Data.Mapping.MemberMaps.FirstOrDefault(m => m.Data.Names.Contains(cfg.SetCode))
			?? throw new InvalidOperationException($"Expected Set Code MemberMap in Printing reference for '{cfg.Name}' (header '{cfg.SetCode}').");
		printingRef.Data.Mapping.MemberMaps.Remove(existing);

		Map(c => c.Printing.Set).Name(cfg.SetCode).Index(4).Optional()
			.TypeConverter(new DragonShieldCodeReadConverter())
			.Convert(args => ToNativeSetCode(args.Value));
	}

	// gk1/gk2 cards with a known guild get Dragon Shield's native code; everything else is identity.
	static string ToNativeSetCode(PhysicalMtgCard card)
	{
		var set = card.Printing.Set;
		return set is not null && card.Printing.CollectorNumber is not null
			&& GuildKitCodes.TryGetValue($"{set}/{card.Printing.CollectorNumber}", out var native)
			? native
			: set ?? string.Empty;
	}

	static IReadOnlyDictionary<string, string> LoadEmbedded(string fileName)
	{
		var resourceName = $"MtgCsvHelper.Resources.{fileName}";
		using var stream = typeof(DragonShieldMap).Assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
		var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
			?? throw new InvalidOperationException($"Failed to deserialize '{resourceName}'.");
		return new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
	}
}
