using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.XUnit3;

namespace MtgCsvHelper.Tests;

public class BaseTest
{
	protected readonly IConfiguration _config;

	public BaseTest(LogEventLevel level = LogEventLevel.Debug)
	{
		// The sink resolves the running test's output helper from TestContext, so nothing needs threading in.
		Log.Logger = AppLogging.CreateDefaultLoggerConfig().WriteTo.XUnit3TestOutput(restrictedToMinimumLevel: level).CreateLogger();
		_config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
	}
}

public class ApiBaseTest(CatalogFixture fixture, LogEventLevel level = LogEventLevel.Debug)
	: BaseTest(level)
{
	protected readonly IReferenceCardCatalog _catalog = fixture.Catalog;
	protected readonly MtgCsvHelper.Services.ICardmarketResolver _resolver = fixture.Resolver;
}
