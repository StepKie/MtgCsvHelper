using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Tests;

public class FinishConverterTests
{
	// DragonShield's "Printing" column: Normal / Foil plus an open-ended set of variant treatments; no etched tier.
	static readonly FinishConverter DragonShield = new(new FinishConfiguration("Printing", Foil: "Foil", Normal: "Normal", Etched: null));
	// Manabox-style: a distinct etched string.
	static readonly FinishConverter WithEtched = new(new FinishConfiguration("Foil", Foil: "foil", Normal: "normal", Etched: "etched"));

	// The accept branches never touch row or memberMapData; only the throw path does.
	static object? Read(FinishConverter c, string text) => c.ConvertFromString(text, null!, null!);
	static string? Write(FinishConverter c, CardFinish f) => c.ConvertToString(f, null!, null!);

	[Theory]
	[InlineData("Foil")]
	[InlineData("Surge Foil")]
	[InlineData("Step and Compleat Foil")]
	[InlineData("Rainbow Foil")]
	[InlineData("Double Rainbow Foil")]
	[InlineData("Gilded Foil")]
	[InlineData("Galaxy Foil")]
	[InlineData("surge foil")]
	public void VariantFoilTreatments_MapToFoil(string printing) =>
		Read(DragonShield, printing).Should().Be(CardFinish.Foil);

	[Theory]
	[InlineData("Normal")]
	[InlineData("")]
	[InlineData("   ")]
	public void NonFoilTreatments_MapToNormal(string printing) =>
		Read(DragonShield, printing).Should().Be(CardFinish.Normal);

	[Fact]
	public void EtchedString_MapsToEtched_WhenFormatHasEtchedTier() =>
		Read(WithEtched, "etched").Should().Be(CardFinish.Etched);

	[Fact]
	public void WriteEtched_WithEtchedTier_EmitsEtchedString() =>
		Write(WithEtched, CardFinish.Etched).Should().Be("etched");

	[Fact]
	public void WriteEtched_WithoutEtchedTier_FallsBackToFoilString() =>
		Write(DragonShield, CardFinish.Etched).Should().Be("Foil", "DragonShield has no etched tier; etched collapses to its Foil string");

	[Fact]
	public void WriteUnknown_EmitsEmptyCell() =>
		// Empty string, NOT null: a null return makes CsvHelper drop the field and shift later columns.
		Write(DragonShield, CardFinish.Unknown).Should().BeEmpty("a finish we never learned writes blank rather than asserting Normal");
}
