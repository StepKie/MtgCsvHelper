using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

/// <summary> The common format is the Scryfall language key (see keys in DEFAULT_LANGUAGES) </summary>
public class LanguageConverter(LanguageConfiguration configuration) : ITypeConverter
{
    readonly LanguageMappings _mappings = configuration.Mappings;

    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        return text switch
        {
            _ when _mappings.en.Equals(text) => nameof(LanguageMappings.en),
            _ when _mappings.es.Equals(text) => nameof(LanguageMappings.es),
            _ when _mappings.fr.Equals(text) => nameof(LanguageMappings.fr),
            _ when _mappings.de.Equals(text) => nameof(LanguageMappings.de),
            _ when _mappings.it.Equals(text) => nameof(LanguageMappings.it),
            _ when _mappings.pt.Equals(text) => nameof(LanguageMappings.pt),
            _ when _mappings.ja.Equals(text) => nameof(LanguageMappings.ja),
            _ when _mappings.ko.Equals(text) => nameof(LanguageMappings.ko),
            _ when _mappings.ru.Equals(text) => nameof(LanguageMappings.ru),
            _ when _mappings.zht.Equals(text) => nameof(LanguageMappings.zht),
            _ when _mappings.zhs.Equals(text) => nameof(LanguageMappings.zhs),
            _ => null,

        };
    }

    public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
    {
        return value switch
        {
            nameof(LanguageMappings.en) => _mappings.en,
            nameof(LanguageMappings.es) => _mappings.es,
            nameof(LanguageMappings.fr) => _mappings.fr,
            nameof(LanguageMappings.de) => _mappings.de,
            nameof(LanguageMappings.it) => _mappings.it,
            nameof(LanguageMappings.pt) => _mappings.pt,
            nameof(LanguageMappings.ja) => _mappings.ja,
            nameof(LanguageMappings.ko) => _mappings.ko,
            nameof(LanguageMappings.ru) => _mappings.ru,
            nameof(LanguageMappings.zht) => _mappings.zht,
            nameof(LanguageMappings.zhs) => _mappings.zhs,
            _ => null,

        };
    }
}

