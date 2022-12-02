using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class CardNameConverter : ITypeConverter
{
	readonly CardNameConfiguration _cardNameConfig;

	public CardNameConverter(CardNameConfiguration configuration) => _cardNameConfig = configuration;

	public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
	{
		if (_cardNameConfig.ShortNames && DeckFormat.CardNames.ContainsKey(text))
		{
			text = DeckFormat.CardNames[text];
		}

		return text;
	}

	public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
	{
		string cardName = value as string ?? throw new ArgumentException($"Expected card name string, got {value}");

		if (cardName is not null && _cardNameConfig.ShortNames && DeckFormat.CardNames.ContainsValue(cardName))
		{
			cardName = DeckFormat.CardNames.First(x => x.Value == cardName).Key;
		}

		return cardName!;
	}
}

