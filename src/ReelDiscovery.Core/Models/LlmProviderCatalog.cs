namespace ReelDiscovery.Models;

public enum LlmProviderKind
{
    /// <summary>OpenAI's native API (also supports DALL-E images and TTS voicemails).</summary>
    OpenAI,

    /// <summary>Anthropic's native Messages API (Claude models).</summary>
    Anthropic,

    /// <summary>Any endpoint speaking the OpenAI chat-completions wire format
    /// (xAI, Google Gemini, Groq/Meta Llama, Ollama, LM Studio, vLLM, ...).</summary>
    OpenAICompatible
}

public class LlmProviderPreset
{
    public string Name { get; set; } = string.Empty;
    public LlmProviderKind Kind { get; set; }

    /// <summary>Base URL for OpenAI-compatible endpoints; null for native APIs.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Environment variable checked for a default API key.</summary>
    public string ApiKeyEnvVar { get; set; } = string.Empty;

    /// <summary>True when this provider can also generate images (DALL-E) and TTS audio.</summary>
    public bool SupportsMedia { get; set; }

    public string? Notes { get; set; }

    public List<AIModelConfig> Models { get; set; } = new();

    public AIModelConfig? DefaultModel =>
        Models.FirstOrDefault(m => m.IsDefault) ?? Models.FirstOrDefault();
}

/// <summary>
/// Built-in provider presets. Pricing is per million tokens; 0 means unknown
/// (cost tracking will report $0 until configured). New providers that expose
/// an OpenAI-compatible endpoint only need a new entry here, no code.
/// </summary>
public static class LlmProviderCatalog
{
    public static List<LlmProviderPreset> All => new()
    {
        new LlmProviderPreset
        {
            Name = "OpenAI",
            Kind = LlmProviderKind.OpenAI,
            ApiKeyEnvVar = "OPENAI_API_KEY",
            SupportsMedia = true,
            Models = AIModelConfig.GetDefaultModels()
        },
        new LlmProviderPreset
        {
            Name = "Anthropic (Claude)",
            Kind = LlmProviderKind.Anthropic,
            ApiKeyEnvVar = "ANTHROPIC_API_KEY",
            Models = new()
            {
                new AIModelConfig { ModelId = "claude-opus-4-8", DisplayName = "Claude Opus 4.8", InputTokenPricePerMillion = 5.00m, OutputTokenPricePerMillion = 25.00m, IsDefault = true },
                new AIModelConfig { ModelId = "claude-fable-5", DisplayName = "Claude Fable 5", InputTokenPricePerMillion = 10.00m, OutputTokenPricePerMillion = 50.00m },
                new AIModelConfig { ModelId = "claude-sonnet-4-6", DisplayName = "Claude Sonnet 4.6", InputTokenPricePerMillion = 3.00m, OutputTokenPricePerMillion = 15.00m },
                new AIModelConfig { ModelId = "claude-haiku-4-5", DisplayName = "Claude Haiku 4.5", InputTokenPricePerMillion = 1.00m, OutputTokenPricePerMillion = 5.00m }
            }
        },
        new LlmProviderPreset
        {
            Name = "xAI (Grok)",
            Kind = LlmProviderKind.OpenAICompatible,
            BaseUrl = "https://api.x.ai/v1",
            ApiKeyEnvVar = "XAI_API_KEY",
            Models = new()
            {
                new AIModelConfig { ModelId = "grok-4", DisplayName = "Grok 4", IsDefault = true },
                new AIModelConfig { ModelId = "grok-3", DisplayName = "Grok 3" },
                new AIModelConfig { ModelId = "grok-3-mini", DisplayName = "Grok 3 Mini" }
            }
        },
        new LlmProviderPreset
        {
            Name = "Google (Gemini)",
            Kind = LlmProviderKind.OpenAICompatible,
            BaseUrl = "https://generativelanguage.googleapis.com/v1beta/openai/",
            ApiKeyEnvVar = "GEMINI_API_KEY",
            Notes = "Uses Gemini's OpenAI-compatible endpoint.",
            Models = new()
            {
                new AIModelConfig { ModelId = "gemini-2.5-pro", DisplayName = "Gemini 2.5 Pro", IsDefault = true },
                new AIModelConfig { ModelId = "gemini-2.5-flash", DisplayName = "Gemini 2.5 Flash" }
            }
        },
        new LlmProviderPreset
        {
            Name = "Groq (Meta Llama)",
            Kind = LlmProviderKind.OpenAICompatible,
            BaseUrl = "https://api.groq.com/openai/v1",
            ApiKeyEnvVar = "GROQ_API_KEY",
            Models = new()
            {
                new AIModelConfig { ModelId = "llama-3.3-70b-versatile", DisplayName = "Llama 3.3 70B", IsDefault = true },
                new AIModelConfig { ModelId = "llama-3.1-8b-instant", DisplayName = "Llama 3.1 8B" }
            }
        },
        new LlmProviderPreset
        {
            Name = "Ollama (local)",
            Kind = LlmProviderKind.OpenAICompatible,
            BaseUrl = "http://localhost:11434/v1",
            ApiKeyEnvVar = "",
            Notes = "Local models; no API key needed.",
            Models = new()
            {
                new AIModelConfig { ModelId = "llama3.2", DisplayName = "Llama 3.2", IsDefault = true },
                new AIModelConfig { ModelId = "mistral", DisplayName = "Mistral" }
            }
        },
        new LlmProviderPreset
        {
            Name = "Custom (OpenAI-compatible)",
            Kind = LlmProviderKind.OpenAICompatible,
            BaseUrl = null,
            ApiKeyEnvVar = "",
            Notes = "Any endpoint speaking the OpenAI chat-completions format. Enter the base URL and model id."
        }
    };

    public static LlmProviderPreset? FindByName(string name) =>
        All.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
}
