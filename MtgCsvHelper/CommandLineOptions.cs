using CommandLine;
using CommandLine.Text;

class CommandLineOptions
{
	[Option('f', "file", HelpText = "Input file(s) to be processed (specify file name or wild card syntax).", Default = "*.csv")]
	public required string InputFilePattern { get; init; }

	[Option("in", Default = "DRAGONSHIELD", HelpText = $"Specify input file format. Must be one of the values in appsettings.json CsvConfigurations keys")]
	public required string InputFormat { get; init; }

	[Option("out", Default = "MOXFIELD", HelpText = "Specify output file format.")]
	public required string OutputFormat { get; init; }

	[Usage(ApplicationAlias = "MtgCsvHelper")]
	public static IEnumerable<Example> Examples => new[]
	{
		new Example(
			"Example usage: Parse a file in Dragonshield format and output Moxfield-compatible .csv",
			new CommandLineOptions
			{
				InputFilePattern = "*.csv",
				InputFormat = "DRAGONSHIELD",
				OutputFormat = "MOXFIELD",
			})
	};
}

