using System.Globalization;
using System.Text;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly CardMapFactory _factory;
	readonly IReferenceCardCatalog _catalog;
	readonly ICardmarketResolver _resolver;
	readonly string _format;

	public MtgCardCsvHandler(IReferenceCardCatalog catalog, ICardmarketResolver resolver, IConfiguration config, string format)
	{
		_format = format;
		_factory = new CardMapFactory(config, catalog);
		_catalog = catalog;
		_resolver = resolver;
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

		var cards = new List<PhysicalMtgCard>();
		var rowNumbers = new List<int>(); // parallel to cards; needed for issue reporting in the post-parse enrichment step
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
				// For non-cardmarket formats, the printing's Name is set during parse; backfill set info now.
				// For cardmarket-style stubs (Name empty, CardMarketId set), defer to the cardmarket enricher.
				if (!string.IsNullOrEmpty(card.Printing.Name))
				{
					EnrichSetInfo(card, _catalog, issues, rowNum);
					if (!IsValidPrinting(card, _catalog, issues, rowNum))
					{
						continue;
					}
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
		await EnrichByCardmarketIdAsync(cards, rowNumbers, issues, ct);

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

	// Validates a parsed row against the Scryfall catalog. Issues an Error and returns false
	// (so the caller drops the row) when (Set, CollectorNumber) doesn't resolve, the name
	// doesn't match the resolved printing, or Foil is claimed for a non-foil-only printing.
	// Rows without enough info to look up (no Set or no CollectorNumber) pass through —
	// EnrichSetInfo already flagged them as Warning, and dropping them on top would be noise.
	static bool IsValidPrinting(PhysicalMtgCard card, IReferenceCardCatalog catalog, List<ImportIssue> issues, int rowNum)
	{
		var p = card.Printing;
		if (string.IsNullOrEmpty(p.Set) || string.IsNullOrEmpty(p.CollectorNumber)) { return true; }

		// Scryfall stores set codes lowercase; FindBySetAndCollectorNumber's key is case-sensitive
		// on the tuple. Imported CSVs use either case (Moxfield uppercase, Topdecked lowercase).
		var match = catalog.FindBySetAndCollectorNumber(p.Set.ToLowerInvariant(), p.CollectorNumber);
		if (match is null)
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, rowNum,
				$"No printing at {p.Set.ToUpperInvariant()} #{p.CollectorNumber} in Scryfall data", CardName: p.Name));
			return false;
		}

		if (!NamesMatch(p.Name, match.Name, catalog))
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, rowNum,
				$"Name '{p.Name}' does not match printing at {p.Set.ToUpperInvariant()} #{p.CollectorNumber} ('{match.Name}')",
				CardName: p.Name));
			return false;
		}

		if (card.Foil == true && !HasFoilFinish(match.Finishes))
		{
			issues.Add(new ImportIssue(IssueSeverity.Error, rowNum,
				"This printing was not released in foil", CardName: p.Name));
			return false;
		}

		return true;
	}

	static bool NamesMatch(string imported, string referenceName, IReferenceCardCatalog catalog)
	{
		if (EqualsNormalized(imported, referenceName)) { return true; }
		// Front-face-only imports (Moxfield's ShortNames=false formats) match the full Scryfall name.
		var expanded = catalog.ExpandFrontFaceToFullName(imported);
		return expanded is not null && EqualsNormalized(expanded, referenceName);
	}

	// Compares with Unicode diacritic stripping (NFD + remove combining marks). TCGPlayer and a
	// few other sites normalize "Lim-Dûl's Vault" → "Lim-Dul's Vault" on export; Scryfall keeps the
	// diacritic. Matching naively would reject every accented card. Done in one pass on both sides.
	static bool EqualsNormalized(string a, string b) =>
		string.Equals(StripDiacritics(a), StripDiacritics(b), StringComparison.OrdinalIgnoreCase);

	static string StripDiacritics(string s)
	{
		var decomposed = s.Normalize(NormalizationForm.FormD);
		var sb = new StringBuilder(decomposed.Length);
		foreach (var c in decomposed)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) { sb.Append(c); }
		}
		return sb.ToString();
	}

	static bool HasFoilFinish(IReadOnlyList<string> finishes) =>
		finishes.Any(f => string.Equals(f, "foil", StringComparison.OrdinalIgnoreCase)
		               || string.Equals(f, "etched", StringComparison.OrdinalIgnoreCase));

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
	async Task EnrichByCardmarketIdAsync(IList<PhysicalMtgCard> cards, IList<int> rowNumbers, List<ImportIssue> issues, CancellationToken ct)
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
		var resolved = await _resolver.ResolveAsync(ids, ct);

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
