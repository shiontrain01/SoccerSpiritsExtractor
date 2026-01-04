namespace Extractor.Api;

public sealed class RequestIdMiddleware
{
    private readonly RequestDelegate _next;
    public const string HeaderName = "X-Request-Id";

    public RequestIdMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(HeaderName, out var v) && !string.IsNullOrWhiteSpace(v)
            ? v.ToString()
            : Guid.NewGuid().ToString("N");

        ctx.TraceIdentifier = id;
        ctx.Response.Headers[HeaderName] = id;

        using (ctx.RequestServices.GetRequiredService<ILogger<RequestIdMiddleware>>()
               .BeginScope(new Dictionary<string, object> { ["request_id"] = id }))
        {
            await _next(ctx);
        }
    }
}
