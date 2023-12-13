using System.Globalization;
using CsvHelper.Configuration;
using MtgCsvHelper.Maps;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly DeckFormat _format;
	readonly CsvToCardMap _classMap;

	public MtgCardCsvHandler(DeckFormat format)
	{
		_format = format;
		_classMap = _format.GenerateClassMap();
	}

	public List<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath, bool amendMissingInfo = true)
	{
		Log.Information($"Parsing {csvFilePath} with input format {_format} ...");

		using var stream = new StreamReader(csvFilePath);
		_ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

		// "Peek" into the first row, and if it is not a separator info row, reset the stream. (Found no more elegant way to do this)
		var hasSeparatorInfoFirstLine = stream.ReadLine()?.Contains("sep=") ?? false;
		using var csv = new CsvReader(hasSeparatorInfoFirstLine ? stream : new(csvFilePath), new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null });
		csv.Context.RegisterClassMap(_classMap);
		List<PhysicalMtgCard> cards = csv.GetRecords<PhysicalMtgCard>().ToList();

		if (amendMissingInfo)
		{
			IMtgApi api = new ScryfallApi();
			var sets = api.GetSets();

			foreach (var card in cards)
			{
				var set = card.Printing.Set;
				set.FullName ??= sets.FirstOrDefault(s => s.Code.Equals(set.Code, StringComparison.OrdinalIgnoreCase))?.FullName;
				if (string.IsNullOrEmpty(set.Code)) { set.Code = sets.FirstOrDefault(s => s.FullName!.Equals(set.FullName, StringComparison.OrdinalIgnoreCase))?.Code.ToUpper() ?? "---"; }
			}
		}

		Log.Information($"Parsed {cards.Sum(c => c.Count)} cards ({cards.Count} unique) cards from {csvFilePath}.");

		return cards;
	}

	public void WriteCollectionCsv(IList<PhysicalMtgCard> cards, string? outputFileName = null)
	{
		outputFileName ??= $"{_format.Name.ToLower()}-output-{DateTime.Now.ToShortDateString()}.csv";
		Log.Information($"Writing {cards.Sum(c => c.Count)} cards ({cards.Count} unique) cards to {outputFileName}");

		using var writer = new StreamWriter(outputFileName);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		csv.Context.RegisterClassMap(_classMap);
		csv.WriteHeader<PhysicalMtgCard>();
		csv.NextRecord();
		csv.WriteRecords(cards);
		csv.Flush();
	}
}
