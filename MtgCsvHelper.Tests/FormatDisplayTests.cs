namespace MtgCsvHelper.Tests;

public class FormatDisplayTests
{
	[Fact]
	public void DisplayNames_HasEntryForEverySupportedFormat()
	{
		// Regression guard for the ARCHIDEKT label bug in 1.3.0: ARCHIDEKT was added to
		// CardMapFactory.Supported but no entry was added here, so the UI fell back to the
		// raw all-caps identifier.
		CardMapFactory.Supported.Should().AllSatisfy(fmt =>
			FormatDisplay.DisplayNames.Should().ContainKey(fmt,
				because: $"every supported format needs a display label; '{fmt}' is missing"));
	}

	[Fact]
	public void For_ReturnsDisplayName_ForKnownFormat()
	{
		FormatDisplay.For("MTGGOLDFISH").Should().Be("MTGGoldfish");
	}

	[Fact]
	public void For_FallsBackToIdentifier_ForUnknownFormat()
	{
		FormatDisplay.For("MADE_UP_FORMAT").Should().Be("MADE_UP_FORMAT");
	}
}
