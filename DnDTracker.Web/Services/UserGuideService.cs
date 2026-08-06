using Markdig;
using Markdig.Extensions.AutoIdentifiers;

namespace DnDTracker.Web.Services;

public class UserGuideService
{
    private static readonly string RelativePath = Path.Combine("Data", "UserGuide", "USER_GUIDE.md");

    private static readonly MarkdownPipeline MarkdownPipeline = new MarkdownPipelineBuilder()
        .UseAutoIdentifiers(AutoIdentifierOptions.GitHub)
        .Build();

    private readonly IWebHostEnvironment environment;
    private string? cachedHtml;

    public UserGuideService(IWebHostEnvironment environment)
    {
        this.environment = environment;
    }

    public async Task<(string? Html, string? Error)> GetHtmlAsync()
    {
        if (cachedHtml is not null)
        {
            return (cachedHtml, null);
        }

        var path = Path.Combine(environment.ContentRootPath, RelativePath);
        if (!File.Exists(path))
        {
            return (null, "The user guide file was not found on the server.");
        }

        var markdown = await File.ReadAllTextAsync(path);
        cachedHtml = Markdown.ToHtml(markdown, MarkdownPipeline);
        return (cachedHtml, null);
    }
}
