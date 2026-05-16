namespace MtgCsvHelper;

/// <summary>
/// Case-insensitive ordinal comparison used everywhere a CSV cell value is matched against a
/// string configured in <c>appsettings.json</c>. Sites are inconsistent across exports
/// (<c>"Etched"</c> vs <c>"etched"</c>, <c>"Foil"</c> vs <c>"foil"</c>, <c>"normal"</c> vs
/// <c>"Normal"</c>), and hand-edited exports compound it. Documented once here; all converters
/// use this rather than calling <c>string.Equals</c> with an explicit comparer arg.
/// </summary>
internal static class CsvMatch
{
	/// <summary>
	/// True if <paramref name="input"/> matches <paramref name="configValue"/> ignoring case.
	/// Returns false if either operand is null.
	/// </summary>
	public static bool MatchesConfig(this string? input, string? configValue) =>
		input is not null
		&& configValue is not null
		&& string.Equals(input, configValue, StringComparison.OrdinalIgnoreCase);
}
