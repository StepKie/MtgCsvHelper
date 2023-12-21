using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

public class CsvToCardMap : ClassMap<PhysicalMtgCard>
{
	public CsvToCardMap(DeckConfig columnConfig)
	{
		Map(card => card.Count).Name(columnConfig.Quantity);
		Map(card => card.Printing.Name).TypeConverter(new CardNameConverter(columnConfig.CardName)).Name(columnConfig.CardName.HeaderName);
		Map(card => card.Printing.CollectorNumber).Name(columnConfig.SetNumber);

		// We implicitly assume that at least one of them is present (some sites use SetName, some use SetCode, some use both...)
		Map(card => card.Printing.Set).TypeConverter<UpperCaseConverter>().Name(columnConfig.SetCode).Optional();
		Map(card => card.Printing.SetName).Name(columnConfig.SetName).Optional();

		if (columnConfig.Condition is ConditionConfiguration cond) { Map(card => card.Condition).TypeConverter(new CardConditionConverter(cond)).Name(cond.HeaderName); }
		if (columnConfig.Finish is FinishConfiguration finish) { Map(card => card.Foil).TypeConverter(new FinishConverter(finish)).Name(finish.HeaderName); }
		if (columnConfig.Language is LanguageConfiguration lang) { Map(card => card.Language).TypeConverter(new LanguageConverter(lang)).Name(lang.HeaderName).Optional(); }
		if (columnConfig.PriceBought is PriceConfiguration price) { Map(card => card.PriceBought).TypeConverter(new PriceConverter(price)).Name(price.HeaderName).Optional(); }
	}
}

public record DeckConfig(
	string Quantity,
	CardNameConfiguration CardName,
	string SetNumber,
	string? SetCode = null,
	string? SetName = null,
	FinishConfiguration? Finish = null,
	ConditionConfiguration? Condition = null,
	LanguageConfiguration? Language = null,
	PriceConfiguration? PriceBought = null
	);

public record CardNameConfiguration(
	string HeaderName,
	bool ShortNames);
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
