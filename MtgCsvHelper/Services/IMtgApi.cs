using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	// FIXME Workaround since it is currently annoying to pass IMtgApi to CardNameConverter instance through DI
	public static IMtgApi Default { get; set; }

	IEnumerable<Set> GetSets();
	Task<IEnumerable<Set>> GetSetsAsync();

	List<string> GetDoubleFacedCardNames();
	Task<List<string>> GetDoubleFacedCardNamesAsync();
	Task LoadData();
}
