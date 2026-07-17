using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class CampaignService(DnDTrackerDbContext db)
{
    public Task<List<Campaign>> GetCampaignsAsync(string userId) =>
        db.Campaigns
            .AsNoTracking()
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();

    public async Task<Campaign?> GetCampaignAsync(string userId, Guid campaignId) =>
        await db.Campaigns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

    public async Task<bool> NameExistsAsync(string userId, string name, Guid? excludeCampaignId = null)
    {
        var trimmedName = name.Trim();
        var query = db.Campaigns.Where(c => c.UserId == userId);

        if (excludeCampaignId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCampaignId.Value);
        }

        var existingNames = await query.Select(c => c.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, trimmedName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<(Campaign? Campaign, string? Error)> CreateAsync(string userId, string name)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (null, "Please enter a campaign name.");
        }

        if (await NameExistsAsync(userId, trimmedName))
        {
            return (null, "A campaign with that name already exists.");
        }

        var campaign = new Campaign
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = trimmedName
        };

        db.Campaigns.Add(campaign);
        await db.SaveChangesAsync();
        return (campaign, null);
    }

    public async Task<string?> UpdateAsync(string userId, Guid campaignId, string name)
    {
        var trimmedName = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return "Please enter a campaign name.";
        }

        if (await NameExistsAsync(userId, trimmedName, campaignId))
        {
            return "A campaign with that name already exists.";
        }

        var campaign = await db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

        if (campaign is null)
        {
            return "Campaign not found.";
        }

        campaign.Name = trimmedName;
        await db.SaveChangesAsync();
        return null;
    }

    public async Task<bool> DeleteAsync(string userId, Guid campaignId)
    {
        var campaign = await db.Campaigns
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

        if (campaign is null)
        {
            return false;
        }

        db.Campaigns.Remove(campaign);
        await db.SaveChangesAsync();
        return true;
    }
}
