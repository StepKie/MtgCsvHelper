namespace MtgCsvHelper.Models;

interface ICardInfo
{
	public IEnumerable<CardCollectionEntry> GetEntries();
}
