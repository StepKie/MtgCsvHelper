using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class FinishConverter(FinishConfiguration configuration) : ITypeConverter
{
	readonly FinishConfiguration _finishConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		// A blank cell is the format's "not foil" — Moxfield even configures Normal as the empty string.
		if (string.IsNullOrWhiteSpace(text)) { return CardFinish.Normal; }
		if (text.MatchesConfig(_finishConfig.Etched)) { return CardFinish.Etched; }
		if (text.MatchesConfig(_finishConfig.Foil)) { return CardFinish.Foil; }
		if (text.MatchesConfig(_finishConfig.Normal)) { return CardFinish.Normal; }
		// Variant treatments (Surge Foil, Rainbow Foil, …) are promo_types layered on a foil finish.
		if (text.Contains("foil", StringComparison.OrdinalIgnoreCase)) { return CardFinish.Foil; }

		throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Foil value '{text}'");
	}

	// Etched falls back to the Foil string when the format has no etched tier (Deckbox/Cardmarket collapse it on storage anyway).
	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
		value switch
		{
			CardFinish.Etched => _finishConfig.Etched ?? _finishConfig.Foil,
			CardFinish.Foil   => _finishConfig.Foil,
			CardFinish.Normal => _finishConfig.Normal,
			// "" not null: a null return makes CsvHelper drop the field and shift every later column.
			_                 => "",
		};
}
