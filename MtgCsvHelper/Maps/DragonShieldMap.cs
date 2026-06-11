using MtgCsvHelper.Converters;
using MtgCsvHelper.Models;

namespace MtgCsvHelper.Maps;

/// <summary>
/// Bidirectional map for the DRAGONSHIELD format. Inherits the standard <see cref="PhysicalCardMap"/>
/// shape, then handles Ravnica Guild Kits, which DragonShield splits into per-guild editions:
/// <list type="bullet">
///   <item><b>Read</b>: the <c>GK1_*/GK2_*</c> set codes DragonShield exports collapse to canonical gk1/gk2 via <see cref="DragonShieldCodeReadConverter"/>.</item>
///   <item><b>Write</b>: DragonShield resolves imports by Set <em>Name</em>, not Set Code, so guild-kit cards
///   emit the native <c>Guild Kit: &lt;Guild&gt;</c> edition (e.g. <c>Guild Kit: Azorius</c>) rather than the
///   canonical <c>RNA Guild Kit</c>, which it doesn't recognize. The (set, collector#) → edition table lives
///   in <c>dragonshield-guildkit-editions.json</c>; cards not in it keep their canonical set name.</item>
/// </list>
/// The resource file is emitted by <c>tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit</c>.
/// </summary>
public sealed class DragonShieldMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> GuildKitEditions = EmbeddedResources.LoadStringMap("dragonshield-guildkit-editions.json");

	public DragonShieldMap(FormatConfig cfg, IReferenceCardCatalog catalog)
		: base(cfg, catalog, new DragonShieldCodeReadConverter())
	{
		// Patch Set Name with a whole-card write Convert — a per-cell converter can't see the collector number the guild lookup needs (same surgery as DeckboxMap).
		if (cfg.SetName is null) { return; }

		var printingRef = ReferenceMaps.FirstOrDefault(r => r.Data.Member?.Name == nameof(PhysicalMtgCard.Printing))
			?? throw new InvalidOperationException($"Expected `Printing` ReferenceMap from base PhysicalCardMap for '{cfg.Name}'.");
		var existing = printingRef.Data.Mapping.MemberMaps.FirstOrDefault(m => m.Data.Names.Contains(cfg.SetName))
			?? throw new InvalidOperationException($"Expected Set Name MemberMap in Printing reference for '{cfg.Name}' (header '{cfg.SetName}').");
		printingRef.Data.Mapping.MemberMaps.Remove(existing);

		Map(c => c.Printing.SetName).Name(cfg.SetName).Index(5).Optional()
			.Convert(args => ToGuildKitEdition(args.Value));
	}

	/// <summary>gk1/gk2 cards get DragonShield's per-guild <c>Guild Kit: &lt;Guild&gt;</c> edition; everything else keeps its set name.</summary>
	static string ToGuildKitEdition(PhysicalMtgCard card)
	{
		var set = card.Printing.Set;
		return set is not null && card.Printing.CollectorNumber is not null
			&& GuildKitEditions.TryGetValue($"{set}/{card.Printing.CollectorNumber}", out var edition)
			? edition
			: card.Printing.SetName ?? string.Empty;
	}
}
