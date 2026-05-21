namespace MtgCsvHelper;

public record FormatConfig(
	string Name,
	string Quantity,
	// Either CardName or CardmarketId (or in the future another card-identifier kind) must be set.
	// Most formats identify cards by name + set; Cardmarket identifies by its internal product ID and
	// requires Scryfall reverse-lookup to fill in name/set/etc. during enrichment.
	CardNameConfiguration? CardName = null,
	string? CardmarketId = null,
	string? SetNumber = null,
	string? SetCode = null,
	string? SetName = null,
	FinishConfiguration? Finish = null,
	ConditionConfiguration? Condition = null,
	LanguageConfiguration? Language = null,
	PriceConfiguration? PriceBought = null,
	DateBoughtConfiguration? DateBought = null,
	string? FolderName = null,
	string? TradeQuantity = null,
	// True if the importer rejects rows with null cells in declared columns (Dragon Shield).
	// Triggers a pre-write defaulting pass that fills nulls. Coupling: also expects
	// PriceBought to be configured (its Currency drives the default price) and a
	// DefaultFolderName to be set.
	bool RequiresWriteDefaults = false,
	// Folder label stamped on rows whose Folder is null/empty when RequiresWriteDefaults
	// is true. No effect otherwise.
	string DefaultFolderName = "Imported",
	// CSV delimiter. Most sites use comma; Cardmarket exports use semicolon.
	string Delimiter = ","
	)
{
	public Currency Currency => Currency.FromString(PriceBought?.Currency);

	// Throws when the config is structurally invalid for use as a read or write map.
	// Add rules here as new invariants emerge (e.g. delimiter sanity, language-mappings completeness).
	public void Validate()
	{
		if (CardName is null && CardmarketId is null)
		{
			throw new InvalidOperationException($"Format '{Name}' must specify either a CardName or a CardmarketId column.");
		}
	}
};

// Shared shape for the rich sub-record configurations: all expose the CSV header they bind to.
// Lets the map base class register them generically without per-type branching.
public interface IHeaderConfig
{
	string HeaderName { get; }
}

public record CardNameConfiguration(
	string HeaderName,
	bool ShortNames = false,
	bool EncodeToken = false) : IHeaderConfig;

public record FinishConfiguration(
	string HeaderName,
	string Foil,
	string Normal,
	string? Etched = null) : IHeaderConfig;

// Mint and Excellent are nullable so a format with no separate tier for them can declare
// `null` instead of duplicating another condition's string. The write-side falls back to
// NearMint when these are null; the read-side simply doesn't match (CsvMatch.MatchesConfig
// returns false for null), so duplicate-string collisions don't depend on switch arm order.
public record ConditionConfiguration(
	string HeaderName,
	string NearMint,
	string Good,
	string LightlyPlayed,
	string Played,
	string Poor,
	string? Mint = null,
	string? Excellent = null) : IHeaderConfig;

public record LanguageConfiguration(
	string HeaderName,
	LanguageMappings Mappings) : IHeaderConfig;

public record LanguageMappings(
	string en,
	string es,
	string fr,
	string de,
	string it,
	string pt,
	string ja,
	string ko,
	string ru,
	string zht,
	string zhs);

public record PriceConfiguration(
	string HeaderName,
	string Currency,
	string CurrencySymbol) : IHeaderConfig;

// Formats is an ordered list passed straight to CsvHelper's TypeConverterOption.Format(...):
// the FIRST entry is used for writes, ALL entries are tried in order on read. Lets a format
// emit a canonical ISO date but still parse a vendor's historical export shape (e.g. Dragon
// Shield's own exports use M/d/yyyy alongside the yyyy-MM-dd it accepts on import).
public record DateBoughtConfiguration(
	string HeaderName,
	string[]? Formats = null) : IHeaderConfig
{
	public string[] FormatsOrDefault => Formats ?? ["yyyy-MM-dd"];
}
