namespace ReelDiscovery.Models;

/// <summary>
/// Serializable definition of a dataset: everything needed to reproduce a generation run
/// except the OpenAI API key (secrets are never written to dataset files).
/// This is the schema behind dataset.yaml.
/// </summary>
public class DatasetDefinition
{
    public int Version { get; set; } = 1;
    public string Provider { get; set; } = "OpenAI";
    public string? BaseUrl { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
    public string Topic { get; set; } = string.Empty;
    public string AdditionalInstructions { get; set; } = string.Empty;
    public int StorylineCount { get; set; } = 10;
    public DatasetMediaHints Media { get; set; } = new();
    public string CompanyDomain { get; set; } = string.Empty;
    public DatasetDateRange DateRange { get; set; } = new();
    public DatasetGenerationSettings Generation { get; set; } = new();
    public List<DatasetStoryline> Storylines { get; set; } = new();
    public List<DatasetCharacter> Characters { get; set; } = new();
}

public class DatasetMediaHints
{
    public bool Documents { get; set; } = true;
    public bool Images { get; set; }
    public bool Voicemails { get; set; }
}

public class DatasetDateRange
{
    public bool LetAiSuggestDates { get; set; } = true;
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public DateTime? AiSuggestedStart { get; set; }
    public DateTime? AiSuggestedEnd { get; set; }
    public string? AiSuggestedReasoning { get; set; }
}

public class DatasetGenerationSettings
{
    public int EmailCount { get; set; } = 50;
    public int AttachmentPercentage { get; set; } = 20;
    public string AttachmentComplexity { get; set; } = nameof(Models.AttachmentComplexity.Detailed);
    public bool IncludeWord { get; set; } = true;
    public bool IncludeExcel { get; set; } = true;
    public bool IncludePowerPoint { get; set; } = true;
    public int ParallelThreads { get; set; } = 3;
    public bool OrganizeBySender { get; set; } = true;
    public bool IncludeImages { get; set; }
    public int ImagePercentage { get; set; } = 10;
    public bool IncludeCalendarInvites { get; set; } = true;
    public int CalendarInvitePercentage { get; set; } = 10;
    public bool EnableAttachmentChains { get; set; } = true;
    public bool IncludeVoicemails { get; set; }
    public int VoicemailPercentage { get; set; } = 5;
}

public class DatasetStoryline
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string TimelineHint { get; set; } = string.Empty;
    public int SuggestedEmailCount { get; set; }
    public List<Guid> InvolvedCharacterIds { get; set; } = new();
}

public class DatasetCharacter
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string PersonalityNotes { get; set; } = string.Empty;
    public string SignatureBlock { get; set; } = string.Empty;
    public bool IsExternal { get; set; }
    public string VoiceId { get; set; } = "alloy";
}
