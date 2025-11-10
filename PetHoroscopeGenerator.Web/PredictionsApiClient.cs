namespace PetHoroscopeGenerator.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;

public class PredictionsApiClient
{
    private readonly IMemoryCache _cache;
    private readonly string? _endpoint;
    private readonly string? _deployment;
    private readonly string? _key;
    private readonly string? _apiKey;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PredictionsApiClient(IConfiguration configuration, HttpClient httpClient, IMemoryCache cache, IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache;
        _endpoint = configuration["AZURE-OPENAI-ENDPOINT"];
        _deployment = configuration["AZURE-OPENAI-GPT-NAME"];
        _key = configuration["AZURE-OPENAI-KEY"];
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string?> GetPredictionAsync(string description, string previewURL, PredictionType type = PredictionType.Pet, CancellationToken cancellationToken = default)
    {
        if (!_httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? true)
        {
            return "Please sign in to see predictions...";
        }

        var cacheKey = $"{type}_{description}_{previewURL}";
        
        if (_cache.TryGetValue(cacheKey, out string? cachedResult))
        {
            return cachedResult;
        }

        if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_deployment) || string.IsNullOrEmpty(_key))
        {
            return "The crystal ball is cloudy... (Azure OpenAI configuration missing)";
        }

        try
        {
            var kernel = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(_deployment, _endpoint, _key)
                .Build();

            var prompt = GetPromptForType(type, description);
            var result = await kernel.InvokePromptAsync(prompt);
            _cache.Set(cacheKey, result.GetValue<string>(), TimeSpan.FromMinutes(30));
            return result.GetValue<string>();
        }
        catch (Exception ex)
        {
            // Log the error
            return "The crystal ball seems to be experiencing technical difficulties...";
        }
    }

    private string GetPromptForType(PredictionType type, string description)
    {
        return type switch
        {
            PredictionType.Plant => $"You are a mystical botanist who can read the aura of plants. " +
                                   $"Generate a whimsical and fun horoscope for a plant based on this description: {description}. " +
                                   $"Include predictions about growth, blooming potential, and plant happiness. Keep it light and fun!",
            
            PredictionType.Pet => $"You are a wise and slightly eccentric pet psychic. " +
                                 $"Generate a fun and playful horoscope for a pet based on this description: {description}. " +
                                 $"Include predictions about their mood, adventures, and treats in their future. Keep it magical and amusing!",
            
            PredictionType.Mythical => $"You are an ancient dragon sage with knowledge of all mythical creatures. " +
                                      $"Generate an epic and mystical horoscope for a mythical creature based on this description: {description}. " +
                                      $"Include predictions about their magical powers, legendary adventures, and destiny. " +
                                      $"Make it grand, fantastical, and filled with ancient wisdom!",
            
            _ => throw new ArgumentException("Invalid prediction type")
        };
    }
}

public enum PredictionType
{
    Pet,
    Plant,
    Mythical
}