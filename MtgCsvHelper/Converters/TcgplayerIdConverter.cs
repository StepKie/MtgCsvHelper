using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary>TCGplayer product id column: 0 means "no TCGplayer product" and round-trips as blank.</summary>
public class TcgplayerIdConverter : DefaultTypeConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		int.TryParse(text, out var id) ? id : 0;

	public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
		value is int id and > 0 ? id.ToString() : "";
}
