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
        // Single-tenant convenience: pre-fill the key from the environment if present.
        var envKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(envKey))
        {
            State.ApiKey = envKey;
        }
    }

    public bool HasApiKey => !string.IsNullOrWhiteSpace(State.ApiKey);

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        var ok = await State.CreateOpenAIService().TestConnectionAsync(ct);
        State.ConnectionTested = ok;
        return ok;
    }

    public async Task GenerateStorylinesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var generator = new StorylineGenerator(State.CreateOpenAIService());
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
        var generator = new CharacterGenerator(State.CreateOpenAIService());
        var result = await generator.GenerateCharactersAsync(State.Topic, State.Storylines, progress, ct);

        State.Characters = result.Characters;
        State.CompanyDomain = result.CompanyDomain;
    }

    public async Task GenerateThemesAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var generator = new ThemeGenerator(State.CreateOpenAIService());
        State.DomainThemes = await generator.GenerateThemesForDomainsAsync(
            State.Topic, State.Characters, progress, ct);
    }
}
