using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly CardMapFactory _factory;
	readonly IReferenceCardCatalog _catalog;
	readonly IMtgApi _api;
	readonly string _format;

	public MtgCardCsvHandler(IReferenceCardCatalog catalog, IMtgApi api, IConfiguration config, string format)
	{
		_format = format;
		_factory = new CardMapFactory(config, catalog);
		_catalog = catalog;
		_api = api;
	}

	// Sync wrappers — fine for non-Blazor callers and for formats that don't need network I/O.
	// Cardmarket forces async (Scryfall lookups), so the sync path blocks on the async path.
	public ParseResult ParseCollectionCsv(string csvFilePath) => ParseCollectionCsvAsync(csvFilePath).GetAwaiter().GetResult();
	public ParseResult ParseCollectionCsv(Stream csvStream) => ParseCollectionCsvAsync(csvStream).GetAwaiter().GetResult();

	public Task<ParseResult> ParseCollectionCsvAsync(string csvFilePath) => ParseCollectionCsvAsync(File.OpenRead(csvFilePath));

	public async Task<ParseResult> ParseCollectionCsvAsync(Stream csvStream)
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

		var cards = new List<PhysicalMtgCard>();
		var rowNumbers = new List<int>(); // parallel to cards; needed for issue reporting in the post-parse enrichment step
		var issues = new List<ImportIssue>();

		while (await csv.ReadAsync())
		{
			// Skip rows that are blank or contain only delimiters/whitespace.
			if (csv.Parser.Record is null || csv.Parser.Record.All(string.IsNullOrWhiteSpace))
			{
				continue;
			}

			int rowNum = csv.Parser.Row;
			try
			{
				var card = csv.GetRecord<PhysicalMtgCard>();
				// For non-cardmarket formats, the printing's Name is set during parse; backfill set info now.
				// For cardmarket-style stubs (Name empty, CardMarketId set), defer to the cardmarket enricher.
				if (!string.IsNullOrEmpty(card.Printing.Name))
				{
					EnrichSetInfo(card, _catalog, issues, rowNum);
				}
				cards.Add(card);
				rowNumbers.Add(rowNum);
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

		// Post-parse enrichment: fill in stubbed cards (Cardmarket) by batched Scryfall lookup.
		await EnrichByCardmarketIdAsync(cards, rowNumbers, issues);

		var collection = new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = cards };
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

	static void EnrichSetInfo(PhysicalMtgCard card, IReferenceCardCatalog catalog, List<ImportIssue> issues, int rowNum)
	{
		var p = card.Printing;
		bool hadSetCode = p.Set is not null;
		bool hadSetName = p.SetName is not null;

		// Both directions are O(1) — the catalog maintains forward (code → name) and reverse (name → code) indexes.
		if (hadSetCode) { p.SetName ??= catalog.GetSetNameByCode(p.Set!); }
		if (hadSetName) { p.Set ??= catalog.GetSetCodeByName(p.SetName!); }

		if (!hadSetCode && !hadSetName)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, rowNum, "No set information found in row", CardName: p.Name));
		}
		else if (hadSetCode && p.SetName is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, rowNum, $"Set code '{p.Set}' not found in Scryfall data", CardName: p.Name));
		}
		else if (hadSetName && p.Set is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Warning, rowNum, $"Set name '{p.SetName}' not found in Scryfall data", CardName: p.Name));
		}
	}

	// For cards whose Printing was parsed as a stub (only CardMarketId set), batch-resolve the
	// full Scryfall card by cardmarket_id and fill in name/set/setName/collectorNumber.
	// IDs not found in Scryfall produce an ImportIssue.Warning per affected card.
	async Task EnrichByCardmarketIdAsync(IList<PhysicalMtgCard> cards, IList<int> rowNumbers, List<ImportIssue> issues)
	{
		// CardMarketId is `int` (not `int?`) in the Scryfall library, so 0 acts as the "unset" sentinel —
		// real cardmarket_ids are always positive in Scryfall data.
		var pending = new List<(PhysicalMtgCard Card, int RowNum)>();
		for (int i = 0; i < cards.Count; i++)
		{
			var c = cards[i];
			if (c.Printing.CardMarketId > 0 && string.IsNullOrEmpty(c.Printing.Name))
			{
				pending.Add((c, rowNumbers[i]));
			}
		}

		if (pending.Count == 0) return;

		var ids = pending.Select(x => x.Card.Printing.CardMarketId).Distinct().ToList();
		var resolved = await _api.GetCardsByCardmarketIdsAsync(ids);

		var unresolved = new List<PhysicalMtgCard>();
		foreach (var entry in pending)
		{
			var id = entry.Card.Printing.CardMarketId;
			if (resolved.TryGetValue(id, out var full))
			{
				entry.Card.Printing.Name = full.Name;
				entry.Card.Printing.Set = full.Set;
				entry.Card.Printing.SetName = full.SetName;
				entry.Card.Printing.CollectorNumber = full.CollectorNumber;
			}
			else
			{
				// Without a name/set, we can't write a meaningful row — drop the card.
				// This is data loss (Error), not just degraded fidelity (Warning).
				issues.Add(new ImportIssue(IssueSeverity.Error, entry.RowNum, $"Cardmarket ID {id} not found in Scryfall data — card skipped"));
				unresolved.Add(entry.Card);
			}
		}
		foreach (var dropped in unresolved)
		{
			cards.Remove(dropped);
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
