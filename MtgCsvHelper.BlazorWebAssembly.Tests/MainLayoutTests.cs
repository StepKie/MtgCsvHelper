using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.BlazorWebAssembly.Layout;
using MudBlazor.Services;

namespace MtgCsvHelper.BlazorWebAssembly.Tests;

// Some MudBlazor services are IAsyncDisposable-only; xunit tears the class down via BunitContext.DisposeAsync.
public class MainLayoutTests : BunitContext
{
	readonly FakeCatalogLoader _catalogLoader = new();

	public MainLayoutTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMudServices();
		Services.AddSingleton<ICatalogLoader>(_catalogLoader);
	}

	static ReferenceCard Ref(string collectorNumber) =>
		new(Id: Guid.NewGuid(), OracleId: null, Name: "Orcish Bowmasters", Set: "LTR", SetName: "The Lord of the Rings",
			CollectorNumber: collectorNumber, Lang: "en", Layout: "normal", Finishes: [], FrameEffects: null,
			BorderColor: null, PromoTypes: null, CardmarketId: null, TcgplayerId: null,
			TcgplayerEtchedId: null, MultiverseIds: null);

	string LayoutMarkup() => Render<MainLayout>().Markup;

	[Fact]
	public void Footer_ReportsCatalogSizeAndBundleDate()
	{
		var built = new DateTimeOffset(2026, 7, 3, 11, 4, 49, TimeSpan.Zero);
		_catalogLoader.Catalog = new ReferenceCardCatalog([Ref("1"), Ref("2"), Ref("3")]);
		_catalogLoader.BundleLastModified = built;

		// Local-time conversion is deliberate — the footer answers "how old is my data" for the reader's clock.
		var expectedDate = built.ToLocalTime().ToString("d MMM yyyy", CultureInfo.InvariantCulture);
		LayoutMarkup().Should().Contain($"Card data: 3 printings, updated {expectedDate}");
	}

	[Fact]
	public void Footer_OmitsDate_WhenHostSendsNoLastModified()
	{
		_catalogLoader.Catalog = new ReferenceCardCatalog([Ref("1")]);
		_catalogLoader.BundleLastModified = null;

		var markup = LayoutMarkup();
		markup.Should().Contain("Card data: 1 printings");
		markup.Should().NotContain("updated", "a missing Last-Modified header must not render an empty or bogus date");
	}

	[Fact]
	public void Footer_Absent_WhileCatalogIsStillLoading()
	{
		_catalogLoader.Catalog = null;

		LayoutMarkup().Should().NotContain("Card data:");
	}
}
