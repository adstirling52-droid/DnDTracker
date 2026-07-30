namespace DnDTracker.Web.Models;

public class SendGridSettings
{
    public string ApiKey { get; set; } = "";

    public string FromEmail { get; set; } = "noreply@tracker.alanstirling.com";

    public string FromName { get; set; } = "DnD Tracker";
}
