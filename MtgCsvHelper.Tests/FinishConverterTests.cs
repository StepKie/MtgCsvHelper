using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Tests;

public class FinishConverterTests
{
	// DragonShield's "Printing" column: Normal / Foil plus an open-ended set of variant treatments.
	static readonly FinishConverter Converter = new(new FinishConfiguration("Printing", Foil: "Foil", Normal: "Normal", Etched: null));

	// The accept/normal branches never touch row or memberMapData; only the throw path does.
	static object? Convert(string text) => Converter.ConvertFromString(text, null!, null!);

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
		Convert(printing).Should().Be(true);

	[Theory]
	[InlineData("Normal")]
	[InlineData("")]
	[InlineData("   ")]
	public void NonFoilTreatments_MapToNonFoil(string printing) =>
		Convert(printing).Should().Be(false);
}
