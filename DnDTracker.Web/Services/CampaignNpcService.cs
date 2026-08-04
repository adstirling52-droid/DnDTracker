using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using DnDTracker.Web.Models.NpcGenerator;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public record CampaignNpcUpdateInput(
    string Name,
    string Occupation,
    string Location,
    string DmSummary,
    bool IsCurrent);

public class CampaignNpcService(DnDTrackerDbContext db, CampaignNpcImageService npcImageService)
{
    public const int MaxLocationLength = 200;
    public const int MaxDmSummaryLength = 4000;

    public async Task<List<CampaignNpc>> GetAllAsync(string userId, Guid campaignId)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return [];
        }

        return await db.CampaignNpcs
            .AsNoTracking()
            .Where(npc => npc.CampaignId == campaignId)
            .OrderByDescending(npc => npc.SavedAtUtc)
            .ThenBy(npc => npc.Name)
            .ToListAsync();
    }

    public async Task<List<CampaignNpc>> GetCurrentAsync(string userId, Guid campaignId)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return [];
        }

        return await db.CampaignNpcs
            .AsNoTracking()
            .Where(npc => npc.CampaignId == campaignId && npc.IsCurrent)
            .OrderBy(npc => npc.Name)
            .ToListAsync();
    }

    public async Task<CampaignNpc?> GetByIdAsync(string userId, Guid campaignId, Guid npcId)
    {
        return await GetOwnedNpcAsync(userId, campaignId, npcId, asNoTracking: true);
    }

    public async Task<(CampaignNpc? Npc, string? Error)> SaveFromGeneratedAsync(
        string userId,
        Guid campaignId,
        GeneratedNpc generated,
        string imagePrompt)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return (null, "Campaign not found.");
        }

        if (string.IsNullOrWhiteSpace(generated.Name))
        {
            return (null, "The generated NPC is missing a name.");
        }

        var npc = CreateFromGenerated(campaignId, generated, imagePrompt);
        db.CampaignNpcs.Add(npc);
        await db.SaveChangesAsync();
        return (npc, null);
    }

    public async Task<string?> DeleteAsync(string userId, Guid campaignId, Guid npcId)
    {
        var npc = await GetOwnedNpcAsync(userId, campaignId, npcId);
        if (npc is null)
        {
            return "Saved NPC not found.";
        }

        await npcImageService.DeleteFilesForNpcAsync(userId, npcId);
        db.CampaignNpcs.Remove(npc);
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> UpdateAsync(
        string userId,
        Guid campaignId,
        Guid npcId,
        CampaignNpcUpdateInput input)
    {
        var npc = await GetOwnedNpcAsync(userId, campaignId, npcId);
        if (npc is null)
        {
            return "Saved NPC not found.";
        }

        var trimmedName = input.Name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return "Please enter an NPC name.";
        }

        var trimmedLocation = input.Location.Trim();
        if (trimmedLocation.Length > MaxLocationLength)
        {
            return $"Location must be {MaxLocationLength} characters or fewer.";
        }

        var trimmedDmSummary = input.DmSummary.Trim();
        if (trimmedDmSummary.Length > MaxDmSummaryLength)
        {
            return $"DM summary must be {MaxDmSummaryLength} characters or fewer.";
        }

        npc.Name = trimmedName;
        npc.Occupation = input.Occupation.Trim();
        npc.Location = trimmedLocation;
        npc.DmSummary = trimmedDmSummary;
        npc.IsCurrent = input.IsCurrent;
        npc.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return null;
    }

    internal static CampaignNpc CreateFromGenerated(Guid campaignId, GeneratedNpc generated, string imagePrompt) =>
        new()
        {
            Id = Guid.NewGuid(),
            CampaignId = campaignId,
            SavedAtUtc = DateTime.UtcNow,
            Name = generated.Name.Trim(),
            Ancestry = generated.Ancestry.Trim(),
            GenderPresentation = generated.GenderPresentation.Trim(),
            AgeCategory = generated.AgeCategory.Trim(),
            Occupation = generated.Occupation.Trim(),
            Appearance = generated.Appearance.Trim(),
            DistinctiveFeature = generated.DistinctiveFeature.Trim(),
            Personality = generated.Personality.Trim(),
            Mannerism = generated.Mannerism.Trim(),
            Voice = generated.Voice.Trim(),
            Background = generated.Background.Trim(),
            Motivation = generated.Motivation.Trim(),
            Secret = generated.Secret.Trim(),
            CurrentProblem = generated.CurrentProblem.Trim(),
            QuestHook = generated.QuestHook.Trim(),
            DangerOrComplication = generated.DangerOrComplication.Trim(),
            DmSummary = generated.DmSummary.Trim(),
            ImagePrompt = imagePrompt.Trim()
        };

    private async Task<CampaignNpc?> GetOwnedNpcAsync(
        string userId,
        Guid campaignId,
        Guid npcId,
        bool asNoTracking = false)
    {
        if (!await OwnsCampaignAsync(userId, campaignId))
        {
            return null;
        }

        var query = db.CampaignNpcs
            .Include(npc => npc.Campaign)
            .Where(npc =>
                npc.Id == npcId &&
                npc.CampaignId == campaignId &&
                npc.Campaign.UserId == userId);

        return asNoTracking
            ? await query.AsNoTracking().FirstOrDefaultAsync()
            : await query.FirstOrDefaultAsync();
    }

    private Task<bool> OwnsCampaignAsync(string userId, Guid campaignId) =>
        db.Campaigns.AnyAsync(c => c.Id == campaignId && c.UserId == userId);
}
