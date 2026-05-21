using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

// On READ from a Deckbox CSV, translates Deckbox-internal Edition Codes (`ex_127`, `pp_neo`)
// to the Scryfall codes the catalog can resolve (TMH2, PNEO). Pass-through for codes already
// in Scryfall form. No write-side override is needed — Deckbox import accepts Scryfall codes,
// so the inherited DefaultTypeConverter behavior (stringify, "" for null) is correct on emit.
internal sealed class DeckboxCodeReadConverter(IReadOnlyDictionary<string, string> codeAliases) : DefaultTypeConverter
{
	public override object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrEmpty(text)) { return null; }
		return codeAliases.TryGetValue(text, out var scryfall) ? scryfall : text.ToUpperInvariant();
	}
}
