namespace DnDTracker.Web.Services;

public sealed class RegistrationRateLimitOptions
{
    public const string SectionName = "RateLimiting:Registration";

    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 5;

    public int WindowMinutes { get; set; } = 60;
}
