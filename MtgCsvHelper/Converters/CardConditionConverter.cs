using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class CardConditionConverter : ITypeConverter
{
	readonly ConditionConfiguration _conditionConfig;

	public CardConditionConverter(ConditionConfiguration configuration) => _conditionConfig = configuration;

	public object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData)
	{
#pragma warning disable format
			return text switch
			{
				_ when text.Equals(_conditionConfig.Mint)          => CardCondition.MINT,
				_ when text.Equals(_conditionConfig.NearMint)      => CardCondition.NEAR_MINT,
				_ when text.Equals(_conditionConfig.Excellent)     => CardCondition.EXCELLENT,
				_ when text.Equals(_conditionConfig.Good)          => CardCondition.GOOD,
				_ when text.Equals(_conditionConfig.LightlyPlayed) => CardCondition.LIGHTLY_PLAYED,
				_ when text.Equals(_conditionConfig.Played)        => CardCondition.PLAYED,
				_ when text.Equals(_conditionConfig.Poor)          => CardCondition.POOR,
				_                                                  => "",

			};

		}

		public string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
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
				_                         => ""
			};
		}
#pragma warning restore format
	}

