using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class FinishConverter(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return false; }
		if (text.Equals(_finishConfig.Foil)) { return true; }
		if (_finishConfig.Etched is not null && text.Equals(_finishConfig.Etched)) { return true; }
		if (text.Equals(_finishConfig.Normal)) { return false; }

		throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Foil value '{text}'");
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is true ? _finishConfig.Foil : _finishConfig.Normal;
}
