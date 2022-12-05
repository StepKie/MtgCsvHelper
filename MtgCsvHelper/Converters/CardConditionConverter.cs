using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class CardConditionConverter : ITypeConverter
{
	readonly ConditionConfiguration _conditionConfig;

	public CardConditionConverter(ConditionConfiguration configuration) => _conditionConfig = configuration;

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData)
	{
#pragma warning disable format
			return text switch
			{
				_ when _conditionConfig.Mint.Equals(text)			=> CardCondition.MINT,
				_ when _conditionConfig.NearMint.Equals (text)		=> CardCondition.NEAR_MINT,
				_ when _conditionConfig.Excellent.Equals(text)		=> CardCondition.EXCELLENT,
				_ when _conditionConfig.Good.Equals(text)			=> CardCondition.GOOD,
				_ when _conditionConfig.LightlyPlayed.Equals(text)	=> CardCondition.LIGHTLY_PLAYED,
				_ when _conditionConfig.Played.Equals(text)			=> CardCondition.PLAYED,
				_ when _conditionConfig.Poor.Equals(text)			=> CardCondition.POOR,
				_													=> CardCondition.UNKNOWN,

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
				_						  => null
			};
		}
#pragma warning restore format
	}

