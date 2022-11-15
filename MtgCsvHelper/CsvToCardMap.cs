using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using DevTrends.ConfigurationExtensions;
using Microsoft.Extensions.Configuration;

namespace MtgCsvHelper;

public class CsvToCardMap : ClassMap<PhysicalMtgCard>
{
	public static IConfiguration? ConfigFile { get; set; }
	static CsvConfiguration _columnConfig;

	public CsvToCardMap(DeckFormat format)
	{
		_columnConfig = ConfigFile?.Bind<CsvConfiguration>($"CsvConfigurations:{format}") ?? throw new ArgumentException($"ConfigFile not specified before attempting to use {nameof(CsvToCardMap)}");

		Map(card => card.Count).Name(_columnConfig.Quantity);
		Map(card => card.Printing.Card.Name).Name(_columnConfig.CardName);
		Map(card => card.Printing.Set.FullName).Name(_columnConfig.SetName);
		Map(card => card.Printing.Set.Code).Name(_columnConfig.SetCode);
		Map(card => card.Printing.IdInSet).Name(_columnConfig.SetNumber);
		Map(card => card.Condition).TypeConverter<CardConditionConverter>().Name(nameof(CsvConfiguration.Condition), _columnConfig.Condition.HeaderName);
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

		public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData) => (value is true) ? _columnConfig.Finish.Foil : _columnConfig.Finish.Normal;
	}
}

public record CsvConfiguration(string Quantity, string CardName, FinishConfiguration Finish, ConditionConfiguration Condition, string SetCode, string SetName, string SetNumber);
public record FinishConfiguration(string HeaderName, string Foil, string Normal, string Etched);
public record ConditionConfiguration(string? HeaderName, string Mint, string NearMint, string Excellent, string Good, string LightlyPlayed, string Played, string Poor);

public enum DeckFormat { UNKNOWN, MOXFIELD, DRAGONSHIELD, DECKBOX }

// TODO This is ugly but we need to register a C# Type in CsvHelper.RegisterClassMap, thus we can not easily use constructor?

public class DragonShieldMap : CsvToCardMap
{
	public DragonShieldMap() : base(DeckFormat.DRAGONSHIELD) { }
}

public class MoxfieldMap : CsvToCardMap
{
	public MoxfieldMap() : base(DeckFormat.MOXFIELD) { }
}

public class DeckboxMap : CsvToCardMap
{
	public DeckboxMap() : base(DeckFormat.DECKBOX) { }
}
