using System.Text;
using Extractor.Domain;

namespace Extractor.Application;

public sealed class ExtractionService : IExtractionService
{
    private readonly IUrlPolicy _urlPolicy;
    private readonly IHtmlFetcher _fetcher;
    private readonly ICharacterPageParser _parser;
    private readonly ILlmJsonClient _llm;
    private readonly IRunStore _runs;
    private readonly ILogger<ExtractionService> _log;

    public ExtractionService(
        IUrlPolicy urlPolicy,
        IHtmlFetcher fetcher,
        ICharacterPageParser parser,
        ILlmJsonClient llm,
        IRunStore runs,
        ILogger<ExtractionService> log)
    {
        _urlPolicy = urlPolicy;
        _fetcher = fetcher;
        _parser = parser;
        _llm = llm;
        _runs = runs;
        _log = log;
    }

    public async Task<ExtractionRun> ExtractFromUrlAsync(string url, CancellationToken ct)
    {
        var runId = Guid.NewGuid().ToString("N");

        try
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return await Fail(runId, url, "Invalid URL.", ct);

            _urlPolicy.EnsureAllowed(uri);

            var html = await _fetcher.FetchAsync(uri, ct);
            var parsed = _parser.Parse(html, url);

            var (systemPrompt, userPrompt) = BuildPrompts(parsed, url);

            var json = await _llm.ExtractJsonAsync(new LlmJsonRequest(
                SchemaName: "soccer_spirits_character",
                JsonSchema: JsonSchema.SoccerSpiritsCharacterSchema,
                SystemPrompt: systemPrompt,
                UserPrompt: userPrompt,
                MaxRetries: 2
            ), ct);

            var okRun = new ExtractionRun(runId, url, true, json, null, DateTimeOffset.UtcNow);
            await _runs.SaveAsync(okRun, ct);
            return okRun;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Extraction failed.");
            return await Fail(runId, url, ex.Message, ct);
        }
    }

    private static (string systemPrompt, string userPrompt) BuildPrompts(ParsedCharacterPage parsed, string sourceUrl)
    {
        var sb = new StringBuilder();

        sb.AppendLine("Known facts extracted deterministically (trust these over the wiki narrative):");
        sb.AppendLine($"Name: {parsed.Name}");

        foreach (var kv in parsed.Fields)
            sb.AppendLine($"{kv.Key}: {kv.Value}");

        sb.AppendLine("Stats:");
        foreach (var kv in parsed.Stats)
            sb.AppendLine($"{kv.Key}: {(kv.Value.HasValue ? kv.Value.Value.ToString() : "null")}");

        sb.AppendLine("Skills section text (extract skill/passive names and descriptions from this):");
        sb.AppendLine(parsed.SkillsText);

        var systemPrompt = """
You are a strict data extraction engine.
Rules:
- Output ONLY valid JSON matching the provided JSON Schema.
- Do NOT add keys not in the schema.
- Prefer the "Known facts" over any conflicting text.
- If a field is unknown, use null (or empty array for skills if absolutely nothing is present).
- Do not include markdown, code fences, or commentary.
""";

        var userPrompt = $"""
Extract the character data as JSON.

sourceUrl: {sourceUrl}

{sb}
""";

        return (systemPrompt, userPrompt);
    }

    private async Task<ExtractionRun> Fail(string runId, string url, string error, CancellationToken ct)
    {
        var run = new ExtractionRun(runId, url, false, "{}", error, DateTimeOffset.UtcNow);
        await _runs.SaveAsync(run, ct);
        return run;
    }
}
