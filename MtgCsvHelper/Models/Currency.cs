using System.Globalization;

namespace MtgCsvHelper.Models;

public record Currency(string Symbol, string ShortName, string LongName, NumberFormatInfo NumberFormat)
{
	// Specific pattern to allow something like €0.20
	static NumberFormatInfo EUR_WITH_DOT()
	{
		var nfi = CultureInfo.CreateSpecificCulture("de-DE").NumberFormat;
		nfi.NumberDecimalSeparator = NumberFormatInfo.InvariantInfo.NumberDecimalSeparator;
		nfi.CurrencyDecimalSeparator = NumberFormatInfo.InvariantInfo.CurrencyDecimalSeparator;
		nfi.CurrencyPositivePattern = NumberFormatInfo.InvariantInfo.CurrencyPositivePattern;
		return nfi;
	}

	public static readonly Currency UNKNOWN = new("?", "?", "?", CultureInfo.InvariantCulture.NumberFormat);
	public static readonly Currency EUR = new("€", "EUR", "Euro", EUR_WITH_DOT());
	public static readonly Currency USD = new("$", "USD", "US Dollar", CultureInfo.CreateSpecificCulture("en-US").NumberFormat);

	public static readonly List<Currency> SupportedCurrencies = [EUR, USD];

	public static Currency FromString(string? input) => input switch
	{
		nameof(EUR) => EUR,
		nameof(USD) => USD,
		_ => UNKNOWN,
	};

	public static CurrencySymbolPosition? SymbolFromString(string input) => input switch
	{
		nameof(CurrencySymbolPosition.Start) => CurrencySymbolPosition.Start,
		nameof(CurrencySymbolPosition.End) => CurrencySymbolPosition.End,
		nameof(CurrencySymbolPosition.Absent) => CurrencySymbolPosition.Absent,
		_ => null
	};
}

public record Money(decimal Value, Currency Currency)
{
	public static Money Parse(string input, Currency currency)
	{
		var value = decimal.Parse(input, NumberStyles.Currency, currency.NumberFormat);
		return new Money(value, currency);
	}

	public string Print(CurrencySymbolPosition pos = CurrencySymbolPosition.Start)
	{
		var value = Value.ToString(Currency.NumberFormat);
		return pos switch
		{
			CurrencySymbolPosition.Start => $"{Currency.Symbol}{value}",
			CurrencySymbolPosition.End => $"{value}{Currency.Symbol}",
			CurrencySymbolPosition.Absent or _ => value,
		};
	}
}

public enum CurrencySymbolPosition
{
	Start,
	End,
	Absent
}
