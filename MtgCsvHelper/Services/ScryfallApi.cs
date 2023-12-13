using System.Text.Json;

namespace MtgCsvHelper.Services;

public class ScryfallApi : IMtgApi
{


	public IEnumerable<Set> GetSets()
	{
		var apiUrl = "https://api.scryfall.com/sets/";
		var jsonData = DownloadJsonAsync(apiUrl).GetAwaiter().GetResult();
		var sets = DeserializeJson(jsonData);
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
		using var jsonDocument = JsonDocument.Parse(jsonData);
		var dataElement = jsonDocument.RootElement.GetProperty("data");

		var sets = dataElement.EnumerateArray().Select(ConvertToSet).ToList();

		return sets;

		Set ConvertToSet(JsonElement setElement) => new()
		{
			FullName = setElement.GetProperty("name").GetString(),
			Code = setElement.GetProperty("code").GetString()!
		};
	}
}
