namespace PetHoroscopeGenerator.Web;

using GitHub.Copilot;
using System.Text;

public class PredictionsApiClient(IConfiguration configuration)
{
    public async Task<string?> GetPredictionAsync(string? petDescription, string? previewUrl, int maxItems = 10, CancellationToken cancellationToken = default)
    {
        var model = configuration["GitHubCopilot:Model"];
        var timeoutSeconds = int.TryParse(configuration["GitHubCopilot:TimeoutSeconds"], out var configuredTimeoutSeconds)
            ? configuredTimeoutSeconds
            : 60;
        // Changed: read optional Copilot credentials only from environment variables so tokens are not encouraged in appsettings.
        var gitHubToken = Environment.GetEnvironmentVariable("GITHUB_COPILOT_TOKEN")
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        var prompt = $"""
            Please generate a horoscope for a pet based on the following information:
            {petDescription}
            """;

        var assistantResponse = new StringBuilder();
        string? errorMessage = null;
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var options = new CopilotClientOptions
        {
            GitHubToken = string.IsNullOrWhiteSpace(gitHubToken) ? null : gitHubToken,
            UseLoggedInUser = string.IsNullOrWhiteSpace(gitHubToken)
        };

        await using var client = new CopilotClient(options);
        await client.StartAsync();

        await using var session = await client.CreateSessionAsync(new SessionConfig
        {
            Model = string.IsNullOrWhiteSpace(model) ? null : model,
            InfiniteSessions = new InfiniteSessionConfig { Enabled = false },
            SkipCustomInstructions = true,
            EnableConfigDiscovery = false,
            AvailableTools = [],
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Replace,
                // Changed: preserve the existing horoscope format while replacing Azure OpenAI/Semantic Kernel with GitHub Copilot SDK.
                Content = """
            Do not use any markdown formatting, octothorpes, or asterisks. 
            Instead add a newline after headers. 
            Limit the output to 800 characters.
            Use the provided image, if one is attached, to say something specific about the pet.
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
            """
            }
        });

        // Changed: subscribe before SendAsync so horoscope response events are captured for this prompt.
        using var subscription = session.On<SessionEvent>(evt =>
        {
            switch (evt)
            {
                case AssistantMessageEvent message when !string.IsNullOrWhiteSpace(message.Data.Content):
                    assistantResponse.Append(message.Data.Content);
                    break;
                case SessionErrorEvent sessionError:
                    errorMessage = sessionError.Data.Message ?? "GitHub Copilot SDK returned an error.";
                    done.TrySetResult();
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
            }
        });
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        using var cancellation = timeout.Token.Register(() => done.TrySetCanceled(timeout.Token));
        timeout.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        await session.SendAsync(new MessageOptions
        {
            Prompt = prompt,
            Attachments = CreateAttachments(previewUrl)
        });
        await done.Task;

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            throw new InvalidOperationException(errorMessage);
        }

        return assistantResponse.Length > 0
            ? assistantResponse.ToString()
            : "The stars are quiet right now. Please try again.";
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

    private static IList<Attachment>? CreateAttachments(string? previewUrl)
    {
        var attachment = CreateImageAttachment(previewUrl);
        return attachment is null ? null : [attachment];
    }

    private static AttachmentBlob? CreateImageAttachment(string? previewUrl)
    {
        if (string.IsNullOrWhiteSpace(previewUrl) || !previewUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var commaIndex = previewUrl.IndexOf(',');
        if (commaIndex < 0)
        {
            return null;
        }

        var metadata = previewUrl[5..commaIndex];
        if (!metadata.EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var mimeType = metadata[..^7];
        var base64Data = previewUrl[(commaIndex + 1)..];

        return new AttachmentBlob
        {
            Data = base64Data,
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "image/png" : mimeType,
            DisplayName = "pet-image"
        };
    }
}

public enum PredictionType
{
    Pet,
    Plant,
    Mythical
}