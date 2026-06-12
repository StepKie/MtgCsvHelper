using CsvHelper.Configuration;
using MtgCsvHelper.Converters;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Maps;

/// <summary>
/// Bidirectional map for the DECKBOX format. Inherits the standard <see cref="PhysicalCardMap"/> shape
/// and customizes the two columns where Deckbox diverges from Scryfall, via the Configure* hooks:
/// <list type="bullet">
///   <item><b>Write</b> (Edition / SetName): Deckbox curates its own edition names ("Magic 2014 Core
///   Set" vs Scryfall's "Magic 2014", etc.) from <c>deckbox-set-aliases.json</c>; sets without an entry
///   fall back to the Scryfall canonical name.</item>
///   <item><b>Read</b> (Edition Code / Set): Deckbox-internal codes like <c>ex_127</c> / <c>pp_neo</c> are
///   translated back to Scryfall codes (<c>deckbox-code-aliases.json</c>) before CatalogValidator runs.</item>
/// </list>
/// Both resource files are emitted by <c>tools/MtgCsvHelper.RefreshReferenceData -- deckbox-aliases</c>.
/// </summary>
public class DeckboxMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> SetCodeToDeckboxName = EmbeddedResources.LoadStringMap("deckbox-set-aliases.json");
	internal static readonly IReadOnlyDictionary<string, string> DeckboxCodeToScryfallCode = EmbeddedResources.LoadStringMap("deckbox-code-aliases.json");

	public DeckboxMap(FormatConfig cfg, IReferenceCardCatalog catalog) : base(cfg, catalog) { }

	/// <summary>Read side: translate Deckbox-internal codes (ex_127, pp_neo) to Scryfall codes before validation.</summary>
	protected override void ConfigureSetCode(MemberMap<PhysicalMtgCard, string> map) =>
		map.TypeConverter(new DeckboxCodeReadConverter(DeckboxCodeToScryfallCode));

	/// <summary>Write side: emit Deckbox's curated edition name (keyed by Set code), else the canonical SetName.</summary>
	protected override void ConfigureSetName(MemberMap<PhysicalMtgCard, string> map) =>
		map.Convert(args =>
		{
			var card = args.Value;
			// Guard the OrdinalIgnoreCase lookup: Set is null on rows SetInfoEnricher couldn't resolve.
			return card.Printing.Set is not null && SetCodeToDeckboxName.TryGetValue(card.Printing.Set, out var alias)
				? alias
				: card.Printing.SetName;
		});
}
