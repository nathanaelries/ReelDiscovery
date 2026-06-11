using ReelDiscovery.Models;
using ReelDiscovery.Services;

namespace ReelDiscovery.Web.Services;

/// <summary>
/// Per-circuit (per-browser-session) wizard state and orchestration of the
/// core generators. Mirrors the WinForms wizard flow.
/// </summary>
public class WizardSession
{
    public WizardState State { get; } = new();

    public WizardSession()
    {
        // Single-tenant convenience: pre-fill keys from the environment if present.
        var preset = LlmProviderCatalog.FindByName(State.ProviderName);
        if (preset != null)
        {
            ApplyProviderPreset(preset);
        }
    }

    public bool HasApiKey =>
        !string.IsNullOrWhiteSpace(State.ApiKey)
        || State.ProviderName.StartsWith("Ollama", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Switches the session to a provider preset: kind, base URL, model list,
    /// default model, and an API key from the preset's environment variable if set.
    /// </summary>
    public void ApplyProviderPreset(LlmProviderPreset preset)
    {
        State.ProviderName = preset.Name;
        State.ProviderKind = preset.Kind;
        State.ProviderBaseUrl = preset.BaseUrl;
        State.AvailableModelConfigs = preset.Models.ToList();
        State.SelectedModel = preset.DefaultModel?.ModelId ?? string.Empty;
        State.ConnectionTested = false;

        State.ApiKey = string.Empty;
        if (!string.IsNullOrEmpty(preset.ApiKeyEnvVar))
        {
            var envKey = Environment.GetEnvironmentVariable(preset.ApiKeyEnvVar);
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                State.ApiKey = envKey;
            }
        }

        // Media (DALL-E/TTS) falls back to an OpenAI key from the environment
        if (preset.Kind != LlmProviderKind.OpenAI && string.IsNullOrWhiteSpace(State.MediaApiKey))
        {
            State.MediaApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? string.Empty;
        }
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var ok = await State.CreateTextProvider().TestConnectionAsync(ct);
        State.ConnectionTested = ok;
        return ok;
    }

    public async Task GenerateStorylinesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var generator = new StorylineGenerator(State.CreateTextProvider());
        var result = await generator.GenerateStorylinesAsync(
            State.Topic,
            State.AdditionalInstructions,
            State.StorylineCount,
            State.WantsDocuments,
            State.WantsImages,
            State.WantsVoicemails,
            progress,
            ct);

        State.Storylines = result.Storylines;
        State.AISuggestedStartDate = result.SuggestedStartDate;
        State.AISuggestedEndDate = result.SuggestedEndDate;
        State.AISuggestedDateReasoning = result.DateRangeReasoning;
    }

    public async Task GenerateCharactersAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var generator = new CharacterGenerator(State.CreateTextProvider());
        var result = await generator.GenerateCharactersAsync(State.Topic, State.Storylines, progress, ct);

        State.Characters = result.Characters;
        State.CompanyDomain = result.CompanyDomain;
    }

    public async Task GenerateThemesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var generator = new ThemeGenerator(State.CreateTextProvider());
        State.DomainThemes = await generator.GenerateThemesForDomainsAsync(
            State.Topic, State.Characters, progress, ct);
    }
}
