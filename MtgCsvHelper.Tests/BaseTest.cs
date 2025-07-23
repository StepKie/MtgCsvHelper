using Microsoft.Extensions.Configuration;
using MtgCsvHelper.Services;
using ScryfallApi.Client;
using Serilog;
using Serilog.Events;

namespace MtgCsvHelper.Tests;

public class BaseTest
{
	protected readonly IMtgApi _api;
	protected readonly IConfiguration _config;

	/// <summary>
	/// Initializes a new test with a default log level of Information.
	/// If you need more detailed logging, change level accordingly
	/// </summary>
	public BaseTest(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	{

		Log.Logger = Logging.GetDefaultLoggerConfig.WriteTo.TestOutput(output, level).CreateLogger();
		_api = new CachedMtgApi(new ScryfallApiClient(CachedMtgApi.DEFAULT_CLIENT));
		_api.LoadData().Wait();

		IMtgApi.Default = _api; // used by CardNameConverter...
		_config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
	}
}
