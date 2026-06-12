using ScryfallApi.Client.Models;
using Serilog;

namespace MtgCsvHelper.Tests;

[Collection(CatalogCollection.Name)]
public class MtgCardCsvHandlerTests(CatalogFixture fixture, ITestOutputHelper output) : ApiBaseTest(fixture, output)
{
	public const string TESTS_FOLDER = "Resources/SampleCsvs/Tests";
	public const string COLLECTIONS_FOLDER = "Resources/SampleCsvs/Collection";

	/// <summary>
	/// Every format that can both read and write must round-trip the reference collection. A format
	/// preserves only the fields it has a column for, so the comparison excludes what it structurally
	/// cannot carry (derived from its config below) rather than hard-coding per-format expectations.
	/// </summary>
	public static TheoryData<string> RoundTrippableFormats()
	{
		var data = new TheoryData<string>();
		foreach (var format in CardMapFactory.WritableFormats.Intersect(CardMapFactory.ReadableFormats, StringComparer.OrdinalIgnoreCase))
		{
			data.Add(format);
		}

		return data;
	}

	/// <summary>
	/// Conditions a format's vocabulary can't represent, with the grade each deterministically degrades
	/// to on write. Folded into the expected cards so the round-trip still asserts the (lossy) result
	/// rather than ignoring the field — drift in the remap then fails the test instead of passing silently.
	/// </summary>
	static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<CardCondition, CardCondition>> ConditionDegradations =
		new Dictionary<string, IReadOnlyDictionary<CardCondition, CardCondition>>(StringComparer.OrdinalIgnoreCase)
		{
			// Archidekt has no Mint tier; Mint degrades to NearMint.
			["ARCHIDEKT"] = new Dictionary<CardCondition, CardCondition> { [CardCondition.Mint] = CardCondition.NearMint },
			// TCGplayer maps both Good and Excellent to "Lightly Played"; Good reads back as Excellent.
			["TCGPLAYER"] = new Dictionary<CardCondition, CardCondition> { [CardCondition.Good] = CardCondition.Excellent },
		};

	/// <summary>Finishes a format's vocabulary can't represent, with the finish each degrades to on write.</summary>
	static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<CardFinish, CardFinish>> FinishDegradations =
		new Dictionary<string, IReadOnlyDictionary<CardFinish, CardFinish>>(StringComparer.OrdinalIgnoreCase)
		{
			// No etched tier — etched degrades to foil on write.
			["DRAGONSHIELD"] = new Dictionary<CardFinish, CardFinish> { [CardFinish.Etched] = CardFinish.Foil },
			["TCGPLAYER"] = new Dictionary<CardFinish, CardFinish> { [CardFinish.Etched] = CardFinish.Foil },
			["MTGO"] = new Dictionary<CardFinish, CardFinish> { [CardFinish.Etched] = CardFinish.Foil },
			["DECKBOX"] = new Dictionary<CardFinish, CardFinish> { [CardFinish.Etched] = CardFinish.Foil },
		};

	/// <summary>
	/// The canonical printings the field-fidelity fixtures are built from — the Ambitious Farmhand
	/// language/condition block plus the two tokens. <see cref="ParseSampleCsv_WithValidInput_ParsesCards"/>
	/// asserts a true bijection against exactly the master rows for these (Name, Set, CollectorNumber)
	/// printings, so a language- or condition-twin mis-parse fails instead of silently matching a
	/// different row that shares every other field. All five fixtures share this coverage today; split
	/// this per-fixture if one ever diverges.
	/// </summary>
	static readonly HashSet<(string? Name, string? Set, string? CollectorNumber)> FieldFidelityPrintings =
	[
		("Ambitious Farmhand // Seasoned Cathar", "MID", "2"),
		("Clue", "TMH2", "14"),
		("Food", "TLTR", "10"),
	];

	[Theory]
	[MemberData(nameof(RoundTrippableFormats))]
	public void ReferenceCollection_RoundTripsThroughFormat(string format)
	{
		var cfg = CardMapFactory.From(_config).First(c => c.Name.Equals(format, StringComparison.OrdinalIgnoreCase));
		MtgCardCsvHandler handler = CreateHandler(format);
		IList<PhysicalMtgCard> originalCards = CanonicalReference.LoadCards(_config, _catalog, _resolver, cfg.Currency);

		string fileName = $"unittest-roundtrip-{format}.csv";
		handler.WriteCollectionCsv(originalCards, fileName);
		List<PhysicalMtgCard> parsedCards = handler.ParseCollectionCsv(fileName).Collection.Cards;

		// Fold each format's deterministic remaps (a grade/finish it can't carry) into the expected cards.
		var condDeg = ConditionDegradations.GetValueOrDefault(format);
		var finDeg = FinishDegradations.GetValueOrDefault(format);
		var expectedCards = originalCards.Select(c =>
		{
			if (condDeg is not null && condDeg.TryGetValue(c.Condition, out var cond)) { c = c with { Condition = cond }; }
			if (finDeg is not null && finDeg.TryGetValue(c.Finish, out var fin)) { c = c with { Finish = fin }; }

			return c;
		}).ToList();

		// Exclude fields the format has no column for; DragonShield also rewrites null Folder/Price/Date.
		parsedCards.Should().BeEquivalentTo(expectedCards, opts =>
		{
			if (cfg.Condition is null) { opts = opts.Excluding(c => c.Condition); }
			if (cfg.Language is null) { opts = opts.Excluding(c => c.Language); }
			if (cfg.PriceBought is null || cfg.RequiresWriteDefaults) { opts = opts.Excluding(c => c.PriceBought); }
			if (cfg.DateBought is null || cfg.RequiresWriteDefaults) { opts = opts.Excluding(c => c.DateBought); }
			if (cfg.RequiresWriteDefaults) { opts = opts.Excluding(c => c.Folder); }

			return opts;
		});
	}

	// Demonic Tutor CMM #509 has an etched printing.
	[Theory]
	[InlineData("MOXFIELD")]
	[InlineData("MANABOX")]
	[InlineData("TOPDECKED")]
	[InlineData("ARCHIDEKT")]
	public void EtchedFinish_RoundTripsThroughEtchedSupportingFormats(string format)
	{
		var handler = CreateHandler(format);
		var etched = EtchedDemonicTutor();

		string fileName = $"unittest-etched-{format}.csv";
		handler.WriteCollectionCsv([etched], fileName);
		var parsed = handler.ParseCollectionCsv(fileName).Collection.Cards;

		parsed.Should().ContainSingle().Which.Finish.Should().Be(CardFinish.Etched);
	}

	[Fact]
	public void EtchedFinish_CollapsesToFoil_WhenFormatHasNoEtchedTier()
	{
		// DragonShield configures no etched string; etched must degrade to foil, not error or vanish.
		var handler = CreateHandler("DRAGONSHIELD");

		string fileName = "unittest-etched-DRAGONSHIELD.csv";
		handler.WriteCollectionCsv([EtchedDemonicTutor()], fileName);
		var parsed = handler.ParseCollectionCsv(fileName).Collection.Cards;

		parsed.Should().ContainSingle().Which.Finish.Should().Be(CardFinish.Foil);
	}

	static PhysicalMtgCard EtchedDemonicTutor() => new()
	{
		Count = 1,
		Condition = CardCondition.NearMint,
		Finish = CardFinish.Etched,
		Language = "en",
		Printing = new Card { Name = "Demonic Tutor", Set = "CMM", SetName = "Commander Masters", CollectorNumber = "509" },
	};

	[Theory]
	[InlineData($"{TESTS_FOLDER}/dragonshield-field-fidelity.csv", "DRAGONSHIELD", "USD")]
	[InlineData($"{TESTS_FOLDER}/moxfield-field-fidelity.csv", "MOXFIELD", "EUR")]
	[InlineData($"{TESTS_FOLDER}/manabox-field-fidelity.csv", "MANABOX", "USD")]
	[InlineData($"{TESTS_FOLDER}/topdecked-field-fidelity.csv", "TOPDECKED", "USD")]
	[InlineData($"{TESTS_FOLDER}/deckbox-field-fidelity.csv", "DECKBOX", "USD")]
	public void ParseSampleCsv_WithValidInput_ParsesCards(string csvFilePath, string deckFormatName, string currency)
	{
		// Arrange
		MtgCardCsvHandler handler = CreateHandler(deckFormatName);
		IList<PhysicalMtgCard> expectedCards = CanonicalReference.LoadCards(_config, _catalog, _resolver, Currency.FromString(currency));
		var expectedSubset = expectedCards
			.Where(c => FieldFidelityPrintings.Contains((c.Printing.Name, c.Printing.Set, c.Printing.CollectorNumber)))
			.ToList();

		// Act
		var result = handler.ParseCollectionCsv(csvFilePath);
		IList<PhysicalMtgCard> cards = result.Collection.Cards;

		// Assert — every data row parses into its exact canonical twin: no errors, no silent drops
		result.ErrorCount.Should().Be(0, $"every field-fidelity row must parse. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");
		cards.Should().HaveCount(CsvFixture.CountDataRows(csvFilePath), "no data row may be silently dropped");
		cards.Should().BeEquivalentTo(expectedSubset, "each row must parse to its exact canonical twin — matching Language and Condition — not merely some card sharing the other fields");
	}

	[Theory]
	[InlineData($"{COLLECTIONS_FOLDER}/dragonshield-collection.csv", "DRAGONSHIELD", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-haves.csv", "MOXFIELD", "DRAGONSHIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/moxfield-haves.csv", "MOXFIELD", "TOPDECKED")]
	[InlineData($"{COLLECTIONS_FOLDER}/topdecked-collection.csv", "TOPDECKED", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/manabox-collection.csv", "MANABOX", "CARDKINGDOM")]
	[InlineData($"{COLLECTIONS_FOLDER}/mtggoldfish-collection.csv", "MTGGOLDFISH", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/mtggoldfish-from-mtgarena.csv", "MTGGOLDFISH", "MOXFIELD")]
	[InlineData($"{COLLECTIONS_FOLDER}/tcgplayer-collection.csv", "TCGPLAYER", "MANABOX")]
	public void ConvertCollectionCsvTest(string csvFilePath, string deckFormatIn, string deckFormatOut)
	{
		// Arrange
		MtgCardCsvHandler handlerIn = CreateHandler(deckFormatIn);
		MtgCardCsvHandler handlerOut = CreateHandler(deckFormatOut);

		var result = handlerIn.ParseCollectionCsv(csvFilePath);
		var cards = result.Collection.Cards;
		var resultFileName = $"unittest_{deckFormatIn}-to-{deckFormatOut}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
		handlerOut.WriteCollectionCsv(cards, resultFileName);

		Log.Information(result.Collection.GenerateSummary());
		cards.Should().HaveCountGreaterThan(500);
	}

	[Fact]
	public void Mtgo_AllRowsParse_WithCollectorNumberStrippedAndMtgoCodeAliased()
	{
		// MTGO fixture has 7 rows of old-set printings (Mirage, Visions, Tempest, Exodus).
		// Two MTGO-specific quirks the parser handles: collector numbers in "N/M" form
		// (116/350 → 116) and 2-letter set codes (MI → MIR via the bundled mtgo_code map).
		var handler = CreateHandler("MTGO");
		var result = handler.ParseCollectionCsv($"{COLLECTIONS_FOLDER}/mtgo-collection.csv");

		result.Collection.Cards.Should().HaveCount(7);
		result.ErrorCount.Should().Be(0,
			$"all 7 rows are real printings with valid (Set, Collector#) once aliased. Issues: {string.Join("; ", result.Issues.Select(i => i.Reason))}");

		// "116/350" → "116" stripping verification.
		result.Collection.Cards.Select(c => c.Printing.CollectorNumber)
			.Should().AllSatisfy(n => n.Should().NotContain("/"));

		// MTGO 2-letter codes (MI, VI, TE, EX) must be rewritten to canonical 3-letter Scryfall
		// codes (MIR, VIS, TMP, EXO) so MTGO → other-format conversions emit the right code.
		result.Collection.Cards.Should().AllSatisfy(c =>
			c.Printing.Set.Should().HaveLength(3, "MTGO 2-letter codes should be resolved to canonical Scryfall codes"));
	}

	[Fact]
	public void Mtgo_ConvertToMoxfield_EmitsCanonicalSetCodesInOutput()
	{
		// End-to-end through the WRITE path: parse MTGO, write to Moxfield, then inspect the
		// emitted CSV. Catches any regression that bypasses the in-memory canonicalization but
		// still produces a non-canonical code in the output column.
		var reader = CreateHandler("MTGO");
		var writer = CreateHandler("MOXFIELD");

		var cards = reader.ParseCollectionCsv($"{COLLECTIONS_FOLDER}/mtgo-collection.csv").Collection.Cards;
		using var output = new MemoryStream();
		writer.WriteCollectionCsv(cards, output);
		var csv = System.Text.Encoding.UTF8.GetString(output.ToArray());

		// Each canonical 3-letter code from the 7 fixture rows must appear in the output.
		foreach (var canonical in new[] { "MIR", "VIS", "TMP", "EXO" })
		{
			csv.Should().Contain(canonical, $"row(s) of {canonical} should round-trip through the write path");
		}
		// And no MTGO 2-letter code should leak through.
		foreach (var mtgo in new[] { ",MI,", ",VI,", ",TE,", ",EX," })
		{
			csv.Should().NotContain(mtgo, $"MTGO 2-letter code {mtgo.Trim(',')} must be canonicalized before write");
		}
	}

	[Fact]
	public void RequiresWriteDefaults_FillsNullFolderPriceDate_AndDoesNotMutateInput()
	{
		var handler = CreateHandler("DRAGONSHIELD");
		var input = new List<PhysicalMtgCard>
		{
			new() { Count = 1, Printing = new Card { Name = "Lightning Bolt", Set = "M11", SetName = "Magic 2011", CollectorNumber = "149" } },
			new() { Count = 1, Printing = new Card { Name = "Llanowar Elves", Set = "M11", SetName = "Magic 2011", CollectorNumber = "182" },
				Folder = "MyDeck", PriceBought = new Money(2.50m, Currency.FromString("USD")), DateBought = new DateTime(2024, 1, 1) },
		};

		using var output = new MemoryStream();
		handler.WriteCollectionCsv(input, output);
		var csv = System.Text.Encoding.UTF8.GetString(output.ToArray());

		// Null/unset fields get the DS defaults.
		csv.Should().Contain("Imported,1").And.Contain("Lightning Bolt");
		csv.Should().Contain(DateTime.Today.ToString("yyyy-MM-dd"));

		// Source-set fields are preserved verbatim.
		csv.Should().Contain("MyDeck,1").And.Contain("Llanowar Elves");
		csv.Should().Contain("2024-01-01");

		// Caller's input is NOT mutated — the projection should leave the records immutable.
		input[0].Folder.Should().BeNull("write-defaults must project, not mutate, the input list");
		input[0].PriceBought.Should().BeNull();
		input[0].DateBought.Should().BeNull();
	}

	MtgCardCsvHandler CreateHandler(string deckFormatName) => new(_catalog, _resolver, _config, deckFormatName);
}
