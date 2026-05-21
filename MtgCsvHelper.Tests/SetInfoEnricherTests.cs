using MtgCsvHelper.Enrichment;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class SetInfoEnricherTests(CatalogFixture fixture)
{
	readonly SetInfoEnricher _enricher = new(fixture.Catalog);

	// Guards the "catalog always wins for SetName" invariant relied on by the Deckbox round-trip
	// path: when a row imports with a non-canonical SetName (e.g. Deckbox's "Extras: Modern
	// Horizons 2") but a resolvable Set code, the catalog's canonical name takes precedence.
	// Required so downstream writers (Moxfield, Manabox, …) emit the canonical form regardless of
	// which CSV the cards came from.
	[Fact]
	public async Task EnrichAsync_NonCanonicalSetName_OverwrittenByCatalog()
	{
		var row = new ParsedRow(new PhysicalMtgCard
		{
			Count = 1,
			Printing = new Card
			{
				Name = "Crimson Vow Token",
				Set = "VOC",
				CollectorNumber = "1",
				SetName = "Innistrad: Crimson Vow Commander", // Deckbox's curated name
			},
		}, RowNumber: 2);

		await _enricher.EnrichAsync([row], [], CancellationToken.None);

		row.Card.Printing.SetName.Should().Be("Crimson Vow Commander",
			because: "the catalog's Scryfall-canonical name must always win over the CSV's value when the set code resolves");
	}

	// Companion to the test above: when the CSV already carries the Scryfall canonical name
	// (Moxfield / Manabox / Archidekt / TopDecked all emit canonical names), the catalog
	// overwrite is a no-op. Documents the intended boundary so the "catalog always wins" rule
	// can't silently degrade into "catalog overwrites with garbage when name lookup misfires".
	[Fact]
	public async Task EnrichAsync_AlreadyCanonicalSetName_PassesThroughUnchanged()
	{
		var row = new ParsedRow(new PhysicalMtgCard
		{
			Count = 1,
			Printing = new Card
			{
				Name = "Ambitious Farmhand // Seasoned Cathar",
				Set = "MID",
				CollectorNumber = "2",
				SetName = "Innistrad: Midnight Hunt",
			},
		}, RowNumber: 2);

		await _enricher.EnrichAsync([row], [], CancellationToken.None);

		row.Card.Printing.SetName.Should().Be("Innistrad: Midnight Hunt",
			because: "a row that already carries the Scryfall canonical name must come out unchanged");
	}
}
