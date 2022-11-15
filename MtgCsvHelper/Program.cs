using CommandLine;
using CommandLine.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Globalization;

using IHost host = Host.CreateDefaultBuilder(args).Build();

// Ask the service provider for the configuration abstraction.
IConfiguration config = host.Services.GetRequiredService<IConfiguration>();

Log.Logger = new LoggerConfiguration()
	.ReadFrom.Configuration(config)
	.CreateLogger();

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithParsed(RunWithOptions)
	.WithNotParsed(HandleParseError);

Log.Information($"Done. Press Ctrl+C to exit");
Console.ReadLine();

// There is no need to run the host continuously ...
//await host.RunAsync();

void RunWithOptions(CommandLineOptions opts)
{
	// Set ConfigFile through singleton (due to how ClassMaps are registered)
	CsvToCardMap.ConfigFile = config;

	var filesToParse = Directory.GetFiles(Directory.GetCurrentDirectory(), opts.InputFilePattern);
	List<PhysicalMtgCard> cardsFound = new();

	foreach (var fileName in filesToParse)
	{
		var parsedCardsFromFile = ParseCollectionCsv(fileName, opts.InputFormat);
		cardsFound.AddRange(parsedCardsFromFile);
	}

	WriteCollectionCsv(cardsFound, opts.OutputFormat);
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); //TODO: handle errors

IList<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath, DeckFormat format)
{
	Log.Information($"Parsing {csvFilePath} with input format {format} ...");
	using StreamReader stream = new(csvFilePath);
	_ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");
	
	// "Peek" into the first row, and if it is not a separator info row, reset the stream. (Found no more elegant way to do this)
	var hasSeparatorInfoFirstLine = stream.ReadLine().Contains("sep=");
	using CsvReader csv = new(hasSeparatorInfoFirstLine ? stream : new(csvFilePath), CultureInfo.InvariantCulture);
	csv.Context.RegisterClassMap(format.GetCsvMapType());
	List<PhysicalMtgCard> autoReadCards = csv.GetRecords<PhysicalMtgCard>().ToList();
	Log.Information($"Parsed {autoReadCards.Count} cards from {csvFilePath}.");
	return autoReadCards;
}

void WriteCollectionCsv(IList<PhysicalMtgCard> cards, DeckFormat format)
{
	var outputFileName = $"{format.ToString().ToLower()}-output-{DateTime.Now.ToShortDateString()}.csv";
	Log.Information($"Writing {cards.Count} cards to {outputFileName}");

	using var writer = new StreamWriter(outputFileName);
	using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

	csv.Context.RegisterClassMap(format.GetCsvMapType());
	csv.WriteHeader<PhysicalMtgCard>();
	csv.NextRecord();
	csv.WriteRecords(cards);
	csv.Flush();
}

class CommandLineOptions
{
	[Option('f', "file", Required = true, HelpText = "Input file(s) to be processed (specify file name or wild card syntax).", Default = new[] { "SampleCsvs/dragonshield-*.csv" })]
	public string InputFilePattern { get; set; }

	[Option("in", Default = DeckFormat.DRAGONSHIELD, HelpText = "Specify input file format.")]
	public DeckFormat InputFormat { get; set; }

	[Option("out", Default = DeckFormat.MOXFIELD, HelpText = "Specify output file format.")]
	public DeckFormat OutputFormat { get; set; }

	[Usage(ApplicationAlias = "MtgCsvHelper")]
	public static IEnumerable<Example> Examples => new List<Example>()
	{
		new Example(
			"Example usage: Parse a file in Dragonshield format and output Moxfield-compatible .csv",
			new CommandLineOptions
			{
				InputFilePattern = "SampleCsvs/dragonshield-*.csv",
				InputFormat = DeckFormat.DRAGONSHIELD,
				OutputFormat = DeckFormat.MOXFIELD
			})
	};
}

