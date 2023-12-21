namespace MtgCsvHelper.Services;

interface IMtgCardCsvHandlerService
{
	MtgCardCsvHandler GetHandler(string id);

	DeckFormat GetFormat(string id);
}
