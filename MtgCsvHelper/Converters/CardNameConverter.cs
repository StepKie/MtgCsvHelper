using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class CardNameConverter(CardNameConfiguration configuration, IReferenceCardCatalog catalog) : ITypeConverter
{
	readonly bool _useShortNames = configuration.ShortNames;
	readonly bool _encodeToken = configuration.EncodeToken;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		var result = (text, _useShortNames) switch
		{
			(null or "", _) => null,
			(_, false) => text,
			(_, true) => catalog.ExpandFrontFaceToFullName(text!) ?? text,
		};

		return result?.Replace(" Token", "");
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{
		string cardName = value as string ?? throw new ArgumentException($"{value} should be a string");
		bool isDoubleFaced = cardName.Contains(" // ");

		cardName = (isDoubleFaced && _useShortNames) ? cardName.Split(" // ").First() : cardName;
		// Remove " Token" from the end of the card name to adhere to Scryfall's naming convention
		// TODO When converting to Moxfield, we would need to add it back in. The only way to detect this is to check if SetID has 4 characters, starting with a "T" (e.g. TMH2 or similar)
		if (_encodeToken && catalog.IsTokenName(cardName)) { cardName += " Token"; }

		return cardName;
	}
}
