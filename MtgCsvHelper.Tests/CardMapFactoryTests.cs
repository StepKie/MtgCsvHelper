namespace MtgCsvHelper.Tests;

public class CardMapFactoryTests
{
	[Fact]
	public void ReadableFormats_ExcludesWriteOnlyFormats()
	{
		CardMapFactory.ReadableFormats.Should().NotContain("CARDKINGDOM",
			because: "CARDKINGDOM is write-only — GenerateReadMap throws for it, so the UI must not offer it as an input");
	}

	[Fact]
	public void WritableFormats_ExcludesReadOnlyFormats()
	{
		CardMapFactory.WritableFormats.Should().NotContain("CARDMARKET",
			because: "CARDMARKET is read-only — GenerateWriteMap throws for it, so the UI must not offer it as an output");
	}

	[Fact]
	public void ReadableFormats_PreservesOrderOfSupported()
	{
		var supportedNoWriteOnly = CardMapFactory.Supported.Where(f => f != "CARDKINGDOM");
		CardMapFactory.ReadableFormats.Should().Equal(supportedNoWriteOnly);
	}

	[Fact]
	public void WritableFormats_PreservesOrderOfSupported()
	{
		var supportedNoReadOnly = CardMapFactory.Supported.Where(f => f != "CARDMARKET");
		CardMapFactory.WritableFormats.Should().Equal(supportedNoReadOnly);
	}
}
