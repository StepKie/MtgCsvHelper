using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class FinishConverter	(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) => text?.Equals(_finishConfig.Foil);

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is true ? _finishConfig.Foil : _finishConfig.Normal;
}
