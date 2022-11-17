using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DevTrends.ConfigurationExtensions;
using Microsoft.Extensions.Configuration;
using static MtgCsvHelper.Maps.CsvToCardMap;

namespace MtgCsvHelper.Maps;

public class CsvToCardMap : ClassMap<PhysicalMtgCard>
{
	public string ColumnNameQuantity { get; set; }
	public static IConfiguration? ConfigFile { get; set; }
	static CsvConfig _columnConfig;

	public CsvToCardMap(DeckFormat format)
	{
		_columnConfig = ConfigFile?.Bind<CsvConfig>($"CsvConfigurations:{format}") ?? throw new ArgumentException($"ConfigFile not specified before attempting to use {nameof(CsvToCardMap)}");

		Map(card => card.Count).Name(_columnConfig.Quantity);
		Map(card => card.Printing.Card.Name).Name(_columnConfig.CardName);
		Map(card => card.Printing.Set.FullName).Name(_columnConfig.SetName).Optional();
		Map(card => card.Printing.Set.Code).Name(_columnConfig.SetCode);
		Map(card => card.Printing.IdInSet).Name(_columnConfig.SetNumber);
		Map(card => card.Condition).TypeConverter<CardConditionConverter>().Name(nameof(CsvConfig.Condition), _columnConfig.Condition.HeaderName);
		Map(card => card.Language).Name("Language");
		Map(card => card.Foil).TypeConverter<FinishConverter>().Name(_columnConfig.Finish.HeaderName);
		//Map(card => card.PriceBought).Name("My Price", "Price Bought");
	}

	public class CardConditionConverter : ITypeConverter
	{
		public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
		{
			return text switch
			{
				_ when text.Equals(_columnConfig.Condition.Mint) => CardCondition.MINT,
				_ when text.Equals(_columnConfig.Condition.NearMint) => CardCondition.NEAR_MINT,
				_ when text.Equals(_columnConfig.Condition.Excellent) => CardCondition.EXCELLENT,
				_ when text.Equals(_columnConfig.Condition.Good) => CardCondition.GOOD,
				_ when text.Equals(_columnConfig.Condition.LightlyPlayed) => CardCondition.LIGHTLY_PLAYED,
				_ when text.Equals(_columnConfig.Condition.Played) => CardCondition.PLAYED,
				_ when text.Equals(_columnConfig.Condition.Poor) => CardCondition.POOR,
				_ => "",

			};
		}

		public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
		{
			return (value as CardCondition) switch
			{
				{ Name: "Mint" } => _columnConfig.Condition.Mint,
				{ Name: "NearMint" } => _columnConfig.Condition.NearMint,
				{ Name: "Excellent" } => _columnConfig.Condition.Excellent,
				{ Name: "Good" } => _columnConfig.Condition.Good,
				{ Name: "LightlyPlayed" } => _columnConfig.Condition.LightlyPlayed,
				{ Name: "Played" } => _columnConfig.Condition.Played,
				{ Name: "Poor" } => _columnConfig.Condition.Poor,
				_ => ""
			};
		}
	}

	public class FinishConverter : ITypeConverter
	{
		public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) => text.Equals(_columnConfig.Finish.Foil);

		public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData) => value is true ? _columnConfig.Finish.Foil : _columnConfig.Finish.Normal;
	}
}

public record CsvConfig(string Quantity, string CardName, FinishConfiguration Finish, ConditionConfiguration Condition, string SetCode, string SetName, string SetNumber);
public record FinishConfiguration(string HeaderName, string Foil, string Normal, string Etched);
public record ConditionConfiguration(string? HeaderName, string Mint, string NearMint, string Excellent, string Good, string LightlyPlayed, string Played, string Poor);

public enum DeckFormat
{
	UNKNOWN,
	MOXFIELD,
	DRAGONSHIELD,
	DECKBOX,
	MANABOX,
	TCGPLAYER,
	CARDKINGDOM,
}

// TODO This is ugly but we need to register a C# Type in CsvHelper.RegisterClassMap, thus we can not easily use constructor?

public class DragonShieldMap : CsvToCardMap
{
	public DragonShieldMap() : base(DeckFormat.DRAGONSHIELD) { }
}



public class DeckboxMap : CsvToCardMap
{
	public DeckboxMap() : base(DeckFormat.DECKBOX) { }
}

public class ManaboxMap : CsvToCardMap
{
	public ManaboxMap() : base(DeckFormat.MANABOX) { }
}

public class TcgPlayerMap : CsvToCardMap
{
	public TcgPlayerMap() : base(DeckFormat.TCGPLAYER) { }
}

public class CardKingdomMap : ClassMap<PhysicalMtgCard>
{
	public CardKingdomMap()
	{

		Map(card => card.Printing.Card.Name).Name("Card Name");
		Map(card => card.Printing.Set.FullName).Name("Edition");
		Map(card => card.Printing.Set.Code).Ignore();
		Map(card => card.Printing.IdInSet).Ignore();
		Map(card => card.Foil).TypeConverter<FinishConverter>().Name("FOIL");
		Map(card => card.Count).Name("Quantity");
	}
}
