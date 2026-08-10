using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class CardConditionConverter(ConditionConfiguration configuration) : ITypeConverter
{
	readonly ConditionConfiguration _conditionConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		// Blank means the export carries no condition info — not a vocabulary error.
		if (string.IsNullOrWhiteSpace(text)) { return CardCondition.Unknown; }

		// Formats whose Mint/Excellent share NearMint's string declare them null, so only the NearMint arm matches — order-independent.
		return text switch
		{
			_ when text.MatchesConfig(_conditionConfig.Mint) => CardCondition.Mint,
			_ when text.MatchesConfig(_conditionConfig.NearMint) => CardCondition.NearMint,
			_ when text.MatchesConfig(_conditionConfig.Excellent) => CardCondition.Excellent,
			_ when text.MatchesConfig(_conditionConfig.Good) => CardCondition.Good,
			_ when text.MatchesConfig(_conditionConfig.LightlyPlayed) => CardCondition.LightlyPlayed,
			_ when text.MatchesConfig(_conditionConfig.Played) => CardCondition.Played,
			_ when text.MatchesConfig(_conditionConfig.Poor) => CardCondition.Poor,
			_ => throw new TypeConverterException(this, memberMapData, text, row.Context, $"Unrecognized Condition value '{text}'"),
		};
	}

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
	{
		// Mint/Excellent fall back to NearMint when the format config declares no separate tier (null → NearMint).
		return value is CardCondition condition
			? condition switch
			{
				CardCondition.Mint => _conditionConfig.Mint ?? _conditionConfig.NearMint,
				CardCondition.NearMint => _conditionConfig.NearMint,
				CardCondition.Excellent => _conditionConfig.Excellent ?? _conditionConfig.NearMint,
				CardCondition.Good => _conditionConfig.Good,
				CardCondition.LightlyPlayed => _conditionConfig.LightlyPlayed,
				CardCondition.Played => _conditionConfig.Played,
				CardCondition.Poor => _conditionConfig.Poor,
				CardCondition.Unknown => "",
				_ => "",
			}
			: "";
	}
}
