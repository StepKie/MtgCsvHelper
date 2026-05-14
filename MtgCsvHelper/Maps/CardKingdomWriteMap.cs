using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

// Card Kingdom requires a fixed 4-column shape with uppercase set names.
// It is a write-only format (their import is buylist-style, no condition/language/foil etched).
public class CardKingdomWriteMap : ClassMap<PhysicalMtgCard>
{
	public CardKingdomWriteMap(FormatConfig columnConfig, IReferenceCardCatalog catalog)
	{
		_ = columnConfig.CardName ?? throw new InvalidOperationException($"Format '{columnConfig.Name}' is missing the required 'CardName' configuration section.");
		_ = columnConfig.Finish ?? throw new InvalidOperationException($"Format '{columnConfig.Name}' is missing the required 'Finish' configuration section.");

		Map(card => card.Printing.Name).TypeConverter(new CardNameConverter(columnConfig.CardName, catalog)).Name(columnConfig.CardName.HeaderName).Index(0);
		if (columnConfig.SetName is not null) { Map(card => card.Printing.SetName).TypeConverter<UpperCaseConverter>().Name(columnConfig.SetName).Index(1); }
		Map(card => card.Foil).TypeConverter(new FinishConverter(columnConfig.Finish)).Name(columnConfig.Finish.HeaderName).Index(2);
		Map(card => card.Count).Name(columnConfig.Quantity).Index(3);
	}
}
