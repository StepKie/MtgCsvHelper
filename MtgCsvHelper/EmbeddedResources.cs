using System.Text.Json;

namespace MtgCsvHelper;

/// <summary>Loads the JSON string-map resources embedded under <c>Resources/</c> in this assembly.</summary>
internal static class EmbeddedResources
{
	/// <summary>Deserializes the embedded <c>Resources/{fileName}</c> JSON object into a case-insensitive string map.</summary>
	public static IReadOnlyDictionary<string, string> LoadStringMap(string fileName)
	{
		var resourceName = $"MtgCsvHelper.Resources.{fileName}";
		using var stream = typeof(EmbeddedResources).Assembly.GetManifestResourceStream(resourceName)
			?? throw new InvalidOperationException($"Embedded resource '{resourceName}' not found.");
		var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(stream)
			?? throw new InvalidOperationException($"Failed to deserialize '{resourceName}'.");
		return new Dictionary<string, string>(raw, StringComparer.OrdinalIgnoreCase);
	}
}
