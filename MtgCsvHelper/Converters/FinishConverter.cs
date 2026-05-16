using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class FinishConverter(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	// DragonShield-specific variant foil treatments. Hardcoded here because no other format
	// in the repo emits these strings; widening this list silently re-routes other formats'
	// values, so per-format config aliases are the right long-term fix (tracked as a follow-up
	// to the tri-state Foil enum below).
	static readonly string[] FoilVariants = ["Rainbow Foil", "Double Rainbow Foil", "Gilded Foil"];

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return false; }
		if (_finishConfig.Foil is not null && text.Equals(_finishConfig.Foil)) { return true; }
		// Etched collapses to foil=true: the current `bool? Foil` model can't represent it.
		// Tri-state Foil enum is the planned follow-up; see ConvertToString for the matching write-side data loss.
		if (_finishConfig.Etched is not null && text.Equals(_finishConfig.Etched)) { return true; }
		if (_finishConfig.Normal is not null && text.Equals(_finishConfig.Normal)) { return false; }
		if (FoilVariants.Contains(text, StringComparer.Ordinal)) { return true; }

		throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Foil value '{text}'");
	}

	// An etched card stored as foil=true is written here as the Foil string — silent promotion
	// from etched to foil on round-trip. Resolved when Foil becomes a tri-state enum (see ConvertFromString).
	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is true ? _finishConfig.Foil : _finishConfig.Normal;
}
