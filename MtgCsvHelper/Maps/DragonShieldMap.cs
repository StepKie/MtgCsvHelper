using MtgCsvHelper.Converters;
using MtgCsvHelper.Models;

namespace MtgCsvHelper.Maps;

/// <summary>
/// Bidirectional map for the DRAGONSHIELD format. Inherits the standard <see cref="PhysicalCardMap"/>
/// shape, then patches the Set Code column where Dragon Shield diverges from Scryfall for Ravnica
/// Guild Kits:
/// <list type="bullet">
///   <item><b>Read</b>: <c>GK1_*/GK2_*</c> guild-kit codes collapse to canonical gk1/gk2 via <see cref="DragonShieldCodeReadConverter"/>.</item>
///   <item><b>Write</b>: gk1/gk2 cards re-emit Dragon Shield's proprietary <c>GK&lt;n&gt;_&lt;GUILD&gt;</c> codes
///   (its importer ignores canonical codes and name-matches reprints onto the wrong edition; the native
///   codes resolve correctly). The (set, collector#) → native-code table lives in
///   <c>dragonshield-guildkit-codes.json</c>; cards not in it fall back to their canonical Scryfall code.</item>
/// </list>
/// The resource file is emitted by <c>tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit</c>.
/// </summary>
public sealed class DragonShieldMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> GuildKitCodes = EmbeddedResources.LoadStringMap("dragonshield-guildkit-codes.json");

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
}
