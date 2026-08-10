using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

// Read side translates Deckbox-internal Edition Codes (ex_127, pp_neo) to Scryfall codes; writes inherit pass-through since Deckbox accepts Scryfall codes.
internal sealed class DeckboxCodeReadConverter(IReadOnlyDictionary<string, string> codeAliases) : DefaultTypeConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return null; }
		return codeAliases.TryGetValue(text, out var scryfall) ? scryfall : text.ToUpperInvariant();
	}
}
