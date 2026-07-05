using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MtgCsvHelper.Services;
using MudBlazor.Services;

namespace MtgCsvHelper.BlazorWebAssembly.Tests;

public class MtgCsvProcessorTests : BunitContext, IAsyncLifetime
{
	readonly FakeCatalogLoader _catalogLoader = new();
	readonly IConfiguration _config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();

	public MtgCsvProcessorTests()
	{
		JSInterop.Mode = JSRuntimeMode.Loose;
		Services.AddMudServices();
		Services.AddSingleton(_config);
		Services.AddSingleton<ICatalogLoader>(_catalogLoader);
		Services.AddSingleton<ICardmarketResolver>(new FakeCardmarketResolver());
	}

	// Some MudBlazor services are IAsyncDisposable-only; the context must be torn down via DisposeAsync.
	public Task InitializeAsync() => Task.CompletedTask;
	Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

	// MudSelect popovers teleport into a MudPopoverProvider, so the page renders alongside one and tests search from the shared root.
	IRenderedComponent<IComponent> RenderProcessor() => Render(b =>
	{
		b.OpenComponent<MudPopoverProvider>(0);
		b.CloseComponent();
		b.OpenComponent<MtgCsvProcessor>(1);
		b.CloseComponent();
	});

	static IRenderedComponent<MudSelect<string>> Select(IRenderedComponent<IComponent> root, string label) =>
		root.FindComponents<MudSelect<string>>().Single(s => s.Instance.Label == label);

	static void Upload(IRenderedComponent<IComponent> root, string csvContent) =>
		root.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText(csvContent, "upload.csv"));

	static string FixtureCsv(string name) =>
		File.ReadAllText(Path.Combine("Resources", "SampleCsvs", "Reference", name));

	[Fact]
	public void FormatSelects_OfferReadableInputsAndWritableOutputs()
	{
		var root = RenderProcessor();

		// Items render in page order — the input select and its items precede the output select's.
		root.FindComponents<MudSelectItem<string>>().Select(i => i.Instance.Value)
			.Should().Equal(CardMapFactory.ReadableFormats.Concat(CardMapFactory.WritableFormats));
	}

	[Fact]
	public void InitialFormats_Differ()
	{
		var root = RenderProcessor();

		Select(root, "Input format").Instance.GetState(x => x.Value)
			.Should().NotBe(Select(root, "Output format").Instance.GetState(x => x.Value));
	}

	[Fact]
	public async Task FileUpload_AutoDetectsInputFormat()
	{
		var csv = FixtureCsv("moxfield.csv");
		var detector = new FormatDetector([.. CardMapFactory.SupportedConfigs(_config)]);
		var expected = detector.Detect(csv.Split('\n')[0]);
		expected.Should().NotBeNull(because: "the reference fixture must be detectable, or the test asserts nothing");

		var root = RenderProcessor();
		await root.InvokeAsync(() => Upload(root, csv));

		await root.WaitForAssertionAsync(() => Select(root, "Input format").Instance.GetState(x => x.Value).Should().Be(expected));
	}

	[Fact]
	public async Task PickingOutputEqualToInput_SnapsInputToDifferentFormat()
	{
		var root = RenderProcessor();
		var collision = Select(root, "Input format").Instance.GetState(x => x.Value)!;

		await root.InvokeAsync(() => Select(root, "Output format").Instance.ValueChanged.InvokeAsync(collision));

		Select(root, "Output format").Instance.GetState(x => x.Value).Should().Be(collision);
		Select(root, "Input format").Instance.GetState(x => x.Value).Should().NotBe(collision);
	}

	[Fact]
	public async Task PickingInputEqualToOutput_SnapsOutputToDifferentFormat()
	{
		var root = RenderProcessor();
		var collision = Select(root, "Output format").Instance.GetState(x => x.Value)!;

		await root.InvokeAsync(() => Select(root, "Input format").Instance.ValueChanged.InvokeAsync(collision));

		Select(root, "Input format").Instance.GetState(x => x.Value).Should().Be(collision);
		Select(root, "Output format").Instance.GetState(x => x.Value).Should().NotBe(collision);
	}

	[Fact]
	public async Task UndetectableCsv_ReenablesManualInputPick()
	{
		var root = RenderProcessor();
		Select(root, "Input format").Instance.Disabled
			.Should().BeTrue(because: "auto-detect owns the input pick until it fails");

		await root.InvokeAsync(() => Upload(root, "foo,bar\n1,2\n"));

		await root.WaitForAssertionAsync(() => Select(root, "Input format").Instance.Disabled.Should().BeFalse());
	}

	[Fact]
	public async Task ConvertingCsvThatDoesNotMatchSelectedFormat_ShowsHeaderError()
	{
		var root = RenderProcessor();
		await root.InvokeAsync(() => Upload(root, "foo,bar\n1,2\n"));

		await root.FindAll("button").Single(b => b.TextContent.Contains("Convert")).ClickAsync(new());

		await root.WaitForAssertionAsync(() => root.FindComponent<MudAlert>().Markup.Should().Contain("missing required column"));
	}

	// Covers the page's StateChanged += StateHasChanged subscription: loader progress must re-render without user input.
	[Fact]
	public async Task CatalogLoadProgress_UpdatesUiOnStateChange()
	{
		_catalogLoader.Catalog = null;
		_catalogLoader.Progress = CatalogLoadProgress.Idle;

		var root = RenderProcessor();
		root.Markup.Should().NotContain("Loading card data");

		_catalogLoader.Progress = new(CatalogLoadPhase.Downloading, 42, 42, 100);
		await root.InvokeAsync(_catalogLoader.RaiseStateChanged);

		root.Markup.Should().Contain("Loading card data… 42%");
	}

	[Fact]
	public async Task FailedCatalogLoad_ShowsError_AndRetryReloads()
	{
		_catalogLoader.Catalog = null;
		_catalogLoader.Error = new InvalidOperationException("bundle fetch failed");
		_catalogLoader.Progress = new(CatalogLoadPhase.Failed, 0, 0, null);

		var root = RenderProcessor();

		root.Markup.Should().Contain("Card data failed to load").And.Contain("bundle fetch failed");
		await root.FindAll("button").Single(b => b.TextContent.Contains("Retry")).ClickAsync(new());
		_catalogLoader.LoadCalls.Should().Be(1);
	}
}
