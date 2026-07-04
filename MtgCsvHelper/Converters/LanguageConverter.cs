using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary> The model value is the Scryfall language code (the keys of <see cref="LanguageMappings"/>). </summary>
public class LanguageConverter : ITypeConverter
{
	readonly IReadOnlyDictionary<string, string> _valueByCode;

	public LanguageConverter(LanguageConfiguration configuration)
	{
		var m = configuration.Mappings;
		_valueByCode = new Dictionary<string, string>
		{
			[nameof(m.en)] = m.en,
			[nameof(m.es)] = m.es,
			[nameof(m.fr)] = m.fr,
			[nameof(m.de)] = m.de,
			[nameof(m.it)] = m.it,
			[nameof(m.pt)] = m.pt,
			[nameof(m.ja)] = m.ja,
			[nameof(m.ko)] = m.ko,
			[nameof(m.ru)] = m.ru,
			[nameof(m.zht)] = m.zht,
			[nameof(m.zhs)] = m.zhs,
		};
	}

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return null; }

		return _valueByCode.FirstOrDefault(kv => text.MatchesConfig(kv.Value)).Key
			?? throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Language value '{text}'");
	}

	// Empty string (not null) on no-match: a null return makes CsvHelper skip the field entirely, shifting every subsequent column.
	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) =>
		value is string code ? _valueByCode.GetValueOrDefault(code) ?? "" : "";
}
