using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class ItemImageService(IWebHostEnvironment environment, DnDTrackerDbContext db)
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

    private string ImagesRoot => Path.Combine(environment.ContentRootPath, "Data", "item-images");

    public static string GetImageUrl(Guid itemId, long version = 0) =>
        version > 0 ? $"/api/items/{itemId}/image?v={version}" : $"/api/items/{itemId}/image";

    public async Task<(string? RelativePath, string? Error)> SaveForItemAsync(
        string userId,
        Guid campaignId,
        Guid itemId,
        Stream fileStream,
        string originalFileName)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return (null, "Item not found.");
        }

        var extension = Path.GetExtension(originalFileName);
        if (!IsAllowedExtension(extension))
        {
            return (null, "Only PNG, JPG, JPEG, and BMP images are supported.");
        }

        var relativePath = BuildRelativePath(userId, itemId, extension);
        var fullPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        DeleteFilesForItem(userId, itemId);

        await using (var output = File.Create(fullPath))
        {
            await fileStream.CopyToAsync(output);
        }

        item.ImagePath = relativePath;
        await db.SaveChangesAsync();
        return (relativePath, null);
    }

    public async Task<string?> ClearForItemAsync(string userId, Guid campaignId, Guid itemId)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return "Item not found.";
        }

        DeleteFilesForItem(userId, itemId);
        item.ImagePath = "";
        await db.SaveChangesAsync();
        return null;
    }

    public Task DeleteFilesForItemAsync(string userId, Guid itemId)
    {
        DeleteFilesForItem(userId, itemId);
        return Task.CompletedTask;
    }

    public async Task<string?> CopyImageForItemAsync(string userId, Guid sourceItemId, Guid targetItemId)
    {
        var sourceItem = await db.Items
            .AsNoTracking()
            .Include(i => i.Campaign)
            .FirstOrDefaultAsync(i => i.Id == sourceItemId && i.Campaign.UserId == userId);

        if (sourceItem is null || string.IsNullOrWhiteSpace(sourceItem.ImagePath))
        {
            return null;
        }

        var sourcePath = GetFullPath(sourceItem.ImagePath);
        if (!File.Exists(sourcePath))
        {
            return null;
        }

        var extension = Path.GetExtension(sourcePath);
        var relativePath = BuildRelativePath(userId, targetItemId, extension);
        var targetPath = GetFullPath(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        DeleteFilesForItem(userId, targetItemId);
        File.Copy(sourcePath, targetPath, overwrite: true);
        return relativePath;
    }

    public async Task<(Stream? Stream, string? ContentType)> OpenImageAsync(string userId, Guid itemId)
    {
        var item = await db.Items
            .AsNoTracking()
            .Include(i => i.Campaign)
            .FirstOrDefaultAsync(i => i.Id == itemId && i.Campaign.UserId == userId);

        if (item is null || string.IsNullOrWhiteSpace(item.ImagePath))
        {
            return (null, null);
        }

        var fullPath = GetFullPath(item.ImagePath);
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

    private static string BuildRelativePath(string userId, Guid itemId, string extension) =>
        $"{userId}/{itemId}{extension.ToLowerInvariant()}";

    private string GetFullPath(string relativePath) =>
        Path.Combine(ImagesRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

    private void DeleteFilesForItem(string userId, Guid itemId)
    {
        var userDirectory = Path.Combine(ImagesRoot, userId);
        if (!Directory.Exists(userDirectory))
        {
            return;
        }

        foreach (var extension in AllowedExtensions)
        {
            var candidate = Path.Combine(userDirectory, $"{itemId}{extension}");
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }

    private async Task<Item?> GetOwnedItemAsync(string userId, Guid campaignId, Guid itemId) =>
        await db.Items
            .Include(i => i.Campaign)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.CampaignId == campaignId &&
                i.Campaign.UserId == userId);
}
