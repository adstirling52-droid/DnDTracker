namespace DnDTracker.Web.Models;

public class SiteSettings
{
    public string PublicSiteUrl { get; set; } = "https://www.alanstirling.com";

    public string TrackerUrl { get; set; } = "https://tracker.alanstirling.com";

    public string PasswordResetEmail { get; set; } = "passwordreset@alanstirling.com";
}
