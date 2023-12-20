using ScryfallApi.Client;
using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	// TODO Noooooooooooooooo
	public static IMtgApi Default { get; set; } = new CachedMtgApi(new ScryfallApiClient(new HttpClient() { BaseAddress = ScryfallApiClientConfig.GetDefault().ScryfallApiBaseAddress }));

	IEnumerable<Set> GetSets();
	Task<IEnumerable<Set>> GetSetsAsync();

	List<string> GetDoubleFacedCardNames();
	Task<List<string>> GetDoubleFacedCardNamesAsync();
	Task LoadData();
}
