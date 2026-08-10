using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Pins the null-collapse invariant for formats declaring Mint/Excellent as null: ambiguous reads
/// resolve to NearMint, and writes of Mint/Excellent emit the NearMint string. TCGPlayer's separate
/// collision (Excellent + Good → "Lightly Played") is out of scope — see CONVERSION_LIMITATIONS.md.
/// </summary>
public class CardConditionConverterTests(ITestOutputHelper output) : BaseTest(output)
{

	static CardConditionConverter ConverterFor(string? mint, string nearMint, string? excellent) =>
		new(new ConditionConfiguration(
			HeaderName: "Condition",
			Mint: mint,
			NearMint: nearMint,
			Excellent: excellent,
			Good: "Good",
			LightlyPlayed: "LP",
			Played: "Played",
			Poor: "Poor"));

	[Theory]
	[InlineData(null, "NM", null)]            // Archidekt — both null
	[InlineData("Mint", "Near Mint", null)]   // Moxfield / Deckbox shape — only Excellent null
	public void AmbiguousString_ResolvesToNearMint_NotMintOrExcellent(string? mint, string nearMint, string? excellent)
	{
		var converter = ConverterFor(mint, nearMint, excellent);

		var result = converter.ConvertFromString(nearMint, row: null!, memberMapData: null!) as CardCondition?;

		result.Should().Be(CardCondition.NearMint,
			"a config-level null on Mint/Excellent eliminates the duplicate-string match, so the NearMint arm wins regardless of switch order");
	}

	[Fact]
	public void WriteMint_WithNullMintConfig_FallsBackToNearMintString()
	{
		var converter = ConverterFor(mint: null, nearMint: "NM", excellent: null);

		var output = converter.ConvertToString(CardCondition.Mint, row: null!, memberMapData: null!);

		output.Should().Be("NM", "Archidekt has no Mint tier; internal Mint writes as the NearMint string");
	}

	[Fact]
	public void WriteExcellent_WithNullExcellentConfig_FallsBackToNearMintString()
	{
		var converter = ConverterFor(mint: "Mint", nearMint: "Near Mint", excellent: null);

		var output = converter.ConvertToString(CardCondition.Excellent, row: null!, memberMapData: null!);

		output.Should().Be("Near Mint", "Moxfield/Deckbox/TopDecked have no Excellent tier; internal Excellent writes as the NearMint string");
	}

	[Fact]
	public void WriteMint_WithDistinctMintConfig_EmitsMintString()
	{
		// DragonShield / Manabox shape — Mint has its own string, Excellent has its own string.
		var converter = ConverterFor(mint: "Mint", nearMint: "NearMint", excellent: "Excellent");

		var output = converter.ConvertToString(CardCondition.Mint, row: null!, memberMapData: null!);

		output.Should().Be("Mint", "formats with a distinct Mint tier keep the original mapping");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	[InlineData(null)]
	public void ReadBlank_ResolvesToUnknown(string? text)
	{
		var converter = ConverterFor(mint: "Mint", nearMint: "Near Mint", excellent: null);

		var result = converter.ConvertFromString(text, row: null!, memberMapData: null!) as CardCondition?;

		result.Should().Be(CardCondition.Unknown, "a blank cell carries no condition info and is not a vocabulary error");
	}
}
