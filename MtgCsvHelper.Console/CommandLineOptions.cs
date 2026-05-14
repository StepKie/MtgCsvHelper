using CommandLine;
using CommandLine.Text;

class CommandLineOptions
{
	[Option('f', "file", HelpText = "Input file(s) to be processed (specify file name or wild card syntax).", Default = "*.csv")]
	public required string InputFilePattern { get; init; }

	[Option("in", HelpText = "Specify input file format. Must be one of the values in appsettings.json CsvConfigurations keys. If omitted, the format is auto-detected from the CSV header row.")]
	public string? InputFormat { get; init; }

	[Option("out", Default = "MOXFIELD", HelpText = "Specify output file format.")]
	public required string OutputFormat { get; init; }

	[Usage(ApplicationAlias = "MtgCsvHelper")]
	public static IEnumerable<Example> Examples =>
	[
		new Example(
			"Auto-detect input format and output Moxfield-compatible .csv",
			new CommandLineOptions
			{
				InputFilePattern = "*.csv",
				OutputFormat = "MOXFIELD",
			}),
		new Example(
			"Force input format (skip auto-detect)",
			new CommandLineOptions
			{
				InputFilePattern = "*.csv",
				InputFormat = "DRAGONSHIELD",
				OutputFormat = "MOXFIELD",
			})
	];
}
