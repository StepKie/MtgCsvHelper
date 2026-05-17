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
		// Order matters: Cardmarket stubs (Name="") must be resolved before SetInfo and CatalogValidator
		// can do anything with them. SetInfo runs before CatalogValidator so the canonical Set is in
		// place when the catalog lookup happens. Note: this ordering means resolved Cardmarket cards
		// also go through SetInfo + Validator — the old inline code ran Cardmarket resolution last
		// and resolved cards bypassed both. Intentional improvement: validation now catches catalog
		// drift in Scryfall-resolved data too.
		_pipeline =
		[
			new CardmarketIdEnricher(resolver),
			new SetInfoEnricher(catalog),
			new CatalogValidator(catalog),
		];
	}

	// Sync wrappers — fine for non-Blazor callers and for formats that don't need network I/O.
	// Cardmarket forces async (Scryfall lookups), so the sync path blocks on the async path.
	public ParseResult ParseCollectionCsv(string csvFilePath) => ParseCollectionCsvAsync(csvFilePath).GetAwaiter().GetResult();
	public ParseResult ParseCollectionCsv(Stream csvStream) => ParseCollectionCsvAsync(csvStream).GetAwaiter().GetResult();

	public Task<ParseResult> ParseCollectionCsvAsync(string csvFilePath, CancellationToken ct = default) => ParseCollectionCsvAsync(File.OpenRead(csvFilePath), ct);

	public async Task<ParseResult> ParseCollectionCsvAsync(Stream csvStream, CancellationToken ct = default)
	{
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
			try
			{
				var card = csv.GetRecord<PhysicalMtgCard>();
				if (card.Count <= 0)
				{
					issues.Add(new ImportIssue(
						IssueSeverity.Error,
						rowNum,
						$"Count must be positive (got {card.Count})",
						RawContent: csv.Parser.RawRecord?.TrimEnd()));
					continue;
				}
				rows.Add(new ParsedRow(card, rowNum));
			}
			catch (TypeConverterException tcex)
			{
				var memberName = tcex.MemberMapData?.Member?.Name ?? "?";
				var value = tcex.Text ?? "";
				issues.Add(new ImportIssue(
					IssueSeverity.Error,
					rowNum,
					$"Invalid value '{value}' for column '{memberName}'",
					RawContent: csv.Parser.RawRecord?.TrimEnd()));
			}
			catch (Exception ex)
			{
				issues.Add(new ImportIssue(
					IssueSeverity.Error,
					rowNum,
					ex.Message,
					RawContent: csv.Parser.RawRecord?.TrimEnd()));
			}
		}

		// Run the post-parse pipeline. Each step mutates rows/issues in place (transforms or drops).
		foreach (var enricher in _pipeline)
		{
			await enricher.EnrichAsync(rows, issues, ct);
		}

		var collection = new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = [.. rows.Select(r => r.Card)] };
		Log.Debug(collection.GenerateSummary());
		return new ParseResult(collection, issues);

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
		using var writer = new StreamWriter(outputStream, leaveOpen: true);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		csv.Context.RegisterClassMap(_factory.GenerateWriteMap(_format));
		csv.WriteHeader<PhysicalMtgCard>();
		csv.NextRecord();
		csv.WriteRecords(cards);
		csv.Flush();
	}
}
