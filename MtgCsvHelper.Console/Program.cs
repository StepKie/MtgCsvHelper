using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MtgCsvHelper;
using MtgCsvHelper.Services;

IHostBuilder builder = Host
	.CreateDefaultBuilder(args)
	.ConfigureServices(builder => builder.ConfigureMtgCsvHelper());

using IHost host = builder.Build();

// Ask the service provider for the configuration abstraction.
var config = host.Services.GetRequiredService<IConfiguration>();
Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

var api = host.Services.GetService<IMtgApi>()!;
await api.LoadData();
IMtgApi.Default = api;

Parser.Default.ParseArguments<CommandLineOptions>(args)
	.WithNotParsed(HandleParseError)
	.WithParsed(RunWithOptions);

Log.Information($"Done. Press Ctrl+C to exit");
Console.ReadLine();

void RunWithOptions(CommandLineOptions opts)
{
	var filesToParse = Directory.GetFiles(Directory.GetCurrentDirectory(), opts.InputFilePattern);

	var reader = new MtgCardCsvHandler(api, config, opts.InputFormat);
	var writer = new MtgCardCsvHandler(api, config, opts.OutputFormat);

	Collection collection = new();

	foreach (var fileName in filesToParse)
	{
		var parsedCollection = reader.ParseCollectionCsv(new FileStream(fileName, FileMode.Open));
		collection.Entries.AddRange(parsedCollection.Entries);
	}

	writer.WriteCollectionCsv(collection);
}
void HandleParseError(IEnumerable<Error> errs) => Console.WriteLine(string.Join(",", errs)); // TODO: handle errors
