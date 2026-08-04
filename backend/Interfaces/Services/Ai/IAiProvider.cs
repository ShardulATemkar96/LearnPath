namespace LearnPath.API.Interfaces.Services.Ai;

public class AiProviderRequest
{
    public string Prompt { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; } = 30;
}

public class AiProviderResponse
{
    public bool Success { get; init; }
    public string Content { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
}

public interface IAiProvider
{
    Task<AiProviderResponse> SendAsync(AiProviderRequest request);
}
