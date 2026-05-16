using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

/// <summary> The common format is the Scryfall language key (see keys in DEFAULT_LANGUAGES) </summary>
public class LanguageConverter(LanguageConfiguration configuration) : ITypeConverter
{
    readonly LanguageMappings _mappings = configuration.Mappings;

    public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
    {
        if (string.IsNullOrEmpty(text)) { return null; }

        // Flipping the operand order (text vs config) so a null config entry safely returns false
        // rather than throwing NRE — latent bug noted in PR #66 review.
        return text switch
        {
            _ when text.MatchesConfig(_mappings.en) => nameof(LanguageMappings.en),
            _ when text.MatchesConfig(_mappings.es) => nameof(LanguageMappings.es),
            _ when text.MatchesConfig(_mappings.fr) => nameof(LanguageMappings.fr),
            _ when text.MatchesConfig(_mappings.de) => nameof(LanguageMappings.de),
            _ when text.MatchesConfig(_mappings.it) => nameof(LanguageMappings.it),
            _ when text.MatchesConfig(_mappings.pt) => nameof(LanguageMappings.pt),
            _ when text.MatchesConfig(_mappings.ja) => nameof(LanguageMappings.ja),
            _ when text.MatchesConfig(_mappings.ko) => nameof(LanguageMappings.ko),
            _ when text.MatchesConfig(_mappings.ru) => nameof(LanguageMappings.ru),
            _ when text.MatchesConfig(_mappings.zht) => nameof(LanguageMappings.zht),
            _ when text.MatchesConfig(_mappings.zhs) => nameof(LanguageMappings.zhs),
            _ => throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Language value '{text}'"),
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

