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
		// Cardmarket stubs (Name="") pass through SetInfo + Validator untouched; CardmarketIdEnricher resolves them last from live Scryfall data, which outranks our bundle.
		_pipeline =
		[
			new SetInfoEnricher(catalog),
			new CatalogValidator(catalog),
			new CardmarketIdEnricher(resolver),
		];
	}

	// Sync wrappers for non-Blazor callers; they block on the async path (Cardmarket forces async via Scryfall lookups).
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

		// Pipeline stages emit issues in mixed row order; sort once at the exit.
		var collection = new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = [.. rows.Select(r => r.Card)] };
		Log.Debug(collection.GenerateSummary());
		return new ParseResult(collection, [.. issues.OrderBy(i => i.RowNumber)]);

		static void CheckIfFirstLineCanBeIgnored(StreamReader stream)
		{
			// Peek at the first line; rewind unless it's a "sep=" marker row.
			var hasSeparatorInfoFirstLine = stream.ReadLine()?.Contains("sep=") ?? false;
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
		Log.Information($"Writing {cards.Sum(c => c.Count)} cards ({cards.Count} unique) to {outputFileName}");
		using var stream = File.Create(outputFileName);
		WriteCollectionCsv(cards, stream);
	}

	public void WriteCollectionCsv(IList<PhysicalMtgCard> cards, Stream outputStream)
	{
		var cfg = _factory.GetFormatConfig(_format)
			?? throw new InvalidOperationException($"Format '{_format}' configuration not found.");

		// Project new records when defaulting so the caller's cards stay immutable across writes.
		var rowsToWrite = cfg.RequiresWriteDefaults ? ApplyWriteDefaults(cards, cfg) : cards;

		if (cfg.Columns is null)
		{
			WriteModeledColumns(rowsToWrite, outputStream);

			return;
		}

		// Render the modeled columns, then re-emit them in the site's full native order for strict, order-sensitive importers.
		using var modeled = new MemoryStream();
		WriteModeledColumns(rowsToWrite, modeled);
		modeled.Position = 0;
		ProjectToNativeColumns(modeled, outputStream, cfg);
	}

	void WriteModeledColumns(IEnumerable<PhysicalMtgCard> rows, Stream outputStream)
	{
		using var writer = new StreamWriter(outputStream, leaveOpen: true);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		csv.Context.RegisterClassMap(_factory.GenerateWriteMap(_format));
		csv.WriteHeader<PhysicalMtgCard>();
		csv.NextRecord();
		csv.WriteRecords(rows);
		csv.Flush();
	}

	// Re-emits the modeled CSV in the declared native column order by header-name lookup, blanking unmodeled columns.
	static void ProjectToNativeColumns(Stream modeled, Stream outputStream, FormatConfig cfg)
	{
		var columns = cfg.Columns!;
		var csvCfg = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = cfg.Delimiter };

		using var reader = new StreamReader(modeled);
		using var csvIn = new CsvReader(reader, csvCfg);
		csvIn.Read();
		csvIn.ReadHeader();
		var modeledHeaders = csvIn.HeaderRecord!;

		// A modeled column absent from Columns would be silently dropped — config error, fail loudly.
		var undeclared = modeledHeaders.Where(h => !columns.Contains(h)).ToList();
		if (undeclared.Count > 0)
		{
			throw new InvalidOperationException(
				$"Format '{cfg.Name}' emits column(s) [{string.Join(", ", undeclared)}] not declared in its Columns list.");
		}

		var modeledSet = modeledHeaders.ToHashSet();

		using var writer = new StreamWriter(outputStream, leaveOpen: true);
		using var csvOut = new CsvWriter(writer, csvCfg);

		foreach (var column in columns) { csvOut.WriteField(column); }
		csvOut.NextRecord();

		while (csvIn.Read())
		{
			foreach (var column in columns)
			{
				csvOut.WriteField(modeledSet.Contains(column) ? csvIn.GetField(column) : string.Empty);
			}
			csvOut.NextRecord();
		}
		csvOut.Flush();
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
