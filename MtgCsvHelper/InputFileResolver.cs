namespace MtgCsvHelper;

// Resolves a CLI-style file argument into a list of matching files.
// Supports the four shapes a user is likely to pass via -f:
//   - bare filename or pattern in cwd:        "foo.csv", "*.csv"
//   - relative path to file or pattern:       "samples/foo.csv", "samples/*.csv"
//   - absolute path to file or pattern:       "C:/abs/foo.csv", "C:/abs/*.csv"
//   - relative path with no filename component: treated as wildcard in that dir
public static class InputFileResolver
{
	public static IEnumerable<string> Resolve(string pattern, string? workingDirectory = null)
	{
		if (string.IsNullOrWhiteSpace(pattern)) { return []; }

		workingDirectory ??= Directory.GetCurrentDirectory();

		var fullPath = Path.IsPathFullyQualified(pattern)
			? pattern
			: Path.Combine(workingDirectory, pattern);

		var dir = Path.GetDirectoryName(fullPath);
		var fileName = Path.GetFileName(fullPath);

		if (string.IsNullOrEmpty(dir)) { dir = workingDirectory; }
		if (string.IsNullOrEmpty(fileName)) { fileName = "*"; }

		return Directory.Exists(dir)
			? Directory.EnumerateFiles(dir, fileName)
			: [];
	}
}
