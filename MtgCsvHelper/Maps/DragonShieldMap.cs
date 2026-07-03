using CsvHelper.Configuration;
using MtgCsvHelper.Converters;
using MtgCsvHelper.Models;

namespace MtgCsvHelper.Maps;

/// <summary>
/// Bidirectional map for the DRAGONSHIELD format. Inherits the standard <see cref="PhysicalCardMap"/>
/// shape and customizes two columns via the Configure* hooks, for Ravnica Guild Kits (which DragonShield
/// splits into per-guild editions):
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

	public DragonShieldMap(FormatConfig cfg, IReferenceCardCatalog catalog) : base(cfg, catalog) { }

	/// <summary>Read side: collapse the proprietary <c>GK1_*/GK2_*</c> set codes to canonical gk1/gk2.</summary>
	protected override void ConfigureSetCode(MemberMap<PhysicalMtgCard, string> map) =>
		map.TypeConverter(new DragonShieldCodeReadConverter());

	/// <summary>Write side: emit the native per-guild <c>Guild Kit: &lt;Guild&gt;</c> edition for gk1/gk2 cards; else the canonical set name.</summary>
	protected override void ConfigureSetName(MemberMap<PhysicalMtgCard, string> map) =>
		map.Convert(args => ToGuildKitEdition(args.Value));

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
