namespace MtgCsvHelper;

public class Logging
{
	public const string DEFAULT_OUTPUT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

	public static LoggerConfiguration GetDefaultLoggerConfig => new LoggerConfiguration()
		.MinimumLevel.Debug()
		.WriteTo.Debug(outputTemplate: DEFAULT_OUTPUT_TEMPLATE);
}
