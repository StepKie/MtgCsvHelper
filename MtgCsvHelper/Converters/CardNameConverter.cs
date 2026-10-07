using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class CardNameConverter(CardNameConfiguration configuration, IReferenceCardCatalog catalog) : ITypeConverter
{
	readonly bool _useShortNames = configuration.ShortNames;
	readonly bool _encodeToken = configuration.EncodeToken;
	readonly IReadOnlyList<string> _fullNameLayouts = configuration.FullNameLayoutsOrDefault;

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
		bool hasTwoHalves = cardName.Contains(" // ");

		// Unknown layout strips too, so brand-new DFC sets not yet in the catalog don't regress on export.
		if (hasTwoHalves && _useShortNames)
		{
			var layout = catalog.GetLayoutByName(cardName);
			if (layout is null || !_fullNameLayouts.Contains(layout, StringComparer.OrdinalIgnoreCase))
			{
				cardName = cardName.Split(" // ").First();
			}
		}

		// Formats with EncodeToken decorate token names (e.g. Dragon Shield's "Beast Token"); re-add the suffix the read path stripped.
		if (_encodeToken && catalog.IsTokenName(cardName)) { cardName += " Token"; }

		return cardName;
	}
}
