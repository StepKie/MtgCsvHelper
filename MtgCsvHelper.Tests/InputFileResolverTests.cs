namespace MtgCsvHelper.Tests;

public sealed class InputFileResolverTests : IDisposable
{
	readonly string _tmpDir;
	readonly string _tmpSubdir;

	public InputFileResolverTests()
	{
		_tmpDir = Path.Combine(Path.GetTempPath(), $"mtgcsvhelper-test-{Guid.NewGuid():N}");
		_tmpSubdir = Path.Combine(_tmpDir, "samples");
		Directory.CreateDirectory(_tmpDir);
		Directory.CreateDirectory(_tmpSubdir);

		File.WriteAllText(Path.Combine(_tmpDir, "alpha.csv"), "x");
		File.WriteAllText(Path.Combine(_tmpDir, "beta.csv"), "x");
		File.WriteAllText(Path.Combine(_tmpDir, "ignored.txt"), "x");
		File.WriteAllText(Path.Combine(_tmpSubdir, "deep.csv"), "x");
	}

	public void Dispose() => Directory.Delete(_tmpDir, recursive: true);

	[Fact]
	public void BareFilename_ResolvesAgainstCwd()
	{
		var result = InputFileResolver.Resolve("alpha.csv", _tmpDir).ToList();
		result.Should().HaveCount(1);
		result[0].Should().EndWith("alpha.csv");
	}

	[Fact]
	public void Wildcard_ResolvesMatchingFilesInCwd()
	{
		var result = InputFileResolver.Resolve("*.csv", _tmpDir).Select(Path.GetFileName).ToList();
		result.Should().BeEquivalentTo(["alpha.csv", "beta.csv"]);
	}

	[Fact]
	public void RelativePathToFile_ResolvesIntoSubdir()
	{
		var result = InputFileResolver.Resolve(Path.Combine("samples", "deep.csv"), _tmpDir).ToList();
		result.Should().HaveCount(1);
		result[0].Should().EndWith("deep.csv");
	}

	[Fact]
	public void RelativePathWithWildcard_ResolvesPatternInSubdir()
	{
		var result = InputFileResolver.Resolve(Path.Combine("samples", "*.csv"), _tmpDir).ToList();
		result.Should().HaveCount(1);
		result[0].Should().EndWith("deep.csv");
	}

	[Fact]
	public void AbsolutePathToFile_Resolves()
	{
		var absolute = Path.Combine(_tmpDir, "alpha.csv");
		var result = InputFileResolver.Resolve(absolute).ToList();
		result.Should().ContainSingle().Which.Should().EndWith("alpha.csv");
	}

	[Fact]
	public void AbsolutePathWithWildcard_Resolves()
	{
		var absolutePattern = Path.Combine(_tmpDir, "*.csv");
		var result = InputFileResolver.Resolve(absolutePattern).Select(Path.GetFileName).ToList();
		result.Should().BeEquivalentTo(["alpha.csv", "beta.csv"]);
	}

	[Fact]
	public void NonexistentDirectory_ReturnsEmpty()
	{
		var result = InputFileResolver.Resolve(Path.Combine("does-not-exist", "*.csv"), _tmpDir);
		result.Should().BeEmpty();
	}

	[Fact]
	public void NonexistentFile_ReturnsEmpty()
	{
		var result = InputFileResolver.Resolve("missing.csv", _tmpDir);
		result.Should().BeEmpty();
	}

	[Fact]
	public void Wildcard_DoesNotMatchOtherExtensions()
	{
		var result = InputFileResolver.Resolve("*.csv", _tmpDir).Select(Path.GetFileName).ToList();
		result.Should().NotContain("ignored.txt");
	}

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public void EmptyOrWhitespacePattern_ReturnsEmpty(string pattern)
	{
		InputFileResolver.Resolve(pattern, _tmpDir).Should().BeEmpty();
	}
}
