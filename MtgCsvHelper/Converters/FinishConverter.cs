using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class FinishConverter(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrWhiteSpace(text)) { return false; }
		if (text.MatchesConfig(_finishConfig.Foil)) { return true; }
		// Etched collapses to foil=true: the current `bool? Foil` model can't represent it.
		// Tri-state Foil enum is the planned follow-up; see ConvertToString for the matching write-side data loss.
		if (text.MatchesConfig(_finishConfig.Etched)) { return true; }
		if (text.MatchesConfig(_finishConfig.Normal)) { return false; }
		// Variant treatments (Surge Foil, Step and Compleat Foil, …) all carry the word "Foil".
		if (text.Contains("foil", StringComparison.OrdinalIgnoreCase)) { return true; }

		throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Foil value '{text}'");
	}

	// An etched card stored as foil=true is written here as the Foil string — silent promotion
	// from etched to foil on round-trip. Resolved when Foil becomes a tri-state enum (see ConvertFromString).
	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is true ? _finishConfig.Foil : _finishConfig.Normal;
}
