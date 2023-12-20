namespace MtgCsvHelper.Models.Tests;

public class CurrencyAndMoneyTests
{

	[Theory]
	[InlineData("Start", CurrencySymbolPosition.Start)]
	[InlineData("End", CurrencySymbolPosition.End)]
	[InlineData("Absent", CurrencySymbolPosition.Absent)]
	[InlineData("InvalidPosition", null)]
	public void GetSymbolPosition_ShouldReturnCorrectEnumValue(string input, CurrencySymbolPosition? expectedPosition)
	{
		// Act
		var actualPosition = Currency.SymbolFromString(input);

		// Assert
		actualPosition.Should().Be(expectedPosition);
	}

	[Theory]
	[InlineData("USD", "$50.25")]
	[InlineData("USD", "50.25$")]
	[InlineData("USD", "50.25")]
	[InlineData("EUR", "€50.25")]
	[InlineData("EUR", "50.25€")]
	[InlineData("EUR", "50.25")]
	public void Money_Parse_ShouldReturnMoneyObjectWithCorrectValueAndCurrency(string currency, string input)
	{
		// Arrange
		var curr = Currency.FromString(currency);
		var expectedMoney = new Money(50.25m, curr);

		// Act
		var actualMoney = Money.Parse(input, curr);

		// Assert
		actualMoney.Should().BeEquivalentTo(expectedMoney);
	}

	[Theory]
	[InlineData("USD", CurrencySymbolPosition.Start, "$50.25")]
	[InlineData("USD", CurrencySymbolPosition.End, "50.25$")]
	[InlineData("USD", CurrencySymbolPosition.Absent, "50.25")]
	[InlineData("EUR", CurrencySymbolPosition.Start, "€50.25")]
	[InlineData("EUR", CurrencySymbolPosition.End, "50.25€")]
	[InlineData("EUR", CurrencySymbolPosition.Absent, "50.25")]
	public void Money_Print_ShouldReturnFormattedStringBasedOnSymbolPosition(string currency, CurrencySymbolPosition position, string expectedOutput)
	{
		// Arrange
		var money = new Money(50.25m, Currency.FromString(currency));

		// Act
		var actualOutput = money.Print(position);

		// Assert
		actualOutput.Should().Be(expectedOutput);
	}
}
