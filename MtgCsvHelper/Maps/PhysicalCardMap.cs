using System.Linq.Expressions;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Converters;

namespace MtgCsvHelper.Maps;

// Bidirectional map for formats whose read and write shape are identical (every supported format except Card Kingdom).
// MapOptional hides the "if config is null skip, otherwise Map(...).Name(...).Optional()" boilerplate
// so the body reads as a flat list of mappings rather than a forest of null checks.
public class PhysicalCardMap : ClassMap<PhysicalMtgCard>
{
	public PhysicalCardMap(FormatConfig cfg, IReferenceCardCatalog catalog, ITypeConverter? setCodeConverter = null)
	{
		// Explicit indices override CsvHelper's clustering of nested-member maps
		// (Printing.Name, Printing.Set, …) — registration order alone interleaves
		// columns wrong. Unmapped indices are skipped on write, so gaps are fine.

		if (cfg.FolderName is not null) { Map(c => c.Folder).Name(cfg.FolderName).Index(0).TypeConverter<EmptyAsNullStringConverter>(); }

		Map(c => c.Count).Name(cfg.Quantity).Index(1);

		if (cfg.TradeQuantity is not null) { Map(c => c.TradeQuantity).Name(cfg.TradeQuantity).Index(2); }

		// Name and Cardmarket-id share index 3 — no format declares both.
		if (cfg.CardName is not null)
		{
			Map(c => c.Printing.Name)
				.TypeConverter(new CardNameConverter(cfg.CardName, catalog))
				.Name(cfg.CardName.HeaderName).Index(3);
		}
		if (cfg.CardmarketId is not null)
		{
			// Stub Card object: only CardMarketId is filled here; name/set/etc. get resolved
			// later in MtgCardCsvHandler.EnrichByCardmarketIdAsync via batched Scryfall lookup.
			Map(c => c.Printing.CardMarketId).Name(cfg.CardmarketId).Index(3);
		}

		// At least one of SetCode / SetName is expected per format (sites differ on which they include).
		// CollectorNumberConverter is applied universally (not MTGO-specific) — it's a no-op for any
		// collector number without a "/", and "/" doesn't appear in Scryfall data.
		MapOptional(c => c.Printing.Set, cfg.SetCode)?.TypeConverter(setCodeConverter ?? new UpperCaseConverter()).Index(4);
		MapOptional(c => c.Printing.SetName, cfg.SetName)?.Index(5);
		MapOptional(c => c.Printing.CollectorNumber, cfg.SetNumber)?.TypeConverter<CollectorNumberConverter>().Index(6);

		MapOptional(c => c.Condition, cfg.Condition, c => new CardConditionConverter(c))?.Index(7);
		MapOptional(c => c.Finish, cfg.Finish, c => new FinishConverter(c))?.Index(8);
		MapOptional(c => c.Language, cfg.Language, c => new LanguageConverter(c))?.Index(9);
		MapOptional(c => c.PriceBought, cfg.PriceBought, c => new PriceConverter(c))?.Index(10);

		if (cfg.DateBought is not null)
		{
			Map(c => c.DateBought).Name(cfg.DateBought.HeaderName).Index(11)
				.TypeConverterOption.Format(cfg.DateBought.FormatsOrDefault);
		}
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
