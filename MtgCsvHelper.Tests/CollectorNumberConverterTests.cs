using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Tests;

// Pins the contract that the read path strips MTGO's "N/M" suffix to "N" and leaves
// everything else alone. The converter is applied to every format's collector-number map
// — a no-op for any string without a `/` — so changes here affect more than just MTGO.
public class CollectorNumberConverterTests
{
	// Null/empty handling is inherited from CsvHelper's StringConverter (uses NullValues config)
	// and not pinned here; the cases below only cover the slash-stripping the converter adds.
	[Theory]
	[InlineData("116/350", "116")]    // MTGO N/M form — strip the total
	[InlineData("116", "116")]        // already plain — no-op
	[InlineData("U8", "U8")]          // letter-prefix collector numbers (Deckbox) pass through
	[InlineData("★1", "★1")]         // star-variant promos — no slash, untouched
	public void ConvertFromString_StripsSetTotal(string input, string expected)
	{
		new CollectorNumberConverter()
			.ConvertFromString(input, row: null!, memberMapData: null!)
			.Should().Be(expected);
	}
}
