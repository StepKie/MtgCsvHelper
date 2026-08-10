using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary>First multiverse id of the printing: "0" when the resolved printing has none (Scryfall's convention), blank when unresolved (null).</summary>
public class MultiverseIdsConverter : DefaultTypeConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		int.TryParse(text, out var id) && id > 0 ? new[] { id } : null;

	public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value switch
	{
		int[] and [var first, ..] => first.ToString(),
		int[] => "0",
		_ => "",
	};
}
