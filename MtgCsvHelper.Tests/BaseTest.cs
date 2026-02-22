using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace MtgCsvHelper.Tests;

public class BaseTest
{
	protected readonly IConfiguration _config;

	public BaseTest(ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	{
		Log.Logger = AppLogging.CreateDefaultLoggerConfig().WriteTo.TestOutput(output, level).CreateLogger();
		_config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
	}
}

public class ApiBaseTest(MtgApiFixture fixture, ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	: BaseTest(output, level)
{
	protected readonly MtgCsvHelper.Services.IMtgApi _api = fixture.Api;
}
