using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Extractor.Application;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

namespace Extractor.Infrastructure;

public sealed class LmStudioJsonClient : ILlmJsonClient
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opt;

    private static readonly IAsyncPolicy<HttpResponseMessage> RetryPolicy =
        HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(r => (int)r.StatusCode == 429)
            .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(250 * i));

    public LmStudioJsonClient(HttpClient http, IOptions<LlmOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    public async Task<string> ExtractJsonAsync(LlmJsonRequest req, CancellationToken ct)
    {
        var endpoint = $"{_opt.BaseUrl.TrimEnd('/')}/chat/completions";

        for (int attempt = 0; attempt <= req.MaxRetries; attempt++)
        {
            var payload = BuildPayload(req);

            using var msg = new HttpRequestMessage(HttpMethod.Post, endpoint);

            // LM Studio geralmente ignora, mas enviamos pra manter compatibilidade
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

            msg.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var resp = await RetryPolicy.ExecuteAsync(async () =>
                await _http.SendAsync(msg, HttpCompletionOption.ResponseContentRead, ct));

            var raw = await resp.Content.ReadAsStringAsync(ct);
            resp.EnsureSuccessStatusCode();

            var content = ExtractAssistantContent(raw);
            if (TryParseJsonObject(content, out var normalized))
                return normalized;

            // Se veio com lixo, pede correção
            req = req with
            {
                UserPrompt = $"""
Your previous output was not a valid JSON object matching the schema.
Output ONLY valid JSON.

Previous output:
{content}

Original request:
{req.UserPrompt}
"""
            };
        }

        return "{}";
    }

    private object BuildPayload(LlmJsonRequest req)
    {
        // response_format json_schema (Structured Output)
        return new
        {
            model = _opt.Model,
            temperature = 0,
            messages = new object[]
            {
                new { role = "system", content = req.SystemPrompt },
                new { role = "user", content = req.UserPrompt }
            },
            response_format = new
            {
                type = "json_schema",
                json_schema = new
                {
                    name = req.SchemaName,
                    schema = JsonDocument.Parse(req.JsonSchema).RootElement
                }
            }
        };
    }

    private static string ExtractAssistantContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        return root.GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }

    private static bool TryParseJsonObject(string s, out string normalized)
    {
        normalized = "{}";
        s = s.Trim();

        // remove ```json fences se houver
        if (s.StartsWith("```"))
        {
            var end = s.LastIndexOf("```", StringComparison.Ordinal);
            if (end > 0) s = s.Substring(3, end - 3).Trim();
            if (s.StartsWith("json", StringComparison.OrdinalIgnoreCase)) s = s.Substring(4).Trim();
        }

        try
        {
            using var doc = JsonDocument.Parse(s);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;
            normalized = doc.RootElement.GetRawText();
            return true;
        }
        catch
        {
            return false;
        }
    }
}
