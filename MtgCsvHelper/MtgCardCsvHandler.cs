using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Enrichment;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly CardMapFactory _factory;
	readonly string _format;
	readonly IReadOnlyList<IEnricher> _pipeline;

	public MtgCardCsvHandler(IReferenceCardCatalog catalog, ICardmarketResolver resolver, IConfiguration config, string format)
	{
		_format = format;
		_factory = new CardMapFactory(config, catalog);
		// Order matches the prior inline implementation:
		// 1. SetInfo + Validator run on non-stub rows (Cardmarket stubs have Name="" and are
		//    skipped by both via their leading null-name guards).
		// 2. CardmarketIdEnricher runs last, resolving stubs from the Scryfall network/catalog.
		// Cardmarket-resolved cards intentionally bypass CatalogValidator: the resolver's data
		// IS Scryfall data, so validating it against our possibly-stale local bundle would drop
		// legitimate cards released after our last bundle refresh.
		_pipeline =
		[
			new SetInfoEnricher(catalog),
			new CatalogValidator(catalog),
			new CardmarketIdEnricher(resolver),
		];
	}

	// Sync wrappers — fine for non-Blazor callers and for formats that don't need network I/O.
	// Cardmarket forces async (Scryfall lookups), so the sync path blocks on the async path.
	public ParseResult ParseCollectionCsv(string csvFilePath) => ParseCollectionCsvAsync(csvFilePath).GetAwaiter().GetResult();
	public ParseResult ParseCollectionCsv(Stream csvStream) => ParseCollectionCsvAsync(csvStream).GetAwaiter().GetResult();

	public Task<ParseResult> ParseCollectionCsvAsync(string csvFilePath, CancellationToken ct = default) => ParseCollectionCsvAsync(File.OpenRead(csvFilePath), ct);

	/// <summary> Parses a collection CSV. <paramref name="csvStream"/> must be seekable: the parser peeks at the first line for a "sep=" marker and rewinds. </summary>
	public async Task<ParseResult> ParseCollectionCsvAsync(Stream csvStream, CancellationToken ct = default)
	{
		if (!csvStream.CanSeek) { throw new ArgumentException("Stream must be seekable", nameof(csvStream)); }

		Log.Information($"Parsing input format {_format} ...");
		using var reader = new StreamReader(csvStream);
		CheckIfFirstLineCanBeIgnored(reader);

		var formatConfig = _factory.GetFormatConfig(_format)
			?? throw new InvalidOperationException($"Format '{_format}' configuration not found.");

		using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
		{
			MissingFieldFound = null,
			Delimiter = formatConfig.Delimiter,
		});
		csv.Context.RegisterClassMap(_factory.GenerateReadMap(_format));

		if (!await csv.ReadAsync())
		{
			// Empty file — no header, no rows.
			return new ParseResult(new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = [] }, []);
		}
		csv.ReadHeader();
		csv.ValidateHeader<PhysicalMtgCard>(); // throws HeaderValidationException if non-Optional fields are missing

		var rows = new List<ParsedRow>();
		var issues = new List<ImportIssue>();

		// CsvHelper's ReadAsync doesn't take a CT, so cancellation is checked per-row instead.
		while (await csv.ReadAsync())
		{
			ct.ThrowIfCancellationRequested();
			// Skip rows that are blank or contain only delimiters/whitespace.
			if (csv.Parser.Record is null || csv.Parser.Record.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			int rowNum = csv.Parser.Row;
			var rawContent = csv.Parser.RawRecord?.TrimEnd();
			try
			{
				var card = csv.GetRecord<PhysicalMtgCard>();
				if (card.Count <= 0)
				{
					issues.Add(new ImportIssue(
						IssueSeverity.Error,
						rowNum,
						$"Count must be positive (got {card.Count})",
						RawContent: rawContent));
					continue;
				}
				rows.Add(new ParsedRow(card, rowNum, rawContent));
			}
			catch (TypeConverterException tcex)
			{
				var memberName = tcex.MemberMapData?.Member?.Name ?? "?";
				var value = tcex.Text ?? "";
				issues.Add(new ImportIssue(
					IssueSeverity.Error,
					rowNum,
					$"Invalid value '{value}' for column '{memberName}'",
					RawContent: rawContent));
			}
			catch (Exception ex)
			{
				issues.Add(new ImportIssue(
					IssueSeverity.Error,
					rowNum,
					ex.Message,
					RawContent: rawContent));
			}
		}

		// Run the post-parse pipeline. Each step mutates rows/issues in place (transforms or drops).
		foreach (var enricher in _pipeline)
		{
			await enricher.EnrichAsync(rows, issues, ct);
		}

		// Pipeline stages emit issues in mixed order (parse loop ascending, PerCardEnricher
		// reverse iteration descending, batch enrichers ascending). Sort by RowNumber once at
		// the exit so consumers always see ascending row order.
		var collection = new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = [.. rows.Select(r => r.Card)] };
		Log.Debug(collection.GenerateSummary());
		return new ParseResult(collection, [.. issues.OrderBy(i => i.RowNumber)]);

		static void CheckIfFirstLineCanBeIgnored(StreamReader stream)
		{
			// "Peek" into the first row, and if it is not a separator info row, reset the stream. (Found no more elegant way to do this)
			var hasSeparatorInfoFirstLine = stream.ReadLine()?.Contains("sep=") ?? false;
			// Reset the stream to the original state if the first line is not a separator info line
			if (!hasSeparatorInfoFirstLine)
			{
				stream.BaseStream.Position = 0;
				stream.DiscardBufferedData();
			}
		}
	}

	public void WriteCollectionCsv(IList<PhysicalMtgCard> cards, string? outputFileName = null)
	{
		outputFileName ??= $"{_format.ToLower()}-output-{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
		Log.Information($"Writing {cards.Sum(c => c.Count)} cards ({cards.Count} unique) cards to {outputFileName}");
		using var stream = File.Create(outputFileName);
		WriteCollectionCsv(cards, stream);
	}

	public void WriteCollectionCsv(IList<PhysicalMtgCard> cards, Stream outputStream)
	{
		var cfg = _factory.GetFormatConfig(_format)
			?? throw new InvalidOperationException($"Format '{_format}' configuration not found.");

		// Project into new records when defaulting so the caller's cards stay immutable —
		// avoids the second-write-sees-first-write-defaults hazard.
		var rowsToWrite = cfg.RequiresWriteDefaults ? ApplyWriteDefaults(cards, cfg) : cards;

		using var writer = new StreamWriter(outputStream, leaveOpen: true);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		csv.Context.RegisterClassMap(_factory.GenerateWriteMap(_format));
		csv.WriteHeader<PhysicalMtgCard>();
		csv.NextRecord();
		csv.WriteRecords(rowsToWrite);
		csv.Flush();
	}

	static IEnumerable<PhysicalMtgCard> ApplyWriteDefaults(IEnumerable<PhysicalMtgCard> cards, FormatConfig cfg)
	{
		var currency = Currency.FromString(cfg.PriceBought?.Currency);
		return cards.Select(c => c with
		{
			PriceBought = c.PriceBought ?? new Money(0m, currency),
			DateBought = c.DateBought ?? DateTime.Today,
			Folder = string.IsNullOrEmpty(c.Folder) ? cfg.DefaultFolderName : c.Folder,
		});
	}
}
