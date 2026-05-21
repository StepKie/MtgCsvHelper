using System.Runtime.CompilerServices;

namespace MtgCsvHelper.Tests;

public class AppsettingsParityTests
{
	// MtgCsvHelper/appsettings.json is the library-side runtime config used by Console + Tests;
	// MtgCsvHelper.BlazorWebAssembly/wwwroot/appsettings.json is the Blazor static-web-asset copy
	// served to the browser. They must stay byte-identical — a divergence means Console and the
	// deployed Blazor app would parse different format mappings for the same CSV file.
	[Fact]
	public void BlazorWwwrootCopy_IsByteIdenticalToLibrarySource()
	{
		var (library, wwwroot) = ResolvePaths();
		File.ReadAllBytes(wwwroot).Should().Equal(File.ReadAllBytes(library),
			because: "wwwroot/appsettings.json must mirror MtgCsvHelper/appsettings.json — the build-time sync target keeps them aligned; commit both files together.");
	}

	static (string Library, string Wwwroot) ResolvePaths([CallerFilePath] string? thisFile = null)
	{
		var repoRoot = new FileInfo(thisFile!).Directory!.Parent
			?? throw new InvalidOperationException("Could not resolve repo root from test file path.");
		return (
			Path.Combine(repoRoot.FullName, "MtgCsvHelper", "appsettings.json"),
			Path.Combine(repoRoot.FullName, "MtgCsvHelper.BlazorWebAssembly", "wwwroot", "appsettings.json"));
	}
}
