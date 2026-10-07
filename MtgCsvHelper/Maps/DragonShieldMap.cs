using CsvHelper.Configuration;
using MtgCsvHelper.Converters;
using MtgCsvHelper.Models;

namespace MtgCsvHelper.Maps;

/// <summary>
/// Bidirectional map for the DRAGONSHIELD format and its successor app's SORTED format, which inherited
/// DragonShield's set codes and edition names. Inherits the standard <see cref="PhysicalCardMap"/>
/// shape and customizes the set and collector-number columns via the Configure* hooks:
/// <list type="bullet">
///   <item><b>Read</b>: the <c>GK1_*/GK2_*</c> set codes DragonShield exports for Ravnica Guild Kits collapse
///   to canonical gk1/gk2 via <see cref="DragonShieldCodeReadConverter"/>.</item>
///   <item><b>Write</b>: DragonShield resolves imports by Set <em>Name</em>, not Set Code, so guild-kit cards
///   emit the native <c>Guild Kit: &lt;Guild&gt;</c> edition (e.g. <c>Guild Kit: Azorius</c>) rather than the
///   canonical <c>RNA Guild Kit</c>, which it doesn't recognize. The (set, collector#) → edition table lives
///   in <c>dragonshield-guildkit-editions.json</c>; cards not in it keep their canonical set name.</item>
///   <item><b>Write</b>: The List printings (<c>PLST DDC-49</c>) are emitted as their original printing
///   (<c>DDC #49</c>): Sorted rejects The List numbers, and DragonShield unwinds them on import anyway.</item>
/// </list>
/// The resource file is emitted by <c>tools/MtgCsvHelper.RefreshReferenceData -- dragonshield-guildkit</c>.
/// </summary>
public sealed class DragonShieldMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> GuildKitEditions = EmbeddedResources.LoadStringMap("dragonshield-guildkit-editions.json");

	readonly IReferenceCardCatalog _catalog;

	public DragonShieldMap(FormatConfig cfg, IReferenceCardCatalog catalog) : base(cfg, catalog) => _catalog = catalog;

	/// <summary>Read side: collapse the proprietary <c>GK1_*/GK2_*</c> set codes to canonical gk1/gk2. Write side: unwind The List.</summary>
	protected override void ConfigureSetCode(MemberMap<PhysicalMtgCard, string> map) =>
		map.TypeConverter(new DragonShieldCodeReadConverter())
			.Convert(args => OriginalOfTheList(args.Value)?.Set ?? args.Value.Printing.Set ?? string.Empty);

	/// <summary>Write side: the guild-kit edition or The List's original set name; else the canonical set name.</summary>
	protected override void ConfigureSetName(MemberMap<PhysicalMtgCard, string> map) =>
		map.Convert(args => ToEdition(args.Value));

	/// <summary>Write side: unwind The List to the original printing's collector number.</summary>
	protected override void ConfigureCollectorNumber(MemberMap<PhysicalMtgCard, string> map) =>
		map.Convert(args => OriginalOfTheList(args.Value)?.CollectorNumber ?? args.Value.Printing.CollectorNumber ?? string.Empty);

	/// <summary>gk1/gk2 cards get DragonShield's per-guild <c>Guild Kit: &lt;Guild&gt;</c> edition, The List cards their original set's name; everything else keeps its set name.</summary>
	string ToEdition(PhysicalMtgCard card)
	{
		var set = card.Printing.Set;
		if (set is not null && card.Printing.CollectorNumber is not null
			&& GuildKitEditions.TryGetValue($"{set}/{card.Printing.CollectorNumber}", out var edition))
		{
			return edition;
		}

		return OriginalOfTheList(card)?.SetName ?? card.Printing.SetName ?? string.Empty;
	}

	/// <summary>
	/// The original printing a The List card reprints, encoded in its collector number (<c>DDC-49</c> → <c>DDC #49</c>).
	/// Null for other sets, and when the catalog has no same-named card at that coordinate.
	/// </summary>
	ReferenceCard? OriginalOfTheList(PhysicalMtgCard card) =>
		card.Printing.Set is "PLST"
			&& card.Printing.CollectorNumber?.Split('-', 2) is [var set, var number]
			&& _catalog.FindBySetAndCollectorNumber(set, number) is { } original
			&& original.Name == card.Printing.Name
				? original
				: null;
}
