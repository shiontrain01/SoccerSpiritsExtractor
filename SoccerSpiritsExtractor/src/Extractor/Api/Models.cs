namespace Extractor.Api;

public sealed class ExtractFromUrlRequest
{
    public string Url { get; set; } = "";
}

public sealed class ExtractResponse
{
    public string RunId { get; set; } = "";
    public bool Success { get; set; }
    public string OutputJson { get; set; } = "{}";
    public string? Error { get; set; }
}
