using System.Linq.Expressions;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Converters;
using MtgCsvHelper.Services;

namespace MtgCsvHelper.Maps;

// Bidirectional map for formats whose read and write shape are identical (every supported format except Card Kingdom).
// MapOptional hides the "if config is null skip, otherwise Map(...).Name(...).Optional()" boilerplate
// so the body reads as a flat list of mappings rather than a forest of null checks.
public class PhysicalCardMap : ClassMap<PhysicalMtgCard>
{
	public PhysicalCardMap(FormatConfig cfg, IMtgApi api)
	{
		Map(c => c.Count).Name(cfg.Quantity).Index(1);
		Map(c => c.Printing.Name)
			.TypeConverter(new CardNameConverter(cfg.CardName, api))
			.Name(cfg.CardName.HeaderName).Index(0);

		// At least one of SetCode / SetName is expected per format (sites differ on which they include).
		MapOptional(c => c.Printing.CollectorNumber, cfg.SetNumber);
		MapOptional(c => c.Printing.Set, cfg.SetCode)?.TypeConverter<UpperCaseConverter>();
		MapOptional(c => c.Printing.SetName, cfg.SetName);

		MapOptional(c => c.Condition, cfg.Condition, c => new CardConditionConverter(c));
		MapOptional(c => c.Foil, cfg.Finish, c => new FinishConverter(c));
		MapOptional(c => c.Language, cfg.Language, c => new LanguageConverter(c));
		MapOptional(c => c.PriceBought, cfg.PriceBought, c => new PriceConverter(c));
	}

	MemberMap<PhysicalMtgCard, TMember>? MapOptional<TMember>(
		Expression<Func<PhysicalMtgCard, TMember?>> property,
		string? headerName)
		=> headerName is null ? null : Map(property).Name(headerName).Optional();

	MemberMap<PhysicalMtgCard, TMember>? MapOptional<TMember, TConfig>(
		Expression<Func<PhysicalMtgCard, TMember?>> property,
		TConfig? config,
		Func<TConfig, ITypeConverter> converterFactory)
		where TConfig : class, IHeaderConfig
		=> config is null ? null : Map(property).TypeConverter(converterFactory(config)).Name(config.HeaderName).Optional();
}
