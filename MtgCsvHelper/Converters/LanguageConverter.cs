using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class LanguageConverter(LanguageConfiguration configuration) : ITypeConverter
{
	readonly bool _useShortNames = configuration.ShortNames;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		return text switch
		{
			null or "" => null,
			_ when _useShortNames => text,
			_ => GetLanguageCode(text)

		};

		string? GetLanguageCode(string englishName)
		{
			var culture = CultureInfo.GetCultures(CultureTypes.NeutralCultures).FirstOrDefault(c => c.EnglishName.Equals(englishName));
			if (culture is null)
			{
				Log.Warning($"Unable to find language code for {englishName}");
				return null;
			}

			return culture.TwoLetterISOLanguageName;
		}
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{
		if (value is null || value is not string langCode) { return null; }
		var cultureInfo = new CultureInfo(langCode);
		return _useShortNames ? langCode : cultureInfo.EnglishName;
	}
}

