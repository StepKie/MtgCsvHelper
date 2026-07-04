namespace MtgCsvHelper.Tests;

public class CurrencyTests
{
	[Theory]
	[InlineData("Start", CurrencySymbolPosition.Start)]
	[InlineData("End", CurrencySymbolPosition.End)]
	[InlineData("Absent", CurrencySymbolPosition.Absent)]
	[InlineData("InvalidPosition", null)]
	public void SymbolFromString_MapsKnownPositions_NullOtherwise(string input, CurrencySymbolPosition? expected)
	{
		Currency.SymbolFromString(input).Should().Be(expected);
	}

	[Theory]
	[InlineData("USD", "$50.25")]
	[InlineData("USD", "50.25$")]
	[InlineData("USD", "50.25")]
	[InlineData("EUR", "€50.25")]
	[InlineData("EUR", "50.25€")]
	[InlineData("EUR", "50.25")]
	public void MoneyParse_AcceptsAnySymbolPosition(string currency, string input)
	{
		var curr = Currency.FromString(currency);

		Money.Parse(input, curr).Should().BeEquivalentTo(new Money(50.25m, curr));
	}

	[Theory]
	[InlineData("USD", CurrencySymbolPosition.Start, "$50.25")]
	[InlineData("USD", CurrencySymbolPosition.End, "50.25$")]
	[InlineData("USD", CurrencySymbolPosition.Absent, "50.25")]
	[InlineData("EUR", CurrencySymbolPosition.Start, "€50.25")]
	[InlineData("EUR", CurrencySymbolPosition.End, "50.25€")]
	[InlineData("EUR", CurrencySymbolPosition.Absent, "50.25")]
	public void MoneyPrint_PlacesSymbolPerPosition(string currency, CurrencySymbolPosition position, string expected)
	{
		new Money(50.25m, Currency.FromString(currency)).Print(position).Should().Be(expected);
	}
}
