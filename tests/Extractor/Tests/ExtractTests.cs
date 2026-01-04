using Extractor.Application;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http.Json;

public class ExtractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ExtractTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ILlmJsonClient>();
                services.AddSingleton<ILlmJsonClient, FakeLlmJsonClient>();
            });
        });
    }

    [Fact]
    public async Task FromUrl_Returns_Response()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync("/extract/from-url", new
        {
            url = "https://soccerspirits.fandom.com/wiki/William"
        });

        Assert.NotNull(resp);
    }

    private sealed class FakeLlmJsonClient : ILlmJsonClient
    {
        public Task<string> ExtractJsonAsync(LlmJsonRequest req, CancellationToken ct)
            => Task.FromResult("{}");
    }
}
