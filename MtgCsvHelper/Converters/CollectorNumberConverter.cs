using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

// Strips MTGO's "/set-total" suffix ("116/350" → "116") on read; a no-op for every other format since Scryfall numbers never contain "/".
public class CollectorNumberConverter : StringConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		text is null ? base.ConvertFromString(text, row, memberMapData) : text.Split('/', 2)[0];
}
