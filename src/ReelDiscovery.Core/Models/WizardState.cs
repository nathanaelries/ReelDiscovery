namespace ReelDiscovery.Models;

public class WizardState
{
    // Step 1 - API Configuration
    public string ApiKey { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = "gpt-4o-mini";
    public bool ConnectionTested { get; set; }

    // Provider selection (defaults to OpenAI for backward compatibility)
    public LlmProviderKind ProviderKind { get; set; } = LlmProviderKind.OpenAI;
    public string ProviderName { get; set; } = "OpenAI";
    public string? ProviderBaseUrl { get; set; }

    /// <summary>
    /// Optional OpenAI key used only for media generation (DALL-E images, TTS voicemails)
    /// when the text provider is not OpenAI. Images/voicemails are skipped without one.
    /// </summary>
    public string MediaApiKey { get; set; } = string.Empty;

    // Model Configurations
    public List<AIModelConfig> AvailableModelConfigs { get; set; } = AIModelConfig.GetDefaultModels();
    public AIModelConfig? SelectedModelConfig => AvailableModelConfigs.FirstOrDefault(m => m.ModelId == SelectedModel);

    // Token Usage Tracking
    public TokenUsageTracker UsageTracker { get; } = new();

    // Step 2 - Topic Input
    public string Topic { get; set; } = string.Empty;
    public string AdditionalInstructions { get; set; } = string.Empty;
    public int StorylineCount { get; set; } = 10;

    // Media type hints (inform storyline generation)
    public bool WantsDocuments { get; set; } = true;
    public bool WantsImages { get; set; } = false;
    public bool WantsVoicemails { get; set; } = false;

    // Step 3 - Storylines
    public List<Storyline> Storylines { get; set; } = new();
    public DateTime? AISuggestedStartDate { get; set; }
    public DateTime? AISuggestedEndDate { get; set; }
    public string? AISuggestedDateReasoning { get; set; }

    // Step 4 - Characters
    public List<Character> Characters { get; set; } = new();
    public string CompanyDomain { get; set; } = string.Empty;

    // Organization themes by domain (used for emails and documents)
    public Dictionary<string, OrganizationTheme> DomainThemes { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Step 5 - Generation Config
    public GenerationConfig Config { get; set; } = new();

    // Step 6-7 - Results
    public List<EmailThread> GeneratedThreads { get; set; } = new();
    public GenerationResult? Result { get; set; }

    // Helper to create OpenAI service with tracking (legacy; prefer CreateTextProvider)
    public Services.OpenAIService CreateOpenAIService()
    {
        var modelConfig = SelectedModelConfig;
        if (modelConfig != null)
        {
            return new Services.OpenAIService(ApiKey, modelConfig, UsageTracker, ProviderBaseUrl);
        }
        return new Services.OpenAIService(ApiKey, SelectedModel, ProviderBaseUrl);
    }

    /// <summary>Creates the text-generation provider for the selected provider/model.</summary>
    public Services.ILlmProvider CreateTextProvider()
    {
        var modelConfig = SelectedModelConfig
            ?? new AIModelConfig { ModelId = SelectedModel, DisplayName = SelectedModel };

        return ProviderKind switch
        {
            LlmProviderKind.Anthropic => new Services.AnthropicService(ApiKey, modelConfig, UsageTracker),
            // OpenAI native and OpenAI-compatible endpoints share the same client
            _ => new Services.OpenAIService(ApiKey, modelConfig, UsageTracker, ProviderBaseUrl)
        };
    }

    /// <summary>Image generation (DALL-E); null when no OpenAI key is available.</summary>
    public Services.IImageGenerationProvider? CreateImageProvider() => CreateMediaService();

    /// <summary>TTS voicemail generation; null when no OpenAI key is available.</summary>
    public Services.ISpeechGenerationProvider? CreateSpeechProvider() => CreateMediaService();

    public bool HasMediaProvider =>
        ProviderKind == LlmProviderKind.OpenAI
            ? !string.IsNullOrWhiteSpace(ApiKey)
            : !string.IsNullOrWhiteSpace(MediaApiKey);

    private Services.OpenAIService? CreateMediaService()
    {
        if (ProviderKind == LlmProviderKind.OpenAI && !string.IsNullOrWhiteSpace(ApiKey))
        {
            return CreateOpenAIService();
        }
        if (!string.IsNullOrWhiteSpace(MediaApiKey))
        {
            // Model id is irrelevant for image/TTS calls; key targets api.openai.com
            return new Services.OpenAIService(MediaApiKey, "gpt-4o-mini");
        }
        return null;
    }

    // Legacy - Available OpenAI models (for backward compatibility)
    public static readonly string[] AvailableModels = new[]
    {
        "gpt-4o",
        "gpt-4o-mini",
        "gpt-4-turbo",
        "gpt-4",
        "gpt-3.5-turbo"
    };
}
