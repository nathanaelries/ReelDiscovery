namespace ReelDiscovery.Services;

/// <summary>
/// Provider-agnostic text generation. Any chat-capable LLM backend
/// (OpenAI, Anthropic Claude, xAI, Gemini, local models via Ollama, ...)
/// implements this; the generators only ever talk to this interface.
/// </summary>
public interface ILlmProvider
{
    string ModelId { get; }

    Task<bool> TestConnectionAsync(CancellationToken ct = default);

    Task<string> GetCompletionAsync(
        string systemPrompt,
        string userPrompt,
        string? operationName = null,
        CancellationToken ct = default);

    Task<T?> GetJsonCompletionAsync<T>(
        string systemPrompt,
        string userPrompt,
        string? operationName = null,
        CancellationToken ct = default) where T : class;
}

/// <summary>
/// Image generation capability (currently OpenAI DALL-E only).
/// Separate from ILlmProvider so a text provider without image support
/// can be paired with a different media provider, or none.
/// </summary>
public interface IImageGenerationProvider
{
    Task<byte[]?> GenerateImageAsync(
        string prompt,
        string? operationName = null,
        CancellationToken ct = default);
}

/// <summary>
/// Text-to-speech capability (currently OpenAI TTS only).
/// </summary>
public interface ISpeechGenerationProvider
{
    Task<byte[]?> GenerateSpeechAsync(
        string text,
        string voice = "alloy",
        string? operationName = null,
        CancellationToken ct = default);
}
