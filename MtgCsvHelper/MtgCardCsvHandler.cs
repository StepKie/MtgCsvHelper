using System.Globalization;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly CardMapFactory _factory;
	readonly IMtgApi _api;
	readonly string _format;

	public MtgCardCsvHandler(IMtgApi api, IConfiguration config, string format)
	{
		_format = format;
		_factory = new CardMapFactory(config, api);
		_api = api;
	}

	public ParseResult ParseCollectionCsv(string csvFilePath) => ParseCollectionCsv(File.OpenRead(csvFilePath));

	public ParseResult ParseCollectionCsv(Stream csvStream)
	{
		Log.Information($"Parsing input format {_format} ...");
		using var reader = new StreamReader(csvStream);
		CheckIfFirstLineCanBeIgnored(reader);

		using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { MissingFieldFound = null });
		csv.Context.RegisterClassMap(_factory.GenerateReadMap(_format));

		if (!csv.Read())
		{
			// Empty file — no header, no rows. Return empty result rather than treating as an error.
			return new ParseResult(new Collection { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = [] }, []);
		}
		csv.ReadHeader();
		csv.ValidateHeader<PhysicalMtgCard>(); // throws HeaderValidationException if non-Optional fields are missing

		var cards = new List<PhysicalMtgCard>();
		var issues = new List<ImportIssue>();
		var sets = _api.GetSets();

		while (csv.Read())
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
				EnrichSetInfo(card, sets, issues, rowNum);
				cards.Add(card);
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

	static void EnrichSetInfo(PhysicalMtgCard card, IEnumerable<ScryfallApi.Client.Models.Set> sets, List<ImportIssue> issues, int rowNum)
	{
		var p = card.Printing;
		bool hadSetCode = p.Set is not null;
		bool hadSetName = p.SetName is not null;

		p.SetName ??= sets.FirstOrDefault(s => s.Code.Equals(p.Set, StringComparison.OrdinalIgnoreCase))?.Name;
		p.Set ??= sets.FirstOrDefault(s => s.Name.Equals(p.SetName, StringComparison.OrdinalIgnoreCase))?.Code.ToUpper();

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
