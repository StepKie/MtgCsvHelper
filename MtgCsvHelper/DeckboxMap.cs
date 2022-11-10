using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper;



public class CsvToCardMap : ClassMap<PhysicalMtgCard>
{
    private readonly IConfiguration _config;
    public static ColumnNames _colNames;

    public void SetMapping(DeckFormat format)
	{

        _colNames = _config.GetColumnNames(DeckFormat.DRAGONSHIELD) ?? throw new ArgumentException("Format configuration not found");

        Map(card => card.Count).Name(_colNames.Quantity);
        Map(card => card.Printing.Card.Name).Name(_colNames.CardName);
        Map(card => card.Printing.Set.FullName).Name(_colNames.SetName);
        Map(card => card.Printing.IdInSet).Name(_colNames.SetNumber);
        Map(card => card.Condition).TypeConverter<CardConditionConverter>().Name(nameof(ColumnNames.Condition), _colNames.Condition.Name);
        Map(card => card.Language).Name("Language");
        Map(card => card.Foil).TypeConverter<FinishConverter>().Name(_colNames.Finish.Name);
        //Map(card => card.PriceBought).Name("My Price", "Price Bought");
    }
    // DECKBOX: Count,Tradelist Count, *Name, *Edition, *Card Number, *Condition, *Language, Foil, Signed, Artist Proof, Altered Art, Misprint, Promo, Textless, *My Price
    // DRAGONSHIELD: Folder Name,Quantity,Trade Quantity,Card Name,Set Code,Set Name,Card Number,Condition,Printing,Language,Price Bought,Date Bought,LOW,MID,MARKET
    public CsvToCardMap(IConfiguration config, DeckFormat format = DeckFormat.DRAGONSHIELD)
    {
        _config = config;
        SetMapping(format);
    }


    public class CardConditionConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {
            return CardCondition.FromDisplayName<CardCondition>(text);
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            return (value as CardCondition)?.Name ?? "";
        }
    }

    public class FinishConverter : ITypeConverter
    {
        public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
        {

            return text.ToLower().Equals(_colNames.Finish.Values[1]);
        }

        public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            int index = (value is true) ? 1 : 0;
            return _colNames.Finish.Values[index];
        }
    }
}


public record ColumnNames(string Quantity, string CardName, ColumnNameWithValues Finish, ColumnNameWithValues Condition, string SetCode, string SetName, string SetNumber);

public record ColumnNameWithValues(string Name, string[] Values);

public enum DeckFormat { MOXFIELD, DRAGONSHIELD }


public record Moo(string meh, string wuff = nameof(Moo))
{
    public record Meh(string mew);
}