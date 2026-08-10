namespace MtgCsvHelper.Tests;

/// <summary>
/// Generates one committed sample CSV per writable format from master.csv, under
/// MtgCsvHelper/Resources/SampleCsvs/Reference/&lt;format&gt;.csv. Those files double as user-facing
/// documentation and as the exact artifacts handed to each live platform for import verification.
///
/// This is a snapshot test: when a committed CSV is missing or no longer matches what master.csv
/// produces, it (re)writes the file to the source tree and fails. The fix is to review the
/// regenerated diff, commit it, and re-run. In a clean tree it is read-only and just asserts the
/// committed files are in sync.
/// </summary>
[Collection(CatalogCollection.Name)]
public class ReferenceCsvSyncTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	static readonly string ReferenceDir = Path.Combine(CanonicalReference.RepoRoot(), "MtgCsvHelper", "Resources", "SampleCsvs", "Reference");

	// DragonShield stamps DateTime.Today onto a null purchase date on write; seed a fixed one so its committed sample stays deterministic.
	static readonly DateTime GenerationDate = new(2024, 1, 1);

	public static TheoryData<string> WritableFormats() => new(CardMapFactory.WritableFormats);

	[Theory]
	[MemberData(nameof(WritableFormats))]
	public void ReferenceCsv_IsInSyncWithReferenceCollection(string format)
	{
		var generated = GenerateCsv(format);
		var path = Path.Combine(ReferenceDir, $"{format.ToLowerInvariant()}.csv");
		var committed = File.Exists(path) ? File.ReadAllText(path) : null;

		if (committed is null || Normalize(committed) != Normalize(generated))
		{
			Directory.CreateDirectory(ReferenceDir);
			File.WriteAllText(path, generated);
			Assert.Fail($"Reference CSV for {format} was missing or out of date; (re)generated {path}.");
		}
	}

	string GenerateCsv(string format)
	{
		var cfg = CardMapFactory.From(_config).First(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));
		var cards = CanonicalReference.LoadCards(_config, _catalog, _resolver, cfg.Currency);
		if (cfg.RequiresWriteDefaults)
		{
			cards = cards.Select(c => c with { DateBought = c.DateBought ?? GenerationDate }).ToList();
		}

		return CsvFixture.WriteToString(new MtgCardCsvHandler(_catalog, _resolver, _config, format), cards);
	}

	// Compare on content, not line endings — git may normalize CRLF/LF differently across machines.
	static string Normalize(string csv) => csv.Replace("\r\n", "\n");
}
