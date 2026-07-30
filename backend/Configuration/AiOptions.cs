namespace LearnPath.API.Configuration;

public class AiOptions
{
    public string Provider { get; set; } = "Nvidia";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://integrate.api.nvidia.com/v1";
    public string Model { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 60;
}
