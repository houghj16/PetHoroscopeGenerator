namespace PetHoroscopeGenerator.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using static System.Net.WebRequestMethods;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;

/* Resolved conflict: keep main's user-secrets credential handling + chat history approach,
   add branch's PredictionType enum, GetPromptForType, and GenerateImageAsync */
public class PredictionsApiClient(HttpClient httpClient)
{
    public async Task<string?> GetPredictionAsync(string petDescription, string previewURL, int maxItems = 10, CancellationToken cancellationToken = default)
    {

        var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        string endpoint = config["AZURE_OPENAI_ENDPOINT"];
        string deployment = config["AZURE_OPENAI_GPT_NAME"];
        string key = config["AZURE_OPENAI_KEY"];

        // Create a Kernel containing the Azure OpenAI Chat Completion Service
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(deployment, endpoint, key)
            .Build();

        // Create and print out the prompt
        string prompt = $"""
            Please generate a horoscope for a pet based on the following information and image:
            {petDescription}
            """;
        Console.WriteLine($"user >>> {prompt}");

        // Create a ChatHistory object and add the system message
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage("""
            Do not use any markdown formatting, octothorpes, or asteriks. 
            Instead add a newline after headers. 
            Limit the output to 800 characters.
            Use the provided image to say something specific about the dog.
            Include a horoscope for the day and one line sections for a lucky treat, a favorite toy, a fun activity, and what to watch out for.
            Format responses like this:
            `[pet name] Pet Horoscope *emojis*

            [horoscope]

            Lucky Treat: [treat] *emojis*  
            Favorite Toy: [toy] *emojis*
            Fun Activity: [activity] *emojis*
            Watch Out For: [danger] *emojis*`
            Add emojis to make the tone playful.
            Add a silly sign off from the cat wizard.
            """);

        // Add the image and userMessage message to the ChatHistory
        var imageContent = new ImageContent(previewURL);

        var collectionItems = new ChatMessageContentItemCollection
        {
            new TextContent(prompt),
            imageContent
        };

        history.AddUserMessage(collectionItems);

        var result = await chat.GetChatMessageContentsAsync(history);
        return result[^1].Content;
    }

    public async Task<string> GenerateImageAsync(string petDescription, CancellationToken cancellationToken = default)
    {
        // Placeholder for the actual implementation of image generation using an external API
        return "https://example.com/generated-image.png";
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