using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class RarityConverter(RarityConfiguration configuration) : ITypeConverter
{
	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		// Unrecognized values degrade to Unknown rather than erroring: rarity is catalog metadata and CatalogValidator re-stamps it on resolve.
		return text switch
		{
			_ when text.MatchesConfig(configuration.Common) => CardRarity.Common,
			_ when text.MatchesConfig(configuration.Uncommon) => CardRarity.Uncommon,
			_ when text.MatchesConfig(configuration.Rare) => CardRarity.Rare,
			_ when text.MatchesConfig(configuration.Special) => CardRarity.Special,
			_ when text.MatchesConfig(configuration.Mythic) => CardRarity.Mythic,
			_ when text.MatchesConfig(configuration.Bonus) => CardRarity.Bonus,
			_ => CardRarity.Unknown,
		};
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value switch
	{
		CardRarity.Common => configuration.Common,
		CardRarity.Uncommon => configuration.Uncommon,
		CardRarity.Rare => configuration.Rare,
		CardRarity.Special => configuration.Special,
		CardRarity.Mythic => configuration.Mythic,
		CardRarity.Bonus => configuration.Bonus,
		_ => "",
	};
}
