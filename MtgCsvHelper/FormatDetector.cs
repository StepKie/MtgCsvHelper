namespace MtgCsvHelper;

/// <summary>
/// Identifies the most likely <see cref="FormatConfig"/> for a CSV by matching its header
/// row against the configured columns of every known format. Case-sensitive on purpose —
/// formats differ in casing (e.g. <c>"Name"</c> vs <c>"NAME"</c> vs <c>"title"</c>), and
/// that difference is a load-bearing signal.
/// </summary>
public sealed class FormatDetector(IReadOnlyList<FormatConfig> formats)
{
	// Empirical: every bundled format has ≥2 uniquely-cased column names; revisit if a very generic format gets added.
	const int MinMatchesForConfidence = 2;

	/// <summary>
	/// Reads the first (non-marker) line from the stream and detects the format from it.
	/// Stream is read in a non-disposing wrapper so the caller controls stream lifetime.
	/// </summary>
	public string? Detect(Stream stream)
	{
		using var reader = new StreamReader(stream, leaveOpen: true);
		var line = ReadHeader(reader);
		return line is null ? null : Detect(line);
	}

	/// <summary>Async variant of <see cref="Detect(Stream)"/>.</summary>
	public async Task<string?> DetectAsync(Stream stream)
	{
		using var reader = new StreamReader(stream, leaveOpen: true);
		var line = await ReadHeaderAsync(reader);
		return line is null ? null : Detect(line);
	}

	/// <summary>
	/// Returns the <see cref="FormatConfig.Name"/> of the best match, or <c>null</c> if no
	/// format produces a confident match (≥ <see cref="MinMatchesForConfidence"/> column hits).
	/// </summary>
	public string? Detect(string headerLine)
	{
		if (string.IsNullOrWhiteSpace(headerLine)) { return null; }

		string? bestName = null;
		int bestScore = 0;
		int bestConfiguredCount = int.MaxValue;
		foreach (var fmt in formats)
		{
			var configured = HeadersOf(fmt).ToList();
			// Strip surrounding quotes before comparing — many exports quote header names (Topdecked: QUANTITY,"NAME",…).
			var actual = headerLine.Split(fmt.Delimiter).Select(h => h.Trim().Trim('"')).ToHashSet();
			var hits = configured.Count(h => actual.Contains(h));
			// Prefer more hits, then fewer configured columns (tighter format); remaining ties fall to iteration order.
			if (hits > bestScore || (hits == bestScore && configured.Count < bestConfiguredCount))
			{
				bestScore = hits;
				bestConfiguredCount = configured.Count;
				bestName = fmt.Name;
			}
		}

		return bestScore >= MinMatchesForConfidence ? bestName : null;
	}

	// First useful header line, skipping the optional (possibly quoted) "sep=" marker some exports prepend.
	static string? ReadHeader(StreamReader reader)
	{
		var line = reader.ReadLine();
		if (IsSepMarker(line)) { line = reader.ReadLine(); }

		return line;
	}

	static async Task<string?> ReadHeaderAsync(StreamReader reader)
	{
		var line = await reader.ReadLineAsync();
		if (IsSepMarker(line)) { line = await reader.ReadLineAsync(); }

		return line;
	}

	static bool IsSepMarker(string? line) =>
		line is not null && line.TrimStart('"').StartsWith("sep=", StringComparison.OrdinalIgnoreCase);

	// Hand-maintained whitelist: a new header-bearing field on FormatConfig must be added here or its detection signal is silently lost.
	static IEnumerable<string> HeadersOf(FormatConfig f)
	{
		yield return f.Quantity;
		if (f.CardName is not null) yield return f.CardName.HeaderName;
		if (f.CardmarketId is not null) yield return f.CardmarketId;
		if (f.SetCode is not null) yield return f.SetCode;
		if (f.SetName is not null) yield return f.SetName;
		if (f.SetNumber is not null) yield return f.SetNumber;
		if (f.ScryfallId is not null) yield return f.ScryfallId;
		if (f.Rarity is not null) yield return f.Rarity.HeaderName;
		if (f.MultiverseId is not null) yield return f.MultiverseId;
		if (f.TcgplayerId is not null) yield return f.TcgplayerId;
		if (f.Finish is not null) yield return f.Finish.HeaderName;
		if (f.Condition is not null) yield return f.Condition.HeaderName;
		if (f.Language is not null) yield return f.Language.HeaderName;
		if (f.PriceBought is not null) yield return f.PriceBought.HeaderName;
	}
}
