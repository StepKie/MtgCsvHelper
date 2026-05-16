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
		//
		// Duplicate-string collisions are handled at the config level: formats whose Mint or
		// Excellent collapses to the same string as NearMint declare those fields as `null` in
		// appsettings.json, so only the NearMint arm matches and switch order is irrelevant.
		// See CardConditionConverterTests.AmbiguousString_ResolvesToNearMint for the invariant.
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
			// Mint and Excellent fall back to NearMint when the format has no separate tier for
			// them (Archidekt, Moxfield, TopDecked, Deckbox). The format's appsettings.json
			// declares those entries as null; this `??` chain emits the NearMint string instead.
			return (value as CardCondition) switch
			{
				{ Name: "Mint" }          => _conditionConfig.Mint      ?? _conditionConfig.NearMint,
				{ Name: "NearMint" }      => _conditionConfig.NearMint,
				{ Name: "Excellent" }     => _conditionConfig.Excellent ?? _conditionConfig.NearMint,
				{ Name: "Good" }          => _conditionConfig.Good,
				{ Name: "LightlyPlayed" } => _conditionConfig.LightlyPlayed,
				{ Name: "Played" }        => _conditionConfig.Played,
				{ Name: "Poor" }          => _conditionConfig.Poor,
				_						  => ""
			};
		}
#pragma warning restore format
	}

