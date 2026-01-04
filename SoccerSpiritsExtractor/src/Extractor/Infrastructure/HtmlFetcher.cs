using Extractor.Application;
using Microsoft.Extensions.Options;

namespace Extractor.Infrastructure;

public sealed class HtmlFetcher : IHtmlFetcher
{
    private readonly HttpClient _http;
    private readonly LlmOptions _opt;

    public HtmlFetcher(HttpClient http, IOptions<LlmOptions> opt)
    {
        _http = http;
        _opt = opt.Value;
    }

    public async Task<string> FetchAsync(Uri url, CancellationToken ct)
    {
        using var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var html = await resp.Content.ReadAsStringAsync(ct);
        if (html.Length > _opt.MaxInputChars)
            html = html[.._opt.MaxInputChars]; // corta para segurança

        return html;
    }
}
