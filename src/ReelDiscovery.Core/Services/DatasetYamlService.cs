using ReelDiscovery.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ReelDiscovery.Services;

/// <summary>
/// Converts between WizardState and the dataset.yaml format (DatasetDefinition).
/// The YAML file captures everything needed to reproduce a dataset except the API key.
/// </summary>
public class DatasetYamlService
{
    private readonly ISerializer _serializer = new SerializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
        .Build();

    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(CamelCaseNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    public string Serialize(WizardState state) => _serializer.Serialize(FromState(state));

    public DatasetDefinition Parse(string yaml) =>
        _deserializer.Deserialize<DatasetDefinition>(yaml)
            ?? throw new InvalidOperationException("YAML did not contain a dataset definition.");

    /// <summary>
    /// Parses YAML and applies it onto an existing WizardState (API key and
    /// runtime-only state such as generated threads are left untouched).
    /// </summary>
    public void ApplyYaml(string yaml, WizardState state) => ApplyTo(Parse(yaml), state);

    public DatasetDefinition FromState(WizardState state)
    {
        return new DatasetDefinition
        {
            Model = state.SelectedModel,
            Topic = state.Topic,
            AdditionalInstructions = state.AdditionalInstructions,
            StorylineCount = state.StorylineCount,
            Media = new DatasetMediaHints
            {
                Documents = state.WantsDocuments,
                Images = state.WantsImages,
                Voicemails = state.WantsVoicemails
            },
            CompanyDomain = state.CompanyDomain,
            DateRange = new DatasetDateRange
            {
                LetAiSuggestDates = state.Config.LetAISuggestDates,
                Start = state.Config.StartDate,
                End = state.Config.EndDate,
                AiSuggestedStart = state.AISuggestedStartDate,
                AiSuggestedEnd = state.AISuggestedEndDate,
                AiSuggestedReasoning = state.AISuggestedDateReasoning
            },
            Generation = new DatasetGenerationSettings
            {
                EmailCount = state.Config.TotalEmailCount,
                AttachmentPercentage = state.Config.AttachmentPercentage,
                AttachmentComplexity = state.Config.AttachmentComplexity.ToString(),
                IncludeWord = state.Config.IncludeWord,
                IncludeExcel = state.Config.IncludeExcel,
                IncludePowerPoint = state.Config.IncludePowerPoint,
                ParallelThreads = state.Config.ParallelThreads,
                OrganizeBySender = state.Config.OrganizeBySender,
                IncludeImages = state.Config.IncludeImages,
                ImagePercentage = state.Config.ImagePercentage,
                IncludeCalendarInvites = state.Config.IncludeCalendarInvites,
                CalendarInvitePercentage = state.Config.CalendarInvitePercentage,
                EnableAttachmentChains = state.Config.EnableAttachmentChains,
                IncludeVoicemails = state.Config.IncludeVoicemails,
                VoicemailPercentage = state.Config.VoicemailPercentage
            },
            Storylines = state.Storylines.Select(s => new DatasetStoryline
            {
                Id = s.Id,
                Title = s.Title,
                Description = s.Description,
                TimelineHint = s.TimelineHint,
                SuggestedEmailCount = s.SuggestedEmailCount,
                InvolvedCharacterIds = s.InvolvedCharacterIds.ToList()
            }).ToList(),
            Characters = state.Characters.Select(c => new DatasetCharacter
            {
                Id = c.Id,
                FirstName = c.FirstName,
                LastName = c.LastName,
                Email = c.Email,
                Role = c.Role,
                Department = c.Department,
                Organization = c.Organization,
                PersonalityNotes = c.PersonalityNotes,
                SignatureBlock = c.SignatureBlock,
                IsExternal = c.IsExternal,
                VoiceId = c.VoiceId
            }).ToList()
        };
    }

    public void ApplyTo(DatasetDefinition def, WizardState state)
    {
        state.SelectedModel = def.Model;
        state.Topic = def.Topic;
        state.AdditionalInstructions = def.AdditionalInstructions;
        state.StorylineCount = def.StorylineCount;
        state.WantsDocuments = def.Media.Documents;
        state.WantsImages = def.Media.Images;
        state.WantsVoicemails = def.Media.Voicemails;
        state.CompanyDomain = def.CompanyDomain;

        state.Config.LetAISuggestDates = def.DateRange.LetAiSuggestDates;
        if (def.DateRange.Start.HasValue) state.Config.StartDate = def.DateRange.Start.Value;
        if (def.DateRange.End.HasValue) state.Config.EndDate = def.DateRange.End.Value;
        state.AISuggestedStartDate = def.DateRange.AiSuggestedStart;
        state.AISuggestedEndDate = def.DateRange.AiSuggestedEnd;
        state.AISuggestedDateReasoning = def.DateRange.AiSuggestedReasoning;

        state.Config.TotalEmailCount = def.Generation.EmailCount;
        state.Config.AttachmentPercentage = def.Generation.AttachmentPercentage;
        if (Enum.TryParse<AttachmentComplexity>(def.Generation.AttachmentComplexity, true, out var complexity))
        {
            state.Config.AttachmentComplexity = complexity;
        }
        state.Config.IncludeWord = def.Generation.IncludeWord;
        state.Config.IncludeExcel = def.Generation.IncludeExcel;
        state.Config.IncludePowerPoint = def.Generation.IncludePowerPoint;
        state.Config.ParallelThreads = def.Generation.ParallelThreads;
        state.Config.OrganizeBySender = def.Generation.OrganizeBySender;
        state.Config.IncludeImages = def.Generation.IncludeImages;
        state.Config.ImagePercentage = def.Generation.ImagePercentage;
        state.Config.IncludeCalendarInvites = def.Generation.IncludeCalendarInvites;
        state.Config.CalendarInvitePercentage = def.Generation.CalendarInvitePercentage;
        state.Config.EnableAttachmentChains = def.Generation.EnableAttachmentChains;
        state.Config.IncludeVoicemails = def.Generation.IncludeVoicemails;
        state.Config.VoicemailPercentage = def.Generation.VoicemailPercentage;

        state.Storylines = def.Storylines.Select(s => new Storyline
        {
            Id = s.Id,
            Title = s.Title,
            Description = s.Description,
            TimelineHint = s.TimelineHint,
            SuggestedEmailCount = s.SuggestedEmailCount,
            InvolvedCharacterIds = s.InvolvedCharacterIds.ToList()
        }).ToList();

        state.Characters = def.Characters.Select(c => new Character
        {
            Id = c.Id,
            FirstName = c.FirstName,
            LastName = c.LastName,
            Email = c.Email,
            Role = c.Role,
            Department = c.Department,
            Organization = c.Organization,
            PersonalityNotes = c.PersonalityNotes,
            SignatureBlock = c.SignatureBlock,
            IsExternal = c.IsExternal,
            VoiceId = c.VoiceId
        }).ToList();
    }
}
