using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;
using ScryfallApi.Client;
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
		_api = new CachedMtgApi(new ScryfallApiClient(new HttpClient() { BaseAddress = ScryfallApiClientConfig.GetDefault().ScryfallApiBaseAddress })); ;
		_api.LoadData().Wait();

		IMtgApi.Default = _api; // used by CardNameConverter...
	}
}
