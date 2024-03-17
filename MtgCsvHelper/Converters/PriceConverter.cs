using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using MtgCsvHelper.Maps;

namespace MtgCsvHelper.Converters;

public class PriceConverter(PriceConfiguration configuration) : ITypeConverter
{
	readonly CurrencySymbolPosition _currencyPos = Currency.SymbolFromString(configuration.CurrencySymbol) ?? CurrencySymbolPosition.Absent;
	readonly Currency _currency = Currency.FromString(configuration.Currency);

	public object? ConvertFromString(string? text, IReaderRow row, MemberMapData memberMapData) => string.IsNullOrEmpty(text) ? null : Money.Parse(text, _currency);

	public string? ConvertToString(object? value, IWriterRow row, MemberMapData memberMapData) => value is Money m ? m.Print(_currencyPos) : "";
}

