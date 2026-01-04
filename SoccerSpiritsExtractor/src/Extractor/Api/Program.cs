using System.Threading.RateLimiting;
using Extractor.Api;
using Extractor.Application;
using Extractor.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Options
builder.Services.Configure<LlmOptions>(builder.Configuration.GetSection("Llm"));
builder.Services.Configure<UrlPolicyOptions>(builder.Configuration.GetSection("UrlPolicy"));

// Rate limit simples por IP
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opt.AddPolicy("fixed", ctx =>
    {
        var key = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });
});

// Application
builder.Services.AddSingleton<IExtractionService, ExtractionService>();

// Infrastructure
builder.Services.AddSingleton<IUrlPolicy, UrlPolicy>();
builder.Services.AddSingleton<ICharacterPageParser, CharacterPageParser>();
builder.Services.AddSingleton<IRunStore, InMemoryRunStore>();

builder.Services.AddHttpClient<IHtmlFetcher, HtmlFetcher>();
builder.Services.AddHttpClient<ILlmJsonClient, LmStudioJsonClient>()
    .ConfigureHttpClient((sp, http) =>
    {
        var opt = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<LlmOptions>>().Value;
        http.Timeout = TimeSpan.FromSeconds(opt.TimeoutSeconds);
    });

var app = builder.Build();

app.UseMiddleware<RequestIdMiddleware>();
app.UseRateLimiter();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers().RequireRateLimiting("fixed");

app.Run();

public partial class Program { }
