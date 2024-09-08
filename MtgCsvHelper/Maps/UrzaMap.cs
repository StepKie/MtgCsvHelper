using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

public class UrzaMap : ClassMap<UrzaCollectionEntry>
{
	public UrzaMap(DeckConfig columnConfig)
	{
		ConfigureMaps(columnConfig);
	}

	void ConfigureMaps(DeckConfig columnConfig)
	{
		Map(entry => entry.RegularEntry.Amount).Name(columnConfig.Quantity).Index(1);
		Map(entry => entry.RegularEntry.Card.Printing.Name).TypeConverter(new CardNameConverter(columnConfig.CardName)).Name(columnConfig.CardName.HeaderName).Index(0);
		Map(entry => entry.RegularEntry.Card.Printing.CollectorNumber).Name(columnConfig.SetNumber).Optional();

		// We implicitly assume that at least one of them is present (some sites use SetName, some use SetCode, some use both...)
		Map(entry => entry.RegularEntry.Card.Printing.Set).TypeConverter<UpperCaseConverter>().Name(columnConfig.SetCode).Optional();
		Map(entry => entry.RegularEntry.Card.Printing.SetName).Name(columnConfig.SetName).Optional();

		if (columnConfig.Condition is ConditionConfiguration cond) { Map(entry => entry.Card.Condition).TypeConverter(new CardConditionConverter(cond)).Name(cond.HeaderName).Optional(); }
		if (columnConfig.Finish is FinishConfiguration finish) { Map(entry => entry.Card.Foil).TypeConverter(new FinishConverter(finish)).Name(finish.HeaderName).Optional(); }
		if (columnConfig.Language is LanguageConfiguration lang) { Map(entry => entry.Card.Language).TypeConverter(new LanguageConverter(lang)).Name(lang.HeaderName).Optional(); }
		if (columnConfig.PriceBought is PriceConfiguration price) { Map(entry => entry.Card.PriceBought).TypeConverter(new PriceConverter(price)).Name(price.HeaderName).Optional(); }
	}
}
