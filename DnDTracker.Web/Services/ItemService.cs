using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public record ItemInput(
    string Name,
    string Description,
    string WhereFound,
    string WhenFound,
    string CurrentStatus,
    string Notes);

public class ItemService(DnDTrackerDbContext db, ItemImageService itemImageService)
{
    public async Task<List<Item>> GetUnassignedAsync(string userId, Guid campaignId)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return [];
        }

        return await db.Items
            .AsNoTracking()
            .Where(i => i.CampaignId == campaignId && i.CharacterId == null)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<List<Item>> GetByCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId)
    {
        if (!await OwnsCharacterAsync(userId, campaignId, characterId))
        {
            return [];
        }

        return await db.Items
            .AsNoTracking()
            .Where(i => i.CharacterId == characterId)
            .OrderBy(i => i.Name)
            .ToListAsync();
    }

    public async Task<(Item? Item, string? Error)> CreateUnassignedAsync(
        string userId,
        Guid campaignId,
        ItemInput input)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return (null, "Campaign not found.");
        }

        var validationError = ValidateInput(input);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        var trimmedName = input.Name.Trim();
        if (await UnassignedNameExistsAsync(campaignId, trimmedName))
        {
            return (null, "An unassigned item with that name already exists.");
        }

        var item = CreateItemEntity(campaignId, null, input);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (item, null);
    }

    public async Task<(Item? Item, string? Error)> CreateForCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        ItemInput input)
    {
        if (!await OwnsCharacterAsync(userId, campaignId, characterId))
        {
            return (null, "Character not found.");
        }

        var validationError = ValidateInput(input);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        var trimmedName = input.Name.Trim();
        if (await CharacterItemNameExistsAsync(characterId, trimmedName))
        {
            return (null, "This character already has an item with that name.");
        }

        var item = CreateItemEntity(campaignId, characterId, input);
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return (item, null);
    }

    public async Task<string?> UpdateAsync(
        string userId,
        Guid campaignId,
        Guid itemId,
        ItemInput input)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return "Item not found.";
        }

        var validationError = ValidateInput(input);
        if (validationError is not null)
        {
            return validationError;
        }

        var trimmedName = input.Name.Trim();
        if (item.CharacterId.HasValue)
        {
            if (await CharacterItemNameExistsAsync(item.CharacterId.Value, trimmedName, itemId))
            {
                return "This character already has an item with that name.";
            }
        }
        else if (await UnassignedNameExistsAsync(campaignId, trimmedName, itemId))
        {
            return "An unassigned item with that name already exists.";
        }

        ApplyInput(item, input);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> DeleteAsync(string userId, Guid campaignId, Guid itemId)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return false;
        }

        await itemImageService.DeleteFilesForItemAsync(userId, itemId);
        db.Items.Remove(item);
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<string?> AssignToCharacterAsync(
        string userId,
        Guid campaignId,
        Guid itemId,
        Guid characterId)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return "Item not found.";
        }

        if (item.CharacterId.HasValue)
        {
            return "This item is already assigned to a character.";
        }

        var character = await db.Characters
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c =>
                c.Id == characterId &&
                c.CampaignId == campaignId &&
                c.Campaign.UserId == userId);

        if (character is null)
        {
            return "Character not found.";
        }

        if (await CharacterItemNameExistsAsync(characterId, item.Name))
        {
            return "This character already has an item with that name.";
        }

        item.CharacterId = characterId;
        item.CurrentStatus = $"Carried by {character.Name}";
        item.Notes = $"Item assigned to {character.Name}.";
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UnassignAsync(string userId, Guid campaignId, Guid itemId)
    {
        var item = await GetOwnedItemAsync(userId, campaignId, itemId);
        if (item is null)
        {
            return "Item not found.";
        }

        if (!item.CharacterId.HasValue)
        {
            return "This item is already unassigned.";
        }

        if (await UnassignedNameExistsAsync(campaignId, item.Name))
        {
            return "An unassigned item with that name already exists.";
        }

        item.CharacterId = null;
        item.CurrentStatus = "Unassigned";
        item.Notes = "Item moved to unassigned pool.";
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<(Item? Item, string? Error)> CopyAsync(
        string userId,
        Guid campaignId,
        Guid sourceItemId,
        Guid? targetCharacterId)
    {
        var sourceItem = await GetOwnedItemAsync(userId, campaignId, sourceItemId);
        if (sourceItem is null)
        {
            return (null, "Item not found.");
        }

        if (targetCharacterId.HasValue &&
            !await OwnsCharacterAsync(userId, campaignId, targetCharacterId.Value))
        {
            return (null, "Character not found.");
        }

        var copyName = await GetAvailableCopyNameAsync(
            campaignId,
            targetCharacterId,
            sourceItem.Name);

        var copy = new Item
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            CharacterId = targetCharacterId,
            Name = copyName,
            Description = sourceItem.Description,
            WhereFound = sourceItem.WhereFound,
            WhenFound = sourceItem.WhenFound,
            CurrentStatus = sourceItem.CurrentStatus,
            Notes = sourceItem.Notes
        };

        var copiedImagePath = await itemImageService.CopyImageForItemAsync(userId, sourceItemId, copy.Id);
        copy.ImagePath = copiedImagePath ?? "";

        db.Items.Add(copy);
        await db.SaveChangesAsync();
        return (copy, null);
    }

    private static string? ValidateInput(ItemInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return "Please enter an item name.";
        }

        return null;
    }

    private static Item CreateItemEntity(Guid campaignId, Guid? characterId, ItemInput input)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            CharacterId = characterId
        };

        ApplyInput(item, input);
        return item;
    }

    private static void ApplyInput(Item item, ItemInput input)
    {
        item.Name = input.Name.Trim();
        item.Description = input.Description.Trim();
        item.WhereFound = input.WhereFound.Trim();
        item.WhenFound = input.WhenFound.Trim();
        item.CurrentStatus = input.CurrentStatus.Trim();
        item.Notes = input.Notes.Trim();
    }

    private async Task<bool> OwnsCampaignAsync(string userId, Guid campaignId) =>
        await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.UserId == userId);

    private async Task<bool> OwnsCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId) =>
        await db.Characters.AnyAsync(c =>
            c.Id == characterId &&
            c.CampaignId == campaignId &&
            c.Campaign.UserId == userId);

    private async Task<Item?> GetOwnedItemAsync(string userId, Guid campaignId, Guid itemId) =>
        await db.Items
            .Include(i => i.Campaign)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.CampaignId == campaignId &&
                i.Campaign.UserId == userId);

    private async Task<bool> UnassignedNameExistsAsync(
        Guid campaignId,
        string name,
        Guid? excludeItemId = null)
    {
        var query = db.Items.Where(i => i.CampaignId == campaignId && i.CharacterId == null);
        if (excludeItemId.HasValue)
        {
            query = query.Where(i => i.Id != excludeItemId.Value);
        }

        var existingNames = await query.Select(i => i.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<bool> CharacterItemNameExistsAsync(
        Guid characterId,
        string name,
        Guid? excludeItemId = null)
    {
        var query = db.Items.Where(i => i.CharacterId == characterId);
        if (excludeItemId.HasValue)
        {
            query = query.Where(i => i.Id != excludeItemId.Value);
        }

        var existingNames = await query.Select(i => i.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string> GetAvailableCopyNameAsync(
        Guid campaignId,
        Guid? characterId,
        string sourceName)
    {
        var baseName = $"{sourceName.Trim()} (copy)";
        if (!await NameExistsInScopeAsync(campaignId, characterId, baseName))
        {
            return baseName;
        }

        for (var copyNumber = 2; copyNumber < 1000; copyNumber++)
        {
            var candidateName = $"{sourceName.Trim()} (copy {copyNumber})";
            if (!await NameExistsInScopeAsync(campaignId, characterId, candidateName))
            {
                return candidateName;
            }
        }

        return $"{sourceName.Trim()} (copy {Guid.NewGuid():N})";
    }

    private async Task<bool> NameExistsInScopeAsync(
        Guid campaignId,
        Guid? characterId,
        string name) =>
        characterId.HasValue
            ? await CharacterItemNameExistsAsync(characterId.Value, name)
            : await UnassignedNameExistsAsync(campaignId, name);
}
