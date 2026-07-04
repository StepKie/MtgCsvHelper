using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace MtgCsvHelper.Tests;

/// <summary>
/// Anchors each writable format's declared <c>Columns</c> (the native header set + order the writer
/// emits) to a real site export captured under Resources/SampleCsvs. Round-trip tests are order-
/// independent, so without this a wrong column order would pass silently — defeating the purpose of
/// emitting the site's exact shape.
/// </summary>
public class NativeColumnOrderTests(ITestOutputHelper output) : BaseTest(output)
{
	const string ResourceRoot = "Resources/SampleCsvs";

	// Format → the real-export fixture whose header defines the native order; CARDKINGDOM is absent (write-only buylist, no site export).
	static readonly IReadOnlyDictionary<string, string> NativeHeaderFixtures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["MOXFIELD"] = "Tests/moxfield-real-export.csv",
		["MANABOX"] = "Tests/manabox-real-export.csv",
		["TOPDECKED"] = "Tests/topdecked-real-export.csv",
		["ARCHIDEKT"] = "Tests/archidekt-real-export.csv",
		["TCGPLAYER"] = "Tests/tcgplayer-real-export.csv",
		["MTGGOLDFISH"] = "Tests/mtggoldfish-real-export.csv",
		["DECKBOX"] = "Tests/deckbox-real-export.csv",
		["DRAGONSHIELD"] = "Tests/dragonshield-real-export.csv",
		["MTGO"] = "Collection/mtgo-collection.csv",
	};

	public static TheoryData<string> AnchoredFormats() => new(NativeHeaderFixtures.Keys);

	[Theory]
	[MemberData(nameof(AnchoredFormats))]
	public void DeclaredColumns_MatchRealExportHeader(string format)
	{
		var cfg = CardMapFactory.From(_config).First(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));
		var expected = ReadHeader(Path.Combine(ResourceRoot, NativeHeaderFixtures[format]));

		cfg.Columns.Should().Equal(expected,
			"the writer must emit each format's columns in the exact order the site exports them");
	}

	// A new writable format must be anchored to a captured export (or be the explicitly exempt buylist).
	[Fact]
	public void EveryWritableFormat_IsAnchoredOrExplicitlyExempt()
	{
		var unanchored = CardMapFactory.WritableFormats
			.Where(f => !NativeHeaderFixtures.ContainsKey(f) && !f.Equals("CARDKINGDOM", StringComparison.OrdinalIgnoreCase))
			.ToList();

		unanchored.Should().BeEmpty("a new writable format needs its Columns anchored to a captured site export");
	}

	// Header of a site export: skip the optional Excel "sep=" directive, then parse one CSV row to strip quoting.
	static string[] ReadHeader(string path)
	{
		var headerLine = File.ReadLines(path)
			.First(line => !string.IsNullOrWhiteSpace(line) && !CsvFixture.IsSepDirective(line));

		using var csv = new CsvReader(new StringReader(headerLine), new CsvConfiguration(CultureInfo.InvariantCulture));
		csv.Read();

		return csv.Parser.Record!;
	}
}
