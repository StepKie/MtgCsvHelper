using System.Text.Json;

namespace MtgCsvHelper.Services;

public class ScryfallApi : IMtgApi
{


	public IEnumerable<Set> GetSets()
	{
		// URL of the API endpoint
		var apiUrl = "https://api.scryfall.com/sets/";

		// Download JSON data
		var jsonData = DownloadJsonAsync(apiUrl).GetAwaiter().GetResult();

		// Deserialize JSON data into IEnumerable<Set>
		var sets = DeserializeJson(jsonData);

		// Do something with the sets
		foreach (var set in sets)
		{
			Console.WriteLine($"Code: {set.Code}, FullName: {set.FullName}, ReleaseDate: {set.ReleaseDate}");
		}

		return sets;
	}

	static async Task<string> DownloadJsonAsync(string apiUrl)
	{
		using (var client = new HttpClient())
		{
			var response = await client.GetAsync(apiUrl);

			if (response.IsSuccessStatusCode)
			{
				return await response.Content.ReadAsStringAsync();
			}
			else
			{
				throw new HttpRequestException($"Failed to download data. Status code: {response.StatusCode}");
			}
		}
	}


	static IEnumerable<Set> DeserializeJson(string jsonData)
	{
		// Parse the JSON to a JsonDocument
		using (var jsonDocument = JsonDocument.Parse(jsonData))
		{
			// Access the "data" array
			var dataElement = jsonDocument.RootElement.GetProperty("data");

			// Deserialize the "data" array into List<Set>
			var sets = new List<Set>();
			foreach (var setElement in dataElement.EnumerateArray())
			{
				var set = new Set
				{
					FullName = setElement.GetProperty("name").GetString(),
					Code = setElement.GetProperty("code").GetString()!
				};
				sets.Add(set);
			}

			// Access the sets
			foreach (var set in sets)
			{
				Console.WriteLine($"Name: {set.FullName}, Code: {set.Code}");
			}


			// Use System.Text.Json.JsonSerializer to deserialize JSON into IEnumerable<Set>

			return sets;
		}

	}
}
