using DnDTracker.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Xunit;

namespace DnDTracker.Web.Tests.Services;

public class RegistrationRateLimitServiceTests
{
    [Fact]
    public async Task TryAcquireAsync_WhenDisabled_AlwaysAllows()
    {
        using var service = CreateService(enabled: false, permitLimit: 1);

        var (firstAllowed, _) = await service.TryAcquireAsync("127.0.0.1");
        var (secondAllowed, _) = await service.TryAcquireAsync("127.0.0.1");

        Assert.True(firstAllowed);
        Assert.True(secondAllowed);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenEnabled_EnforcesPermitLimitPerClientKey()
    {
        using var service = CreateService(enabled: true, permitLimit: 2, windowMinutes: 60);

        var (firstAllowed, _) = await service.TryAcquireAsync("203.0.113.10");
        var (secondAllowed, _) = await service.TryAcquireAsync("203.0.113.10");
        var (thirdAllowed, retryAfter) = await service.TryAcquireAsync("203.0.113.10");

        Assert.True(firstAllowed);
        Assert.True(secondAllowed);
        Assert.False(thirdAllowed);
        Assert.NotNull(retryAfter);
        Assert.True(retryAfter.Value > TimeSpan.Zero);
    }

    [Fact]
    public async Task TryAcquireAsync_WhenEnabled_TracksClientKeysIndependently()
    {
        using var service = CreateService(enabled: true, permitLimit: 1, windowMinutes: 60);

        var (firstClientAllowed, _) = await service.TryAcquireAsync("203.0.113.10");
        var (secondClientAllowed, _) = await service.TryAcquireAsync("203.0.113.11");
        var (firstClientBlocked, _) = await service.TryAcquireAsync("203.0.113.10");

        Assert.True(firstClientAllowed);
        Assert.True(secondClientAllowed);
        Assert.False(firstClientBlocked);
    }

    [Theory]
    [InlineData(60, "1 minute")]
    [InlineData(90, "2 minutes")]
    [InlineData(30, "30 seconds")]
    [InlineData(1, "1 second")]
    [InlineData(2, "2 seconds")]
    public void FormatRetryAfter_UsesReadableUnits(int totalSeconds, string expected)
    {
        var formatted = RegistrationRateLimitService.FormatRetryAfter(TimeSpan.FromSeconds(totalSeconds));

        Assert.Equal(expected, formatted);
    }

    [Fact]
    public void GetClientKey_UsesRemoteIpAddress()
    {
        var context = new DefaultHttpContext
        {
            Connection =
            {
                RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.4")
            }
        };

        var clientKey = RegistrationRateLimitService.GetClientKey(context);

        Assert.Equal("198.51.100.4", clientKey);
    }

    [Fact]
    public void GetClientKey_WhenContextMissing_ReturnsUnknown()
    {
        Assert.Equal("unknown", RegistrationRateLimitService.GetClientKey(null));
    }

    private static RegistrationRateLimitService CreateService(
        bool enabled,
        int permitLimit = 5,
        int windowMinutes = 60)
    {
        var options = Options.Create(new RegistrationRateLimitOptions
        {
            Enabled = enabled,
            PermitLimit = permitLimit,
            WindowMinutes = windowMinutes
        });

        return new RegistrationRateLimitService(options);
    }
}
