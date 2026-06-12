using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary>
/// Binds the Scryfall UUID column to <c>Printing.Id</c>: a blank or unparseable cell reads as
/// <see cref="Guid.Empty"/>, and an unresolved Id writes as a blank cell rather than an all-zero GUID.
/// </summary>
internal sealed class ScryfallIdConverter : DefaultTypeConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) =>
		Guid.TryParse(text, out var id) ? id : Guid.Empty;

	public override string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
		value is Guid id && id != Guid.Empty ? id.ToString("D") : string.Empty;
}
