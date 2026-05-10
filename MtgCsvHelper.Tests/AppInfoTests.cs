using System.Reflection;

namespace MtgCsvHelper.Tests;

public class AppInfoTests
{
	[Fact]
	public void Version_MatchesAssemblyInformationalVersion()
	{
		var expected = typeof(AppInfo).Assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

		expected.Should().NotBeNullOrEmpty(because: "the assembly must carry InformationalVersion (set via Directory.Build.props)");
		AppInfo.Version.Should().Be(expected);
	}

	[Fact]
	public void UserAgent_IsProductSlashVersion()
	{
		AppInfo.UserAgent.Should().Be($"MtgCsvHelper/{AppInfo.Version}");
	}
}
