using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class CardNameConverter : ITypeConverter
{
	readonly bool _useShortNames;
	readonly List<string> _doubleFacedCards;

	public CardNameConverter(CardNameConfiguration configuration)
	{
		_useShortNames = configuration.ShortNames;
		var api = ServiceConfiguration.CachedApi;
		_doubleFacedCards = api.GetDoubleFacedCardNames();
	}

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		if (string.IsNullOrWhiteSpace(text)) { return null; }

		var match = _doubleFacedCards.FirstOrDefault(c => c.Split(" // ").First().Equals(text));

		return (_useShortNames && match is not null) ? match : text;
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{
		string cardName = value as string ?? throw new ArgumentException($"{value} should be a string");
		bool isDoubleFaced = cardName.Contains(" // ");

		cardName = (isDoubleFaced && _useShortNames) ? cardName.Split(" // ").First() : cardName;
		// Remove " Token" from the end of the card name to adhere to Scryfall's naming convention
		// TODO When converting to Moxfield, we would need to add it back in. The only way to detect this is to check if SetID has 4 characters, starting with a "T" (e.g. TMH2 or similar)
		if (cardName.EndsWith(" Token")) { cardName = cardName.Replace(" Token", ""); }

		return cardName;
	}
}

