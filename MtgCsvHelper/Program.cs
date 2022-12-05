using CommandLine;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;
using System.Globalization;

using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithParsed(RunWithOptions)
	.WithNotParsed(HandleParseError);

Log.Information($"Done. Press Ctrl+C to exit");
Console.ReadLine();

void RunWithOptions(CommandLineOptions opts)
{
	var filesToParse = Directory.GetFiles(Directory.GetCurrentDirectory(), opts.InputFilePattern);
	List<PhysicalMtgCard> cardsFound = new();

	foreach (var fileName in filesToParse)
	{
		var parsedCardsFromFile = ParseCollectionCsv(fileName, new DeckFormat(config, opts.InputFormat));
		cardsFound.AddRange(parsedCardsFromFile);
	}

	WriteCollectionCsv(cardsFound, new DeckFormat(config, opts.OutputFormat));
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); //TODO: handle errors

IList<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath, DeckFormat format)
{
	Log.Information($"Parsing {csvFilePath} with input format {format} ...");
	using StreamReader stream = new(csvFilePath);
	_ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

	// "Peek" into the first row, and if it is not a separator info row, reset the stream. (Found no more elegant way to do this)
	var hasSeparatorInfoFirstLine = stream.ReadLine()?.Contains("sep=") ?? false;
	using CsvReader csv = new(hasSeparatorInfoFirstLine ? stream : new(csvFilePath), new CsvConfiguration(CultureInfo.InvariantCulture) { HeaderValidated = null, MissingFieldFound = null });
	csv.Context.RegisterClassMap(format.GenerateClassMap());
	List<PhysicalMtgCard> autoReadCards = csv.GetRecords<PhysicalMtgCard>().ToList();
	Log.Information($"Parsed {autoReadCards.Count} cards from {csvFilePath}.");
	return autoReadCards;
}

void WriteCollectionCsv(IList<PhysicalMtgCard> cards, DeckFormat format)
{
	var outputFileName = $"{format.Name.ToLower()}-output-{DateTime.Now.ToShortDateString()}.csv";
	Log.Information($"Writing {cards.Count} cards to {outputFileName}");

	using var writer = new StreamWriter(outputFileName);
	using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

	csv.Context.RegisterClassMap(format.GenerateClassMap());
	csv.WriteHeader<PhysicalMtgCard>();
	csv.NextRecord();
	csv.WriteRecords(cards);
	csv.Flush();
}
