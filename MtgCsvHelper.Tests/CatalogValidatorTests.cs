using MtgCsvHelper.Enrichment;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class CatalogValidatorTests(CatalogFixture fixture)
{
	readonly CatalogValidator _validator = new(fixture.Catalog);

	// TWOE #15's front face "Monster" is shared by several DFCs, so expanding it picks an arbitrary one.
	const string SharedFrontSet = "TWOE";
	const string SharedFrontNumber = "15";
	const string SharedFront = "Monster";

	static ParsedRow Row(string name, string set, string collectorNumber) =>
		new(new PhysicalMtgCard
		{
			Count = 1,
			Printing = new Card { Name = name, Set = set, CollectorNumber = collectorNumber, SetName = "" },
		}, RowNumber: 1);

	async Task<(List<ParsedRow> kept, List<ImportIssue> issues)> Validate(ParsedRow row)
	{
		List<ParsedRow> rows = [row];
		List<ImportIssue> issues = [];
		await _validator.EnrichAsync(rows, issues, CancellationToken.None);

		return (rows, issues);
	}

	[Fact]
	public async Task FrontFaceOnlyShortName_IsAcceptedAndCanonicalized()
	{
		var canonical = fixture.Catalog.FindBySetAndCollectorNumber(SharedFrontSet, SharedFrontNumber)!.Name;
		var (kept, issues) = await Validate(Row(SharedFront, SharedFrontSet, SharedFrontNumber));

		issues.Should().BeEmpty();
		kept.Should().ContainSingle().Which.Card.Printing.Name.Should().Be(canonical);
	}

	// A wrongly-expanded full name at a (Set, #) pinning a different printing must be accepted and corrected, not dropped.
	[Fact]
	public async Task WronglyExpandedSharedFrontFace_IsAcceptedAndCanonicalized()
	{
		var canonical = fixture.Catalog.FindBySetAndCollectorNumber(SharedFrontSet, SharedFrontNumber)!.Name;
		var arbitraryExpansion = fixture.Catalog.ExpandFrontFaceToFullName(SharedFront);
		arbitraryExpansion.Should().NotBeNullOrEmpty(because: "the shared front face must expand to some DFC full name");
		arbitraryExpansion.Should().NotBe(canonical,
			because: "the bug only reproduces when the arbitrary expansion differs from the printing pinned by (Set, #)");

		var (kept, issues) = await Validate(Row(arbitraryExpansion!, SharedFrontSet, SharedFrontNumber));

		issues.Should().BeEmpty();
		kept.Should().ContainSingle().Which.Card.Printing.Name.Should().Be(canonical);
	}

	[Fact]
	public async Task ExactCanonicalName_PassesThroughUnchanged()
	{
		var canonical = fixture.Catalog.FindBySetAndCollectorNumber(SharedFrontSet, SharedFrontNumber)!.Name;
		var (kept, issues) = await Validate(Row(canonical, SharedFrontSet, SharedFrontNumber));

		issues.Should().BeEmpty();
		kept.Should().ContainSingle().Which.Card.Printing.Name.Should().Be(canonical);
	}

	[Fact]
	public async Task ValidatedRow_GetsRarityBackfilledFromCatalog()
	{
		var expected = fixture.Catalog.FindBySetAndCollectorNumber(SharedFrontSet, SharedFrontNumber)!.Rarity;
		expected.Should().NotBe(CardRarity.Unknown, because: "the bundle carries rarity for every printing");

		var (kept, issues) = await Validate(Row(SharedFront, SharedFrontSet, SharedFrontNumber));

		issues.Should().BeEmpty();
		kept.Should().ContainSingle().Which.Card.Rarity.Should().Be(expected);
	}

	[Fact]
	public async Task WrongName_AtValidSetAndNumber_IsDropped()
	{
		var (kept, issues) = await Validate(Row("Lightning Bolt", SharedFrontSet, SharedFrontNumber));

		kept.Should().BeEmpty();
		issues.Should().ContainSingle().Which.Severity.Should().Be(IssueSeverity.Error);
	}
}
