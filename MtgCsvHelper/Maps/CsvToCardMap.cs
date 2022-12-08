using CsvHelper.Configuration;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

public class CsvToCardMap : ClassMap<PhysicalMtgCard>
{
	public CsvToCardMap(DeckConfig columnConfig)
	{
		Map(card => card.Count).Name(columnConfig.Quantity);
		Map(card => card.Printing.Card.Name).TypeConverter(new CardNameConverter(columnConfig.CardName)).Name(columnConfig.CardName.HeaderName);

		if (!string.IsNullOrEmpty(columnConfig.SetName)) { Map(card => card.Printing.Set.FullName).Name(columnConfig.SetName); }
		if (!string.IsNullOrEmpty(columnConfig.SetCode)) { Map(card => card.Printing.Set.Code).Name(columnConfig.SetCode); }
		if (!string.IsNullOrEmpty(columnConfig.SetNumber)) { Map(card => card.Printing.IdInSet).Name(columnConfig.SetNumber); }
		if (columnConfig.Condition is not null) { Map(card => card.Condition).TypeConverter(new CardConditionConverter(columnConfig.Condition)).Name(columnConfig.Condition.HeaderName); }
		if (columnConfig.Finish is not null) { Map(card => card.Foil).TypeConverter(new FinishConverter(columnConfig.Finish)).Name(columnConfig.Finish.HeaderName); }

		if (columnConfig.Finish is not null) { Map(card => card.Foil).TypeConverter(new FinishConverter(columnConfig.Finish)).Name(columnConfig.Finish.HeaderName); }

		Map(card => card.Language).Name("Language").Optional();
		//Map(card => card.PriceBought).Name("My Price", "Price Bought");
	}
}

public record DeckConfig(
	string Quantity,
	CardNameConfiguration CardName,
	FinishConfiguration Finish,
	ConditionConfiguration Condition,
	string SetCode,
	string? SetName,
	string SetNumber
	);

public record CardNameConfiguration(string HeaderName, bool ShortNames);
public record FinishConfiguration(string HeaderName, string Foil, string Normal, string Etched);
public record ConditionConfiguration(string HeaderName, string Mint, string NearMint, string Excellent, string Good, string LightlyPlayed, string Played, string Poor);
