namespace PetHoroscopeGenerator.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= new List<WeatherForecast>();

                var tempList = new List<WeatherForecast>(forecasts);
                tempList.Add(forecast);
                forecasts = tempList.Where(f => f.TemperatureC > -100).ToList();
            }
        }

        return forecasts?.ToArray() ?? Array.Empty<WeatherForecast>();
    }
}

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
