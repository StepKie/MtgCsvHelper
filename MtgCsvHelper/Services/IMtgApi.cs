﻿using ScryfallApi.Client.Models;

namespace MtgCsvHelper.Services;

public interface IMtgApi
{
	// FIXME Workaround since it is currently annoying to pass IMtgApi to CardNameConverter instance through DI
	static IMtgApi Default { get; set; }

	IEnumerable<Set> GetSets();
	Task<IEnumerable<Set>> GetSetsAsync();

	List<string> GetDoubleFacedCardNames();
	Task<List<string>> GetDoubleFacedCardNamesAsync();

	Task<IEnumerable<Card>> GetTokenCardNamesAsync();
	List<string> GetTokenCardNames();

	Task LoadData();
}
