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

	static ParsedRow Row(string name, string set, string collectorNumber, string? language = null) =>
		new(new PhysicalMtgCard
		{
			Count = 1,
			Language = language,
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

	// Real Dragon Shield decorations: "Morph Creature" (TKTK #11) and "Beast Token (4/4)" (TMH2 #9; " Token" is stripped before validation).
	[Theory]
	[InlineData("TKTK", "11", " Creature")]
	[InlineData("TMH2", "9", " (4/4)")]
	public async Task DecoratedName_ExtendingCanonical_IsAcceptedAndCanonicalized(string set, string number, string decoration)
	{
		var canonical = fixture.Catalog.FindBySetAndCollectorNumber(set, number)!.Name;
		var (kept, issues) = await Validate(Row(canonical + decoration, set, number));

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

	// The catalog is English-only, so a localized name can never match; the resolved (Set, #) identifies the printing.
	[Fact]
	public async Task NonEnglishRow_NameMismatch_IsKeptWithEnglishName_AndWarning()
	{
		var canonical = fixture.Catalog.FindBySetAndCollectorNumber("M11", "149")!.Name;
		var (kept, issues) = await Validate(Row("Fulmine", "M11", "149", language: "it"));

		kept.Should().ContainSingle().Which.Card.Printing.Name.Should().Be(canonical);
		issues.Should().ContainSingle().Which.Severity.Should().Be(IssueSeverity.Warning);
	}

	[Fact]
	public async Task EnglishRow_NameMismatch_IsStillDropped()
	{
		var (kept, issues) = await Validate(Row("Fulmine", "M11", "149", language: "en"));

		kept.Should().BeEmpty();
		issues.Should().ContainSingle().Which.Severity.Should().Be(IssueSeverity.Error);
	}

	// MB1 was folded into The List (MB1 #62 → plst #AER-13), so the row is rewritten to the successor printing, not dropped.
	[Fact]
	public async Task StaleSetCode_RewrittenToSuccessorPrinting_WithWarning()
	{
		var (kept, issues) = await Validate(Row("Countless Gears Renegade", "MB1", "62"));

		var printing = kept.Should().ContainSingle().Which.Card.Printing;
		printing.Set.Should().Be("PLST", because: "MB1 is a retired alias for The List");
		printing.CollectorNumber.Should().Be("AER-13");
		printing.Id.Should().NotBe(Guid.Empty, because: "the stale-rewrite path backfills the resolved printing's Scryfall id");
		issues.Should().ContainSingle().Which.Severity.Should().Be(IssueSeverity.Warning);
	}

	[Fact]
	public async Task UnresolvableBySetNumberAndName_StillErrors()
	{
		var (kept, issues) = await Validate(Row("Nonexistent Phantasm Qzx", "MB1", "62"));

		kept.Should().BeEmpty();
		issues.Should().ContainSingle().Which.Severity.Should().Be(IssueSeverity.Error);
	}
}
