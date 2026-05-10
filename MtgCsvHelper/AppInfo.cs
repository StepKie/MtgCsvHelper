using System.Reflection;

namespace MtgCsvHelper;

// Single source of truth for app-wide identity strings (version, User-Agent).
// Read once from this assembly's metadata at static-init time; the underlying value
// comes from <Version> in Directory.Build.props via MSBuild's auto-generated
// AssemblyInformationalVersionAttribute.
public static class AppInfo
{
	public static string Version { get; } = typeof(AppInfo).Assembly
		.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
		.InformationalVersion ?? "unknown";

	public static string UserAgent { get; } = $"MtgCsvHelper/{Version}";
}
