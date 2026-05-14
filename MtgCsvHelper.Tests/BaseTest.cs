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

public class ApiBaseTest(CatalogFixture fixture, ITestOutputHelper output, LogEventLevel level = LogEventLevel.Debug)
	: BaseTest(output, level)
{
	protected readonly IReferenceCardCatalog _catalog = fixture.Catalog;
	protected readonly MtgCsvHelper.Services.ICardmarketResolver _resolver = fixture.Resolver;
}
