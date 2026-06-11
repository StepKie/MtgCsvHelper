using MtgCsvHelper.Converters;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Maps;

// Bidirectional map for the DECKBOX format. Inherits the standard PhysicalCardMap shape, then
// patches the two columns where Deckbox diverges from Scryfall:
//
//   - WRITE: Edition (SetName) — Deckbox curates its own edition names ("Magic 2014 Core Set"
//     vs Scryfall's "Magic 2014", "Promo Pack: Kamigawa: Neon Dynasty" vs "Kamigawa: Neon
//     Dynasty Promos", etc.). The mismatches live in `deckbox-set-aliases.json`; sets without
//     an entry fall back to Scryfall canonical (the ~95% of editions where they agree).
//
//   - READ:  Edition Code (Set)  — Deckbox uses internal codes like `ex_127` or `pp_neo` that
//     don't resolve in the Scryfall catalog. `deckbox-code-aliases.json` translates them back
//     to Scryfall codes (TMH2, PNEO, …) before CatalogValidator runs.
//
// Both resource files are emitted by `tools/MtgCsvHelper.RefreshReferenceData -- deckbox-aliases`.
public class DeckboxMap : PhysicalCardMap
{
	internal static readonly IReadOnlyDictionary<string, string> SetCodeToDeckboxName = EmbeddedResources.LoadStringMap("deckbox-set-aliases.json");
	internal static readonly IReadOnlyDictionary<string, string> DeckboxCodeToScryfallCode = EmbeddedResources.LoadStringMap("deckbox-code-aliases.json");

	public DeckboxMap(FormatConfig cfg, IReferenceCardCatalog catalog) : base(cfg, catalog)
	{
		// We reach into CsvHelper's `ReferenceMaps.Data.Mapping.MemberMaps` to swap two member
		// mappings after the base ctor has registered the defaults. This relies on those
		// collections being publicly mutable — they are in CsvHelper 33, but a major upgrade
		// could change the API. The alternative — duplicating all PhysicalCardMap mappings
		// here (like CardKingdomWriteMap does) — drifts on every change to the base map.
		// Tracked in #88: extract `protected virtual ConfigureSetName/SetCode` hooks in
		// PhysicalCardMap to lift this out of CsvHelper internals.
		var printingRef = ReferenceMaps.FirstOrDefault(r => r.Data.Member?.Name == nameof(PhysicalMtgCard.Printing))
			?? throw new InvalidOperationException($"Expected `Printing` ReferenceMap from base PhysicalCardMap for '{cfg.Name}'.");

		// SetName (Edition column) — replace the default member access with a write-side Convert
		// that gets the whole PhysicalMtgCard, because the alias lookup is keyed by Set *code*.
		// Drops in via Optional() so reads of a missing column don't throw.
		if (cfg.SetName is not null)
		{
			var existing = printingRef.Data.Mapping.MemberMaps.FirstOrDefault(m => m.Data.Names.Contains(cfg.SetName))
				?? throw new InvalidOperationException($"Expected SetName MemberMap in Printing reference for '{cfg.Name}' (header '{cfg.SetName}').");
			printingRef.Data.Mapping.MemberMaps.Remove(existing);

			Map(c => c.Printing.SetName).Name(cfg.SetName).Optional()
				.Convert(args =>
				{
					var card = args.Value;
					// Set can be null on rows that escaped SetInfoEnricher without a catalog hit
					// (no Edition + no recognizable Edition Code). Dictionary<string,string> with
					// OrdinalIgnoreCase throws ArgumentNullException on a null key, so guard before
					// the lookup and fall straight to SetName for those rows.
					return card.Printing.Set is not null
						&& SetCodeToDeckboxName.TryGetValue(card.Printing.Set, out var alias)
						? alias
						: card.Printing.SetName;
				});
		}

		// SetCode (Edition Code column) — wrap the read side so Deckbox-internal codes like
		// `ex_127` get translated to Scryfall codes (TMH2) before CatalogValidator looks them up.
		// Write side is identity: we already store Scryfall codes in card.Printing.Set, and
		// Scryfall codes resolve fine on Deckbox import.
		if (cfg.SetCode is not null)
		{
			var existing = printingRef.Data.Mapping.MemberMaps.FirstOrDefault(m => m.Data.Names.Contains(cfg.SetCode))
				?? throw new InvalidOperationException($"Expected SetCode MemberMap in Printing reference for '{cfg.Name}' (header '{cfg.SetCode}').");
			printingRef.Data.Mapping.MemberMaps.Remove(existing);

			Map(c => c.Printing.Set).Name(cfg.SetCode).Optional()
				.TypeConverter(new DeckboxCodeReadConverter(DeckboxCodeToScryfallCode));
		}
	}
}
