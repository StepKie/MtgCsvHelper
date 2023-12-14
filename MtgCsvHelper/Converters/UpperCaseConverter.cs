using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class UpperCaseConverter : StringConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		base.ConvertFromString(text?.ToUpperInvariant(), row, memberMapData);
}
