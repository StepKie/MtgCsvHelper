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
// There is no need to run the host continuously ...
//await host.RunAsync();

void RunWithOptions(CommandLineOptions opts)
{
	// Set ConfigFile through singleton (due to how ClassMaps are registered)
	CsvToCardMap.ConfigFile = config;
	var files = opts.InputFiles;
	Console.WriteLine($"Files: {files}");
	string dsCsvFile = files.Any() ? files.First() : $"SampleCsvs/drasgonshield-collection_2022-11-11.csv";

	Log.Information($"Parsing Csv with input format {opts.InputFormat} ...");
	var cards = ParseCollectionCsv(dsCsvFile, opts.InputFormat);
	Log.Information($"Found {cards.Count} cards, writing to target file ...");

	WriteCollectionCsv(cards, opts.OutputFormat);
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); //TODO: handle errors

IList<PhysicalMtgCard> ParseCollectionCsv(string csvFilePath, DeckFormat format)
{
	using StreamReader stream = new(csvFilePath);
	_ = stream ?? throw new FileNotFoundException($"{csvFilePath} not found");

	using CsvReader csv = new(stream, CultureInfo.InvariantCulture);
	csv.Read(); // Read twice to discard "=sep" in DragonShield
	csv.Read();
	_ = csv.ReadHeader();

	csv.Context.RegisterClassMap(format.GetCsvMapType());
	List<PhysicalMtgCard> autoReadCards = csv.GetRecords<PhysicalMtgCard>().ToList();

	return autoReadCards;
}

void WriteCollectionCsv(IList<PhysicalMtgCard> cards, DeckFormat format)
{
	using var writer = new StreamWriter($"{format.ToString().ToLower()}-output-{DateTime.Now.ToShortDateString()}.csv");
	using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

	csv.Context.RegisterClassMap(format.GetCsvMapType());

	csv.WriteHeader<PhysicalMtgCard>();
	csv.NextRecord();
	csv.WriteRecords(cards);
	csv.Flush();
}

class CommandLineOptions
{
	[Option('f', "file", Required = true, HelpText = "Input files to be processed.")]
	public IEnumerable<string> InputFiles { get; set; }

	[Option("in", Default = DeckFormat.DRAGONSHIELD, HelpText = "Specify output file format.")]
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
				InputFiles = new[] { "\\.my-exported-dragonshield-collection.csv" },
				InputFormat = DeckFormat.DRAGONSHIELD,
				OutputFormat = DeckFormat.MOXFIELD
			})
	};
}

