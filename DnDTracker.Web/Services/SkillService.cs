using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class SkillService(DnDTrackerDbContext db)
{
    public async Task<List<Skill>> GetByCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId)
    {
        if (!await OwnsCharacterAsync(userId, campaignId, characterId))
        {
            return [];
        }

        return await db.Skills
            .AsNoTracking()
            .Where(s => s.CharacterId == characterId)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<(Skill? Skill, string? Error)> CreateAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        string name,
        string description,
        string notes)
    {
        if (!await OwnsCharacterAsync(userId, campaignId, characterId))
        {
            return (null, "Character not found.");
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (null, "Please enter a skill name.");
        }

        if (await NameExistsForCharacterAsync(characterId, trimmedName))
        {
            return (null, "A skill with that name already exists for this character.");
        }

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            Name = trimmedName,
            Description = description.Trim(),
            Notes = notes.Trim()
        };

        db.Skills.Add(skill);
        await db.SaveChangesAsync();
        return (skill, null);
    }

    public async Task<string?> UpdateAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        Guid skillId,
        string name,
        string description,
        string notes)
    {
        var skill = await GetOwnedSkillAsync(userId, campaignId, characterId, skillId);
        if (skill is null)
        {
            return "Skill not found.";
        }

        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return "Please enter a skill name.";
        }

        if (await NameExistsForCharacterAsync(characterId, trimmedName, skillId))
        {
            return "A skill with that name already exists for this character.";
        }

        skill.Name = trimmedName;
        skill.Description = description.Trim();
        skill.Notes = notes.Trim();
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> DeleteAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        Guid skillId)
    {
        var skill = await GetOwnedSkillAsync(userId, campaignId, characterId, skillId);
        if (skill is null)
        {
            return false;
        }

        db.Skills.Remove(skill);
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<bool> OwnsCharacterAsync(
        string userId,
        Guid campaignId,
        Guid characterId) =>
        await db.Characters.AnyAsync(c =>
            c.Id == characterId &&
            c.CampaignId == campaignId &&
            c.Campaign.UserId == userId);

    private async Task<Skill?> GetOwnedSkillAsync(
        string userId,
        Guid campaignId,
        Guid characterId,
        Guid skillId) =>
        await db.Skills
            .Include(s => s.Character)
            .ThenInclude(c => c.Campaign)
            .FirstOrDefaultAsync(s =>
                s.Id == skillId &&
                s.CharacterId == characterId &&
                s.Character.CampaignId == campaignId &&
                s.Character.Campaign.UserId == userId);

    private async Task<bool> NameExistsForCharacterAsync(
        Guid characterId,
        string name,
        Guid? excludeSkillId = null)
    {
        var query = db.Skills.Where(s => s.CharacterId == characterId);
        if (excludeSkillId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSkillId.Value);
        }

        var existingNames = await query.Select(s => s.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }
}
