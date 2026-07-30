using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LearnPath.API.Configuration;
using LearnPath.API.Interfaces.Services.Ai;
using Microsoft.Extensions.Options;

namespace LearnPath.API.Services.Ai;

public class NvidiaProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<NvidiaProvider> _logger;

    public NvidiaProvider(HttpClient httpClient, IOptions<AiOptions> options, ILogger<NvidiaProvider> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AiProviderResponse> SendAsync(AiProviderRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new AiProviderResponse
            {
                Success = false,
                ErrorMessage = "AI Feedback is unavailable. The NVIDIA API key is not configured.",
            };
        }

        var url = $"{_options.BaseUrl.TrimEnd('/')}/chat/completions";

        var nvidiaRequest = new NvidiaRequest
        {
            Model = _options.Model,
            Messages =
            [
                new NvidiaMessage
                {
                    Role = "user",
                    Content = request.Prompt,
                },
            ],
            Stream = false,
        };

        _httpClient.DefaultRequestHeaders.Remove("Authorization");
        _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_options.ApiKey}");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, nvidiaRequest, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning(
                    "NVIDIA API error: HTTP {(int)response.StatusCode} ({StatusCode}) for model {Model}. Body: {ErrorBody}",
                    (int)response.StatusCode, response.StatusCode, _options.Model, errorBody);
                var prefix = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "AI Feedback is unavailable. The NVIDIA API key is invalid.",
                    System.Net.HttpStatusCode.Forbidden => "AI Feedback is unavailable. Access to the NVIDIA API is denied.",
                    System.Net.HttpStatusCode.TooManyRequests => "AI Feedback is temporarily unavailable due to rate limiting.",
                    System.Net.HttpStatusCode.ServiceUnavailable => "AI Feedback is temporarily unavailable. The AI service is experiencing issues.",
                    _ => $"AI Feedback request failed ({(int)response.StatusCode}).",
                };
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"{prefix} NVIDIA response: {errorBody}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<NvidiaResponse>(cts.Token);

            if (result?.Choices is null || result.Choices.Count == 0)
            {
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = "AI returned no results. The model may have been blocked or returned empty content.",
                };
            }

            var text = result.Choices[0]?.Message?.Content;
            if (string.IsNullOrWhiteSpace(text))
            {
                var finishReason = result.Choices[0]?.FinishReason ?? "unknown";
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = finishReason switch
                    {
                        "content_filter" => "AI generation was blocked by content filters.",
                        "length" => "AI response was truncated due to length limits.",
                        _ => $"AI response was empty (finish reason: {finishReason}). Please try again.",
                    },
                };
            }

            return new AiProviderResponse { Success = true, Content = text };
        }
        catch (TaskCanceledException)
        {
            return new AiProviderResponse
            {
                Success = false,
                ErrorMessage = "AI Feedback request timed out. The submission may be too large or the service is busy.",
            };
        }
        catch (HttpRequestException ex)
        {
            return new AiProviderResponse
            {
                Success = false,
                ErrorMessage = $"Could not reach the AI service: {ex.Message}",
            };
        }
    }
}

internal class NvidiaRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<NvidiaMessage> Messages { get; set; } = [];

    [JsonPropertyName("stream")]
    public bool Stream { get; set; }
}

internal class NvidiaMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

internal class NvidiaResponse
{
    [JsonPropertyName("choices")]
    public List<NvidiaChoice>? Choices { get; set; }
}

internal class NvidiaChoice
{
    [JsonPropertyName("message")]
    public NvidiaMessage? Message { get; set; }

    [JsonPropertyName("finish_reason")]
    public string? FinishReason { get; set; }
}
