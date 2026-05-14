namespace MtgCsvHelper;

/// <summary>
/// Identifies the most likely <see cref="FormatConfig"/> for a CSV by matching its header
/// row against the configured columns of every known format. Case-sensitive on purpose —
/// formats differ in casing (e.g. <c>"Name"</c> vs <c>"NAME"</c> vs <c>"title"</c>), and
/// that difference is a load-bearing signal.
/// </summary>
public sealed class FormatDetector
{
	readonly IReadOnlyList<FormatConfig> _formats;
	// 2 was chosen empirically: every real-world format in the bundled appsettings has at
	// least 2 column names that no other format shares at the same casing, making accidental
	// 2-hit collisions on unrelated CSVs unlikely. If a new format with very generic header
	// names gets added (e.g. "Name,Set"), revisit.
	const int MinMatchesForConfidence = 2;

	public FormatDetector(IReadOnlyList<FormatConfig> formats) => _formats = formats;

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
		if (string.IsNullOrWhiteSpace(headerLine)) return null;

		string? bestName = null;
		int bestScore = 0;
		int bestConfiguredCount = int.MaxValue;
		foreach (var fmt in _formats)
		{
			var configured = HeadersOf(fmt).ToList();
			// Many exports quote header names (e.g. Topdecked: QUANTITY,"NAME",SETCODE,…) so strip
			// surrounding quotes before comparing — otherwise `"NAME"` wouldn't match `NAME`.
			var actual = headerLine.Split(fmt.Delimiter).Select(h => h.Trim().Trim('"')).ToHashSet();
			var hits = configured.Count(h => actual.Contains(h));
			// Strict improvement OR same score but tighter format (fewer extra unused headers
			// would indicate a more specific match). Ties beyond that fall back to iteration
			// order — if two formats end up with the same hit count *and* the same configured
			// column count, first-in-list wins silently. The current configured formats are
			// distinct enough that this doesn't happen; reconsider if a new format gets added
			// whose header set is a near-duplicate of an existing one.
			if (hits > bestScore || (hits == bestScore && configured.Count < bestConfiguredCount))
			{
				bestScore = hits;
				bestConfiguredCount = configured.Count;
				bestName = fmt.Name;
			}
		}

		return bestScore >= MinMatchesForConfidence ? bestName : null;
	}

	// Reads the first useful CSV header line, skipping the optional "sep=," marker that
	// some exports prepend (e.g. DragonShield writes `"sep=,"` — quoted, so we trim a
	// leading quote before testing).
	static string? ReadHeader(StreamReader reader)
	{
		var line = reader.ReadLine();
		if (IsSepMarker(line)) line = reader.ReadLine();
		return line;
	}

	static async Task<string?> ReadHeaderAsync(StreamReader reader)
	{
		var line = await reader.ReadLineAsync();
		if (IsSepMarker(line)) line = await reader.ReadLineAsync();
		return line;
	}

	static bool IsSepMarker(string? line) =>
		line is not null && line.TrimStart('"').StartsWith("sep=", StringComparison.OrdinalIgnoreCase);

	static IEnumerable<string> HeadersOf(FormatConfig f)
	{
		yield return f.Quantity;
		if (f.CardName is not null) yield return f.CardName.HeaderName;
		if (f.CardmarketId is not null) yield return f.CardmarketId;
		if (f.SetCode is not null) yield return f.SetCode;
		if (f.SetName is not null) yield return f.SetName;
		if (f.SetNumber is not null) yield return f.SetNumber;
		if (f.Finish is not null) yield return f.Finish.HeaderName;
		if (f.Condition is not null) yield return f.Condition.HeaderName;
		if (f.Language is not null) yield return f.Language.HeaderName;
		if (f.PriceBought is not null) yield return f.PriceBought.HeaderName;
	}
}
