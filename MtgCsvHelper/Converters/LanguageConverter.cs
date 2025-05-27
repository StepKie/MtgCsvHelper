using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class LanguageConverter(LanguageConfiguration configuration) : ITypeConverter
{
	// We have used CultureInfo.GetCultures(CultureTypes.NeutralCultures) before
	// but in WebAssembly, the names are shortened and dont contain the full name in CultureInfo.EnglishName
	// hence, we use our own dictionary
	static readonly Dictionary<string, string> _languages = new()
	{
		["English"] = "en",
		["Spanish"] = "es",
		["French"] = "fr",
		["German"] = "de",
		["Italian"] = "it",
		["Portuguese"] = "pt",
		["Japanese"] = "jp",
	};

	readonly bool _useShortNames = configuration.ShortNames;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		return text switch
		{
			null or "" => "",
			_ when _useShortNames => text,
			_ => _languages.GetValueOrDefault(text),
		};
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{
		if (value is not string langCode) { return ""; }
		return _useShortNames ? langCode : _languages.FirstOrDefault(x => x.Value == langCode).Key;
	}
}

