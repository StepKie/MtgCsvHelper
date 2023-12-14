using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;
using Serilog;
using Serilog.Events;

namespace MtgCsvHelper.Tests;

public class BaseTest
{
	protected readonly IMtgApi _api;

	/// <summary> Parsed from appsettings.test.json </summary>
	public static IConfiguration TestConfiguration => new ConfigurationBuilder().AddJsonFile("appsettings.test.json").Build();

	/// <summary>
	/// Initializes a new test with a default log level of Information.
	/// If you need more detailed logging, change level accordingly
	/// </summary>
	public BaseTest(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	{

		Log.Logger = Logging.GetDefaultLoggerConfig.WriteTo.TestOutput(output, level).CreateLogger();

		_api = ServiceConfiguration.CachedApi;
		_api.LoadData().Wait();
	}
}
