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
        _endpoint = configuration["AZURE-OPENAI-ENDPOINT"];
        _deployment = configuration["AZURE-OPENAI-GPT-NAME"];
        _key = configuration["AZURE-OPENAI-KEY"];
    }
    public async Task<string?> GetPredictionAsync(string petDescription, string previewURL, int maxItems = 10, CancellationToken cancellationToken = default)
    {

        //var config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
        //string endpoint = config["AZURE_OPENAI_ENDPOINT"];
        //string deployment = config["AZURE_OPENAI_GPT_NAME"];
        //string key = config["AZURE_OPENAI_KEY"];

        // Create a Kernel containing the Azure OpenAI Chat Completion Service
        Kernel kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(_deployment, _endpoint, _key)
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

        //return await kernel.InvokePromptAsync<string>(prompt, new(new OpenAIPromptExecutionSettings() { MaxTokens = 400 }), cancellationToken: cancellationToken);
    }
}