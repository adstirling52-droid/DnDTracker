using System.Threading.RateLimiting;
using Microsoft.Extensions.Options;

namespace DnDTracker.Web.Services;

public sealed class RegistrationRateLimitService : IDisposable
{
    private readonly RegistrationRateLimitOptions _options;
    private readonly PartitionedRateLimiter<string> _limiter;

    public RegistrationRateLimitService(IOptions<RegistrationRateLimitOptions> options)
    {
        _options = options.Value;
        _limiter = PartitionedRateLimiter.Create<string, string>(clientKey =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientKey,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = Math.Max(1, _options.PermitLimit),
                    Window = TimeSpan.FromMinutes(Math.Max(1, _options.WindowMinutes)),
                    QueueLimit = 0
                }));
    }

    public async ValueTask<(bool Allowed, TimeSpan? RetryAfter)> TryAcquireAsync(
        string clientKey,
        CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return (true, null);
        }

        using var lease = await _limiter.AcquireAsync(clientKey, 1, cancellationToken);
        if (lease.IsAcquired)
        {
            return (true, null);
        }

        TimeSpan? retryAfter = null;
        if (lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue))
        {
            retryAfter = retryAfterValue;
        }

        return (false, retryAfter);
    }

    public static string GetClientKey(HttpContext? httpContext) =>
        httpContext?.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    public static string FormatRetryAfter(TimeSpan retryAfter)
    {
        if (retryAfter.TotalMinutes >= 1)
        {
            var minutes = (int)Math.Ceiling(retryAfter.TotalMinutes);
            return minutes == 1 ? "1 minute" : $"{minutes} minutes";
        }

        var seconds = (int)Math.Ceiling(retryAfter.TotalSeconds);
        return seconds == 1 ? "1 second" : $"{seconds} seconds";
    }

    public void Dispose() => _limiter.Dispose();
}
