namespace PetHoroscopeGenerator.Web;

public class LuckyNumbersApiClient
{
    private readonly HttpClient _httpClient;

    public LuckyNumbersApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<string> GetLuckyNumbersAsync()
    {
        var response = await _httpClient.GetFromJsonAsync<string[]>("/api/luckynumbers");
        return response != null ? string.Join(", ", response) : string.Empty;
    }
}
