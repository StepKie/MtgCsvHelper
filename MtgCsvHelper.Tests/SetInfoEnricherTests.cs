using MtgCsvHelper.Enrichment;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class SetInfoEnricherTests(CatalogFixture fixture)
{
	readonly SetInfoEnricher _enricher = new(fixture.Catalog);

	// Catalog always wins for SetName: a non-canonical import name (Deckbox "Extras: …") with a resolvable code gets the canonical name, so every writer emits the canonical form.
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

	// Companion boundary: an already-canonical SetName passes through as a no-op overwrite.
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

	// One issue per row: an unknown set code with a CSV-provided name must not warn here on top of CatalogValidator's downstream error.
	[Fact]
	public async Task EnrichAsync_UnknownSetCode_WithCsvProvidedSetName_DoesNotWarn()
	{
		var row = new ParsedRow(new PhysicalMtgCard
		{
			Count = 1,
			Printing = new Card
			{
				Name = "Countless Gears Renegade",
				Set = "MB1",                 // not in catalog (redirected to PLST upstream)
				CollectorNumber = "62",
				SetName = "Mystery Booster", // also not in catalog
			},
		}, RowNumber: 1);

		var issues = new List<ImportIssue>();
		await _enricher.EnrichAsync([row], issues, CancellationToken.None);

		issues.Should().BeEmpty(because: "CatalogValidator's downstream lookup already emits the precise error for this row");
	}
}
