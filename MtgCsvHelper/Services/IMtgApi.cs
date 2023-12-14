using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	IEnumerable<Set> GetSets();
	Task<IEnumerable<Set>> GetSetsAsync();

	List<string> GetDoubleFacedCardNames();
	Task<List<string>> GetDoubleFacedCardNamesAsync();
	Task LoadData();
}
