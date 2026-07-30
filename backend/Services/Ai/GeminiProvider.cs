using System.Net.Http.Json;
using System.Text.Json.Serialization;
using LearnPath.API.Configuration;
using LearnPath.API.Interfaces.Services.Ai;
using Microsoft.Extensions.Options;

namespace LearnPath.API.Services.Ai;

public class GeminiProvider : IAiProvider
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiProvider> _logger;

    public GeminiProvider(HttpClient httpClient, IOptions<AiOptions> options, ILogger<GeminiProvider> logger)
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
                ErrorMessage = "AI Feedback is unavailable. The Gemini API key is not configured.",
            };
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var geminiRequest = new GeminiRequest
        {
            Contents =
            [
                new GeminiContent
                {
                    Parts = [new GeminiPart { Text = request.Prompt }],
                },
            ],
        };

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_options.TimeoutSeconds));

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(url, geminiRequest, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cts.Token);
                _logger.LogWarning(
                    "Gemini API error: HTTP {(int)response.StatusCode} ({StatusCode}) for model {Model}. Body: {ErrorBody}",
                    (int)response.StatusCode, response.StatusCode, _options.Model, errorBody);
                var prefix = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.Unauthorized => "AI Feedback is unavailable. The Gemini API key is invalid.",
                    System.Net.HttpStatusCode.Forbidden => "AI Feedback is unavailable. Access to the Gemini API is denied.",
                    System.Net.HttpStatusCode.TooManyRequests => "AI Feedback is temporarily unavailable due to rate limiting.",
                    System.Net.HttpStatusCode.ServiceUnavailable => "AI Feedback is temporarily unavailable. The AI service is experiencing issues.",
                    _ => $"AI Feedback request failed ({(int)response.StatusCode}).",
                };
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = $"{prefix} Gemini response: {errorBody}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>(cts.Token);

            if (result?.Candidates is null || result.Candidates.Count == 0)
            {
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = "AI returned no results. The model may have been blocked or returned empty content.",
                };
            }

            var text = result.Candidates[0]?.Content?.Parts?[0]?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                var finishReason = result.Candidates[0]?.FinishReason ?? "unknown";
                return new AiProviderResponse
                {
                    Success = false,
                    ErrorMessage = finishReason switch
                    {
                        "SAFETY" => "AI generation was blocked by safety filters. The submission may contain content that cannot be processed.",
                        "RECITATION" => "AI generation was blocked due to content similarity concerns.",
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

internal class GeminiRequest
{
    [JsonPropertyName("contents")]
    public List<GeminiContent> Contents { get; set; } = [];
}

internal class GeminiContent
{
    [JsonPropertyName("parts")]
    public List<GeminiPart> Parts { get; set; } = [];
}

internal class GeminiPart
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

internal class GeminiResponse
{
    [JsonPropertyName("candidates")]
    public List<GeminiCandidate>? Candidates { get; set; }
}

internal class GeminiCandidate
{
    [JsonPropertyName("content")]
    public GeminiContent? Content { get; set; }

    [JsonPropertyName("finishReason")]
    public string? FinishReason { get; set; }
}
