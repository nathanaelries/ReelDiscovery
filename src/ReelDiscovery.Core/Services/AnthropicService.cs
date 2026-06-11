using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using ReelDiscovery.Models;

namespace ReelDiscovery.Services;

/// <summary>
/// Claude text generation via the official Anthropic C# SDK (Messages API).
/// Text-only: image and TTS generation stay with providers that support them.
/// </summary>
public class AnthropicService : ILlmProvider
{
    private readonly AnthropicClient _client;
    private readonly string _model;
    private readonly AIModelConfig? _modelConfig;
    private readonly TokenUsageTracker? _usageTracker;

    // Large enough for the 15-email JSON batches; non-streaming requests
    // above ~16K output risk HTTP timeouts.
    private const int MaxOutputTokens = 16000;
    private const int MaxJsonRetries = 3;

    public string ModelId => _model;

    public AnthropicService(string apiKey, string model)
    {
        // The SDK retries 429/5xx with exponential backoff internally.
        _client = new AnthropicClient { ApiKey = apiKey, MaxRetries = 4 };
        _model = model;
    }

    public AnthropicService(string apiKey, AIModelConfig modelConfig, TokenUsageTracker? usageTracker = null)
        : this(apiKey, modelConfig.ModelId)
    {
        _modelConfig = modelConfig;
        _usageTracker = usageTracker;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken ct = default)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            var message = await _client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = 16,
                Messages =
                [
                    new() { Role = Role.User, Content = "Say 'connected' if you can read this." }
                ]
            });
            return message.Content.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetCompletionAsync(
        string systemPrompt,
        string userPrompt,
        string? operationName = null,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var message = await _client.Messages.Create(new MessageCreateParams
        {
            Model = _model,
            MaxTokens = MaxOutputTokens,
            System = systemPrompt,
            Messages =
            [
                new() { Role = Role.User, Content = userPrompt }
            ]
        });

        TrackUsage(operationName ?? "Completion", message);

        var text = ExtractText(message);
        if (string.IsNullOrEmpty(text))
        {
            throw new InvalidOperationException("Empty response from Anthropic");
        }
        return text;
    }

    public async Task<T?> GetJsonCompletionAsync<T>(
        string systemPrompt,
        string userPrompt,
        string? operationName = null,
        CancellationToken ct = default) where T : class
    {
        // Claude has no json_object response format flag; instruct and parse,
        // stripping markdown fences defensively.
        var system = systemPrompt +
            "\n\nIMPORTANT: Respond ONLY with valid JSON. No markdown, no code fences, no explanations, just the JSON object.";

        for (int attempt = 0; attempt < MaxJsonRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var message = await _client.Messages.Create(new MessageCreateParams
            {
                Model = _model,
                MaxTokens = MaxOutputTokens,
                System = system,
                Messages =
                [
                    new() { Role = Role.User, Content = userPrompt }
                ]
            });

            TrackUsage(operationName ?? "JSON Completion", message);

            var text = ExtractText(message);
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            try
            {
                return JsonSerializer.Deserialize<T>(StripCodeFences(text), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException)
            {
                if (attempt == MaxJsonRetries - 1)
                    throw;
            }
        }

        throw new InvalidOperationException("Failed to get valid JSON response from Anthropic after multiple attempts.");
    }

    private static string? ExtractText(Message message)
    {
        // Content is a union of block types; pick the first text block.
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock) && !string.IsNullOrEmpty(textBlock.Text))
            {
                return textBlock.Text;
            }
        }
        return null;
    }

    private static string StripCodeFences(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewline = trimmed.IndexOf('\n');
            if (firstNewline >= 0)
            {
                trimmed = trimmed[(firstNewline + 1)..];
            }
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (lastFence >= 0)
            {
                trimmed = trimmed[..lastFence];
            }
        }
        return trimmed.Trim();
    }

    private void TrackUsage(string operation, Message message)
    {
        if (_usageTracker != null && _modelConfig != null && message.Usage != null)
        {
            _usageTracker.RecordUsage(
                operation,
                _modelConfig,
                (int)message.Usage.InputTokens,
                (int)message.Usage.OutputTokens);
        }
    }
}
