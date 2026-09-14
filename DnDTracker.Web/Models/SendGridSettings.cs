namespace DnDTracker.Web.Models;

// Preserved for optional re-enable in Program.cs (SendGrid registration is currently commented out).

public class SendGridSettings
{
    public string ApiKey { get; set; } = "";

    public string FromEmail { get; set; } = "noreply@tracker.alanstirling.com";

    public string FromName { get; set; } = "DnD Tracker";
}
