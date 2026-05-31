using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Tests;

public class DragonShieldCodeReadConverterTests
{
	static readonly DragonShieldCodeReadConverter Converter = new();

	// The Guild Kit branch returns before dereferencing row/memberMapData; passthrough is covered end-to-end.
	static object? Convert(string text) => Converter.ConvertFromString(text, null!, null!);

	[Theory]
	[InlineData("GK1_DIMIR", "GK1")]
	[InlineData("GK2_AZORIU", "GK2")]
	[InlineData("GK2_RAKDOS", "GK2")]
	[InlineData("GK2_ORZHOV", "GK2")]
	[InlineData("gk2_simic", "GK2")]
	public void GuildKitCodes_CollapseToScryfallSetCode(string dragonShieldCode, string expected) =>
		Convert(dragonShieldCode).Should().Be(expected);
}
