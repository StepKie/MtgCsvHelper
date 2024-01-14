using System.Globalization;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Maps;
using MtgCsvHelper.Services;

namespace MtgCsvHelper;
public class MtgCardCsvHandler
{
	readonly PhysicalCardMap _classMap;
	readonly IMtgApi _api;
	readonly string _format;

	public MtgCardCsvHandler(IMtgApi api, IConfiguration config, string format)
	{
		_format = format;
		_classMap = new CardMapFactory(config).GenerateClassMap(format) ?? throw new ArgumentException($"Unsupported {format} - not found in configuration file");
		_api = api;
	}

	public string Format { get; init; }

	public Collection ParseCollectionCsv(string csvFilePath) => ParseCollectionCsv(File.OpenRead(csvFilePath));

	public Collection ParseCollectionCsv(Stream csvFilePath)
	{
		Log.Information($"Parsing {csvFilePath} with input format {_format} ...");
		using var stream = new StreamReader(csvFilePath);
		CheckIfFirstLineCanBeIgnored(stream);

		using var csv = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture) {/* HeaderValidated = null,*/ MissingFieldFound = null });
		csv.Context.RegisterClassMap(_classMap);
		List<PhysicalMtgCard> cards = csv.GetRecords<PhysicalMtgCard>().ToList();

		var sets = _api.GetSets();

		foreach (var card in cards)
		{
			var logicalCard = card.Printing;
			logicalCard.SetName ??= sets.FirstOrDefault(s => s.Code.Equals(logicalCard.Set, StringComparison.OrdinalIgnoreCase))?.Name;
			logicalCard.Set ??= sets.FirstOrDefault(s => s.Name.Equals(logicalCard.SetName, StringComparison.OrdinalIgnoreCase))?.Code.ToUpper();
		}

		Collection collection = new() { Name = $"Import {_format}, Date: {DateTime.Now}", Cards = cards };
		Log.Debug(collection.GenerateSummary());

		return collection;

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

		using var writer = new StreamWriter(outputFileName);
		using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

		csv.Context.RegisterClassMap(_classMap);
		csv.WriteHeader<PhysicalMtgCard>();
		csv.NextRecord();
		csv.WriteRecords(cards);
		csv.Flush();
	}
}
