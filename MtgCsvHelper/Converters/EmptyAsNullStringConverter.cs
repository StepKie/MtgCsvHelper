using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class EmptyAsNullStringConverter : StringConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		string.IsNullOrEmpty(text) ? null : text;

	// Intentionally no ConvertToString override: StringConverter writes "" for null, which is
	// what we want. Overriding it to return null would make CsvHelper skip the field entirely
	// and shift every subsequent column one slot left (see LanguageConverter for the same trap).
}
