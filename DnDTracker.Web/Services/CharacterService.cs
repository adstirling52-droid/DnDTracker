using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class CharacterService(DnDTrackerDbContext db)
{
    public async Task<List<Character>> GetByCampaignAsync(string userId, Guid campaignId)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return [];
        }

        return await db.Characters
            .AsNoTracking()
            .Where(c => c.CampaignId == campaignId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<(Character? Character, string? Error)> CreateAsync(
        string userId,
        Guid campaignId,
        string name)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return (null, "Campaign not found.");
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (null, "Please enter a character name.");
        }

        if (await NameExistsInCampaignAsync(campaignId, trimmedName))
        {
            return (null, "A character with that name already exists in this campaign.");
        }

        var character = new Character
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            Name = trimmedName
        };

        db.Characters.Add(character);
        await db.SaveChangesAsync();
        return (character, null);
    }

    public async Task<string?> UpdateAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        string name)
    {
        var character = await GetOwnedCharacterAsync(userId, campaignId, characterId);
        if (character is null)
        {
            return "Character not found.";
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return "Please enter a character name.";
        }

        if (await NameExistsInCampaignAsync(campaignId, trimmedName, characterId))
        {
            return "A character with that name already exists in this campaign.";
        }

        character.Name = trimmedName;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> DeleteAsync(string userId, Guid campaignId, Guid characterId)
    {
        var character = await GetOwnedCharacterAsync(userId, campaignId, characterId);
        if (character is null)
        {
            return false;
        }

        var items = await db.Items
            .Where(i => i.CharacterId == characterId)
            .ToListAsync();

        db.Items.RemoveRange(items);
        db.Characters.Remove(character);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> OwnsCampaignAsync(string userId, Guid campaignId) =>
        await db.Campaigns.AnyAsync(c => c.Id == campaignId && c.UserId == userId);

    private async Task<Character?> GetOwnedCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId) =>
        await db.Characters
            .Include(c => c.Campaign)
            .FirstOrDefaultAsync(c =>
                c.Id == characterId &&
                c.CampaignId == campaignId &&
                c.Campaign.UserId == userId);

    private async Task<bool> NameExistsInCampaignAsync(
        Guid campaignId,
        string name,
        Guid? excludeCharacterId = null)
    {
        var query = db.Characters.Where(c => c.CampaignId == campaignId);
        if (excludeCharacterId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCharacterId.Value);
        }

        var existingNames = await query.Select(c => c.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }
}
