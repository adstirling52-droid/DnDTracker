using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class CampaignNpcImageService(IWebHostEnvironment environment, DnDTrackerDbContext db)
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
        ".jpg",
        ".jpeg",
        ".bmp"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".bmp"] = "image/bmp"
    };

    private string ImagesRoot => Path.Combine(environment.ContentRootPath, "Data", "npc-images");

    public static string GetImageUrl(Guid npcId, long version = 0) =>
        version > 0 ? $"/api/campaign-npcs/{npcId}/image?v={version}" : $"/api/campaign-npcs/{npcId}/image";

    public async Task<(string? RelativePath, string? Error)> SaveForNpcAsync(
        string userId,
        Guid campaignId,
        Guid npcId,
        Stream fileStream,
        string originalFileName)
    {
        var npc = await GetOwnedNpcAsync(userId, campaignId, npcId);
        if (npc is null)
        {
            return (null, "Saved NPC not found.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!IsAllowedExtension(extension))
        {
            return (null, "Only PNG, JPG, JPEG, and BMP images are supported.");
        }

        var relativePath = BuildRelativePath(userId, npcId, extension);
        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        DeleteFilesForNpc(userId, npcId);

        await using (var output = File.Create(fullPath))
        {
            await fileStream.CopyToAsync(output);
        }

        npc.ImagePath = relativePath;
        await db.SaveChangesAsync();
        return (relativePath, null);
    }

    public async Task<string?> ClearForNpcAsync(string userId, Guid campaignId, Guid npcId)
    {
        var npc = await GetOwnedNpcAsync(userId, campaignId, npcId);
        if (npc is null)
        {
            return "Saved NPC not found.";
        }

        DeleteFilesForNpc(userId, npcId);
        npc.ImagePath = "";
        await db.SaveChangesAsync();
        return null;
    }

    public Task DeleteFilesForNpcAsync(string userId, Guid npcId)
    {
        DeleteFilesForNpc(userId, npcId);
        return Task.CompletedTask;
    }

    public async Task<(Stream? Stream, string? ContentType)> OpenImageAsync(string userId, Guid npcId)
    {
        var npc = await db.CampaignNpcs
            .AsNoTracking()
            .Include(n => n.Campaign)
            .FirstOrDefaultAsync(n => n.Id == npcId && n.Campaign.UserId == userId);

        if (npc is null || string.IsNullOrWhiteSpace(npc.ImagePath))
        {
            return (null, null);
        }

        var fullPath = GetFullPath(npc.ImagePath);
        if (!File.Exists(fullPath))
        {
            return (null, null);
        }

        var extension = Path.GetExtension(fullPath);
        ContentTypes.TryGetValue(extension, out var contentType);
        return (File.OpenRead(fullPath), contentType ?? "application/octet-stream");
    }

    public static bool IsAllowedExtension(string extension) =>
        AllowedExtensions.Contains(extension);

    private static string BuildRelativePath(string userId, Guid npcId, string extension) =>
        $"{userId}/{npcId}{extension.ToLowerInvariant()}";

    private string GetFullPath(string relativePath) =>
        Path.Combine(ImagesRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private void DeleteFilesForNpc(string userId, Guid npcId)
    {
        var userDirectory = Path.Combine(ImagesRoot, userId);
        if (!Directory.Exists(userDirectory))
        {
            return;
        }

        foreach (var extension in AllowedExtensions)
        {
            var candidate = Path.Combine(userDirectory, $"{npcId}{extension}");
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }

    private async Task<CampaignNpc?> GetOwnedNpcAsync(string userId, Guid campaignId, Guid npcId) =>
        await db.CampaignNpcs
            .Include(npc => npc.Campaign)
            .FirstOrDefaultAsync(npc =>
                npc.Id == npcId &&
                npc.CampaignId == campaignId &&
                npc.Campaign.UserId == userId);
}
