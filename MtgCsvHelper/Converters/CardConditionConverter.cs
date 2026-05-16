using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

namespace MtgCsvHelper.Converters;

public class CardConditionConverter(ConditionConfiguration configuration) : ITypeConverter
{
	readonly ConditionConfiguration _conditionConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
		// TODO: tighten to reject unmapped values once appsettings supports per-enum aliases —
		// real Moxfield exports use both "Near Mint" (binder export) and "NM" (collection export).
#pragma warning disable format
			return text switch
			{
				_ when text.MatchesConfig(_conditionConfig.Mint)			=> CardCondition.MINT,
				_ when text.MatchesConfig(_conditionConfig.NearMint)		=> CardCondition.NEAR_MINT,
				_ when text.MatchesConfig(_conditionConfig.Excellent)		=> CardCondition.EXCELLENT,
				_ when text.MatchesConfig(_conditionConfig.Good)			=> CardCondition.GOOD,
				_ when text.MatchesConfig(_conditionConfig.LightlyPlayed)	=> CardCondition.LIGHTLY_PLAYED,
				_ when text.MatchesConfig(_conditionConfig.Played)			=> CardCondition.PLAYED,
				_ when text.MatchesConfig(_conditionConfig.Poor)			=> CardCondition.POOR,
				_															=> CardCondition.UNKNOWN,
			};

		}

		public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData)
		{
			return (value as CardCondition) switch
			{
				{ Name: "Mint" }          => _conditionConfig.Mint,
				{ Name: "NearMint" }      => _conditionConfig.NearMint,
				{ Name: "Excellent" }     => _conditionConfig.Excellent,
				{ Name: "Good" }          => _conditionConfig.Good,
				{ Name: "LightlyPlayed" } => _conditionConfig.LightlyPlayed,
				{ Name: "Played" }        => _conditionConfig.Played,
				{ Name: "Poor" }          => _conditionConfig.Poor,
				_						  => ""
			};
		}
#pragma warning restore format
	}

