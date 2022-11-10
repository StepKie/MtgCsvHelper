using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper;

public class CardConditionConverter : ITypeConverter
{
	public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
	{
		return CardCondition.FromDisplayName<CardCondition>(text);
	}

	public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
	{
		return (value as CardCondition)?.Name  ?? "";
	}
}

public class FinishConverter : ITypeConverter
{
	public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
	{

		return text.ToLower().Equals("foil");
	}

	public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
	{
		return (value is true) ? "Foil" : "Normal";
	}
}

public class DeckboxMap : ClassMap<PhysicalMtgCard>
{
	
	// DECKBOX: Count,Tradelist Count, *Name, *Edition, *Card Number, *Condition, *Language, Foil, Signed, Artist Proof, Altered Art, Misprint, Promo, Textless, *My Price
	// DRAGONSHIELD: Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought,LOW,MID,MARKET
	public DeckboxMap()
	{
		Map(physicalCard => physicalCard.Count).Name("Count", "Quantity");
		Map(physicalCard => physicalCard.Printing.Card.Name).Name("Name", "Card Name");
		Map(physicalCard => physicalCard.Printing.Set.FullName).Name("Edition", "Set Name");
		Map(physicalCard => physicalCard.Printing.IdInSet).Name("Card Number");
		Map(physicalCard => physicalCard.Condition).TypeConverter<CardConditionConverter>().Name("Condition");
		Map(physicalCard => physicalCard.Language).Name("Language");
		Map(physicalCard => physicalCard.Foil).TypeConverter<FinishConverter>(). Name("Foil", "Printing").TypeConverterOption.CultureInfo(null);
		//Map(physicalCard => physicalCard.PriceBought).Name("My Price", "Price Bought");
	}
}