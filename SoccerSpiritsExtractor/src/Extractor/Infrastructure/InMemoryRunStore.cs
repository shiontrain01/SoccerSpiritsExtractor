using System.Collections.Concurrent;
using Extractor.Application;
using Extractor.Domain;

namespace Extractor.Infrastructure;

public sealed class InMemoryRunStore : IRunStore
{
    private readonly ConcurrentDictionary<string, ExtractionRun> _db = new();

    public Task SaveAsync(ExtractionRun run, CancellationToken ct)
    {
        _db[run.RunId] = run;
        return Task.CompletedTask;
    }

    public Task<ExtractionRun?> GetAsync(string runId, CancellationToken ct)
    {
        _db.TryGetValue(runId, out var run);
        return Task.FromResult(run);
    }
}
