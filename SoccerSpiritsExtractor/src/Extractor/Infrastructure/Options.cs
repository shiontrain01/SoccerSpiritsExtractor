namespace Extractor.Infrastructure;

public sealed class LlmOptions
{
    public string BaseUrl { get; set; } = "http://localhost:1234/v1";
    public string ApiKey { get; set; } = "lm-studio";
    public string Model { get; set; } = "qwen2.5-7b-instruct";
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxInputChars { get; set; } = 120_000; // HTML é grande
}

public sealed class UrlPolicyOptions
{
    public string[] AllowedHosts { get; set; } = new[]
    {
        "soccerspirits.fandom.com",
        "static.wikia.nocookie.net"
    };
}
