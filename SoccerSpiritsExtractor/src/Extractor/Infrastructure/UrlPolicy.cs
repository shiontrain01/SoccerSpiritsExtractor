using Extractor.Application;
using Microsoft.Extensions.Options;

namespace Extractor.Infrastructure;

public sealed class UrlPolicy : IUrlPolicy
{
    private readonly HashSet<string> _allowedHosts;

    public UrlPolicy(IOptions<UrlPolicyOptions> opt)
    {
        _allowedHosts = opt.Value.AllowedHosts
            .Select(h => h.Trim().ToLowerInvariant())
            .ToHashSet();
    }

    public void EnsureAllowed(Uri url)
    {
        var host = url.Host.ToLowerInvariant();
        if (!_allowedHosts.Contains(host))
            throw new InvalidOperationException($"Host not allowed: {url.Host}");

        if (url.Scheme != Uri.UriSchemeHttps && url.Scheme != Uri.UriSchemeHttp)
            throw new InvalidOperationException("Only http/https allowed.");
    }
}
