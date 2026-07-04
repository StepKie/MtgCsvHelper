namespace MtgCsvHelper.RefreshReferenceData;

internal static class RepoRoot
{
	/// <summary>Walks up from the tool's bin output to the directory containing MtgCsvHelper.slnx.</summary>
	public static string Find()
	{
		var dir = new DirectoryInfo(AppContext.BaseDirectory);
		while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "MtgCsvHelper.slnx")))
		{
			dir = dir.Parent;
		}

		return dir?.FullName ?? throw new InvalidOperationException($"Could not locate repo root (MtgCsvHelper.slnx) above {AppContext.BaseDirectory}");
	}
}
