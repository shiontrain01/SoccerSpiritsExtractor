using Extractor.Domain;

namespace Extractor.Application;

public interface IHtmlFetcher
{
    Task<string> FetchAsync(Uri url, CancellationToken ct);
}

public interface IUrlPolicy
{
    void EnsureAllowed(Uri url);
}

public interface ICharacterPageParser
{
    ParsedCharacterPage Parse(string html, string sourceUrl);
}

public sealed record ParsedCharacterPage(
    string Name,
    Dictionary<string, string?> Fields,  // Attribute, Rarity, Type, Team etc
    Dictionary<string, decimal?> Stats,  // vários stats
    string SkillsText                  // trecho textual “limpo” para o LLM
);

public interface ILlmJsonClient
{
    Task<string> ExtractJsonAsync(LlmJsonRequest req, CancellationToken ct);
}

public sealed record LlmJsonRequest(
    string SchemaName,
    string JsonSchema,
    string SystemPrompt,
    string UserPrompt,
    int MaxRetries
);

public interface IExtractionService
{
    Task<ExtractionRun> ExtractFromUrlAsync(string url, CancellationToken ct);
}
