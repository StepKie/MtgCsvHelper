using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

public class DefaultCollectionEntryMap : ClassMap<CardCollectionEntry>
{
	public DefaultCollectionEntryMap(DeckConfig columnConfig, bool ck)
	{
		// Yes it is ugly. No I dont care. F#!& you CardKingdom!
		if (ck)
		{
			ConfigureCK(columnConfig);
		}
		else
		{
			ConfigureMaps(columnConfig);
		}
	}

	void ConfigureMaps(DeckConfig columnConfig)
	{
		Map(entry => entry.Amount).Name(columnConfig.Quantity).Index(1);
		Map(entry => entry.Card.Printing.Name).TypeConverter(new CardNameConverter(columnConfig.CardName)).Name(columnConfig.CardName.HeaderName).Index(0);
		Map(entry => entry.Card.Printing.CollectorNumber).Name(columnConfig.SetNumber).Optional();

		// We implicitly assume that at least one of them is present (some sites use SetName, some use SetCode, some use both...)
		Map(entry => entry.Card.Printing.Set).TypeConverter<UpperCaseConverter>().Name(columnConfig.SetCode).Optional();
		Map(entry => entry.Card.Printing.SetName).Name(columnConfig.SetName).Optional();

		if (columnConfig.Condition is ConditionConfiguration cond) { Map(entry => entry.Card.Condition).TypeConverter(new CardConditionConverter(cond)).Name(cond.HeaderName).Optional(); }
		if (columnConfig.Finish is FinishConfiguration finish) { Map(entry => entry.Card.Foil).TypeConverter(new FinishConverter(finish)).Name(finish.HeaderName).Optional(); }
		if (columnConfig.Language is LanguageConfiguration lang) { Map(entry => entry.Card.Language).TypeConverter(new LanguageConverter(lang)).Name(lang.HeaderName).Optional(); }
		if (columnConfig.PriceBought is PriceConfiguration price) { Map(entry => entry.Card.PriceBought).TypeConverter(new PriceConverter(price)).Name(price.HeaderName).Optional(); }
	}

	void ConfigureCK(DeckConfig columnConfig)
	{
		Map(entry => entry.Card.Printing.Name).TypeConverter(new CardNameConverter(columnConfig.CardName)).Name(columnConfig.CardName.HeaderName).Index(0);
		Map(entry => entry.Card.Printing.SetName).TypeConverter<UpperCaseConverter>().Name(columnConfig.SetName).Index(1);
		Map(entry => entry.Card.Foil).TypeConverter(new FinishConverter(columnConfig.Finish!)).Name(columnConfig.Finish!.HeaderName).Index(2);
		Map(entry => entry.Amount).Name(columnConfig.Quantity).Index(3);
	}
}

public record DeckConfig(
	string Name,
	string Quantity,
	CardNameConfiguration CardName,
	string SetNumber,
	string? SetCode = null,
	string? SetName = null,
	FinishConfiguration? Finish = null,
	ConditionConfiguration? Condition = null,
	LanguageConfiguration? Language = null,
	PriceConfiguration? PriceBought = null
	)
{
	public Currency Currency => Currency.FromString(PriceBought?.Currency);
};

public record CardNameConfiguration(
	string HeaderName,
	bool ShortNames = false,
	bool EncodeToken = false);
public record FinishConfiguration(
	string HeaderName,
	string Foil,
	string Normal,
	string? Etched = null);
public record ConditionConfiguration(
	string HeaderName,
	string Mint,
	string NearMint,
	string Excellent,
	string Good,
	string LightlyPlayed,
	string Played,
	string Poor);
public record LanguageConfiguration(
	string HeaderName,
	bool ShortNames);

public record PriceConfiguration(
	string HeaderName,
	string Currency,
	string CurrencySymbol);
