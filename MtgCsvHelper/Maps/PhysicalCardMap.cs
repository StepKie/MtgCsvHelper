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
	public PhysicalCardMap(FormatConfig cfg, IReferenceCardCatalog catalog)
	{
		// Explicit indices override CsvHelper's clustering of nested-member maps (registration order alone interleaves columns wrong); unmapped indices are skipped on write.
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
			// Stub Card: only CardMarketId is set here; CardmarketIdEnricher resolves the rest via batched Scryfall lookup.
			Map(c => c.Printing.CardMarketId).Name(cfg.CardmarketId).Index(3);
		}

		// CollectorNumberConverter is applied universally: a no-op for numbers without "/", which Scryfall data never contains.
		var setCodeMap = MapOptional(c => c.Printing.Set, cfg.SetCode);
		if (setCodeMap is not null) { ConfigureSetCode(setCodeMap.Index(4)); }
		var setNameMap = MapOptional(c => c.Printing.SetName, cfg.SetName);
		if (setNameMap is not null) { ConfigureSetName(setNameMap.Index(5)); }
		MapOptional(c => c.Printing.CollectorNumber, cfg.SetNumber)?.TypeConverter<CollectorNumberConverter>().Index(6);

		// Printing.Id is a non-nullable Guid, so it can't go through MapOptional (which expects a nullable member).
		if (cfg.ScryfallId is not null) { Map(c => c.Printing.Id).Name(cfg.ScryfallId).TypeConverter<ScryfallIdConverter>().Index(12).Optional(); }

		// Catalog-stamped metadata columns: CatalogValidator fills the model fields when the printing resolves.
		MapOptional(c => c.Rarity, cfg.Rarity, c => new RarityConverter(c))?.Index(13);
		MapOptional(c => c.Printing.MultiverseIds, cfg.MultiverseId)?.TypeConverter<MultiverseIdsConverter>().Index(14);
		MapOptional(c => c.Printing.TcgplayerId, cfg.TcgplayerId)?.TypeConverter<TcgplayerIdConverter>().Index(15);

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

	/// <summary>Configures the Set Code column; default uppercases the canonical Scryfall code. Override to swap the read converter for formats with proprietary codes (Deckbox, Dragon Shield).</summary>
	protected virtual void ConfigureSetCode(MemberMap<PhysicalMtgCard, string> map) => map.TypeConverter<UpperCaseConverter>();

	/// <summary>Configures the Set Name column; default is pass-through. Override to emit a format's curated edition names (Deckbox aliases, Dragon Shield guild kits).</summary>
	protected virtual void ConfigureSetName(MemberMap<PhysicalMtgCard, string> map) { }

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
