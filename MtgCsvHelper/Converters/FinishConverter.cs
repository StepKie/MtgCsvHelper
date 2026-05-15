using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class FinishConverter(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	// Known variant foil treatments emitted by some sites (DragonShield: "Rainbow Foil",
	// "Double Rainbow Foil"). They collapse to foil=true in our binary `bool? Foil` model;
	// a tri-state finish enum + config-level aliases would preserve the distinction (follow-up).
	static readonly string[] FoilVariants = ["Rainbow Foil", "Double Rainbow Foil", "Gilded Foil"];

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return false; }
		if (text.Equals(_finishConfig.Foil)) { return true; }
		if (_finishConfig.Etched is not null && text.Equals(_finishConfig.Etched)) { return true; }
		if (text.Equals(_finishConfig.Normal)) { return false; }
		if (FoilVariants.Contains(text, StringComparer.Ordinal)) { return true; }

		throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Foil value '{text}'");
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is true ? _finishConfig.Foil : _finishConfig.Normal;
}
