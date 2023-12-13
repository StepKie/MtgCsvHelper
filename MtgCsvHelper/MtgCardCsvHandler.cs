using System.Globalization;
using CsvHelper.Configuration;
using MtgCsvHelper.Maps;

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

	public IList<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath)
	{
		Log.Information($"Parsing {csvFilePath} with input format {_format} ...");

		using var stream = new StreamReader(csvFilePath);
		_ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

		// "Peek" into the first row, and if it is not a separator info row, reset the stream. (Found no more elegant way to do this)
		var hasSeparatorInfoFirstLine = stream.ReadLine()?.Contains("sep=") ?? false;
		using var csv = new CsvReader(hasSeparatorInfoFirstLine ? stream : new(csvFilePath), new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null });
		csv.Context.RegisterClassMap(_classMap);
		List<PhysicalMtgCard> cards = csv.GetRecords<PhysicalMtgCard>().ToList();
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
