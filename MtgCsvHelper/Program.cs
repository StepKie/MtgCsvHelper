using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;

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
	var reader = new MtgCardCsvHandler(new DeckFormat(config, opts.InputFormat));
	var writer = new MtgCardCsvHandler(new DeckFormat(config, opts.OutputFormat));

	List<PhysicalMtgCard> cardsFound = [];

	foreach (var fileName in filesToParse)
	{
		var parsedCardsFromFile = reader.ParseCollectionCsv(fileName);
		cardsFound.AddRange(parsedCardsFromFile);
	}

	writer.WriteCollectionCsv(cardsFound);
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); // TODO: handle errors
