using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class CardNameConverter : ITypeConverter
{
	readonly bool _useShortNames;

	public CardNameConverter(CardNameConfiguration configuration) => _useShortNames = configuration.ShortNames;

	public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
	{
		var match = DeckFormat.CardNames.FirstOrDefault(c => c.StartsWith(text));

		return (_useShortNames && match is not null) ? match : text;
	}

	public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
	{
		string cardName = value as string ?? throw new ArgumentException($"{value} should be a string");
		bool isDoubleFaced = cardName.Contains(" // ");

		return (isDoubleFaced && _useShortNames) ? cardName.Split(" // ").First() : cardName;
	}
}

