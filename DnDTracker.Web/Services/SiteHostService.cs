namespace DnDTracker.Web.Services;

public class SiteHostService(IHttpContextAccessor httpContextAccessor)
{
    public bool IsTrackerHost
    {
        get
        {
            var host = httpContextAccessor.HttpContext?.Request.Host.Host;
            return host is not null
                && host.StartsWith("tracker.", StringComparison.OrdinalIgnoreCase);
        }
    }

    public string AppHomePath => IsTrackerHost ? "/" : "/dnd";
}
