using Extractor.Domain;

namespace Extractor.Application;

public interface IRunStore
{
    Task SaveAsync(ExtractionRun run, CancellationToken ct);
    Task<ExtractionRun?> GetAsync(string runId, CancellationToken ct);
}
