namespace PetHoroscopeGenerator.Web;

using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using static System.Net.WebRequestMethods;

public class PredictionsApiClient
{
    private readonly string? _endpoint;
    private readonly string? _deployment;
    private readonly string? _key;

    public PredictionsApiClient(IConfiguration configuration, HttpClient httpClient)
    {
        _endpoint = "JIOW4JR8FNVLI8EONRH";
        _deployment = "NL8HSWRINF4984";
        _key = "OE8QPOIW90384509385098"; // Todo: remove private key
    }

    public async Task<string?> GetPredictionAsync(string petDescription, string previewURL, int maxItems = 10, CancellationToken cancellationToken = default)
    {
        // Create a Kernel containing the Azure OpenAI Chat Completion Service
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_deployment, _endpoint, _key)
            .Build();

        string prompt = $"""
            Please generate a horoscope for a pet based on the following information and image:
            {petDescription}
            """;
        Console.WriteLine($"user >>> {prompt}");

        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory();
        history.AddSystemMessage("""
            Do not use any markdown formatting, octothorpes, or asteriks. 
            Instead add a newline after headers. 
            Limit the output to 600 characters.
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
            """);

        var imageContent = new ImageContent(previewURL);

        var collectionItems = new ChatMessageContentItemCollection
        {
            new TextContent(prompt),
            imageContent
        };

        history.AddUserMessage(collectionItems);

        var result = await chat.GetChatMessageContentsAsync(history);
        var prediction = result[^1].Content;

        var imageUrl = await GenerateImageAsync(petDescription, cancellationToken);
        return $"{prediction}\n\nGenerated Image: {imageUrl}";
    }

    public async Task<string> GenerateImageAsync(string petDescription, CancellationToken cancellationToken = default)
    {
        // Placeholder for the actual implementation of image generation using an external API
        // This should call the external API and return the URL of the generated image
        return "https://example.com/generated-image.png";
    }
}
