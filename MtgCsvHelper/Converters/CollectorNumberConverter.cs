using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

// MTGO emits collector numbers as "N/M" (e.g. "116/350") where M is the set total.
// Strip the suffix on read; all other formats pass through unchanged since "/" doesn't
// appear in Scryfall collector numbers. Write side preserves whatever's in the model
// (we don't know the set total, so MTGO round-trips will emit just "N").
public class CollectorNumberConverter : StringConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		text is null ? base.ConvertFromString(text, row, memberMapData) : text.Split('/', 2)[0];
}
