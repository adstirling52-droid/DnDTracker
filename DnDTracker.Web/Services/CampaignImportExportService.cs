using System.Text.Json;
using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using DnDTracker.Web.Models.ImportExport;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public record CampaignImportSummary(
    int ItemsWithProvenanceSkipped,
    int ItemsWithImagesSkipped);

public class CampaignImportExportService(DnDTrackerDbContext db, CampaignService campaignService)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public async Task<(string? Json, string? FileName, string? Error)> ExportAsync(
        string userId,
        Guid campaignId)
    {
        var campaign = await db.Campaigns
            .AsNoTracking()
            .Include(c => c.Characters)
                .ThenInclude(character => character.Items)
            .Include(c => c.Characters)
                .ThenInclude(character => character.Skills)
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == campaignId && c.UserId == userId);

        if (campaign is null)
        {
            return (null, null, "Campaign not found.");
        }

        var dto = new DesktopCampaignDto
        {
            Name = campaign.Name,
            Characters = campaign.Characters
                .OrderBy(character => character.Name)
                .Select(character => new DesktopCharacterDto
                {
                    Name = character.Name,
                    Items = character.Items
                        .OrderBy(item => item.Name)
                        .Select(MapItemToDto)
                        .ToList(),
                    Skills = character.Skills
                        .OrderBy(skill => skill.Name)
                        .Select(skill => new DesktopSkillDto
                        {
                            Name = skill.Name,
                            Description = skill.Description,
                            Notes = skill.Notes
                        })
                        .ToList()
                })
                .ToList(),
            UnassignedItems = campaign.Items
                .Where(item => item.CharacterId is null)
                .OrderBy(item => item.Name)
                .Select(MapItemToDto)
                .ToList()
        };

        var json = JsonSerializer.Serialize(dto, JsonOptions);
        var fileName = $"{SanitizeFileName(campaign.Name)}.json";
        return (json, fileName, null);
    }

    public async Task<(Campaign? Campaign, CampaignImportSummary? Summary, string? Error)> ImportAsync(
        string userId,
        string json)
    {
        DesktopCampaignDto? dto;
        try
        {
            dto = JsonSerializer.Deserialize<DesktopCampaignDto>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return (null, null, "The selected file is not valid campaign JSON.");
        }

        if (dto is null)
        {
            return (null, null, "The selected file is not valid campaign JSON.");
        }

        var campaignName = dto.Name.Trim();
        if (string.IsNullOrWhiteSpace(campaignName))
        {
            return (null, null, "The campaign file does not contain a campaign name.");
        }

        if (await campaignService.NameExistsAsync(userId, campaignName))
        {
            return (null, null, "A campaign with that name already exists.");
        }

        var summary = new CampaignImportSummary(0, 0);
        await using var transaction = await db.Database.BeginTransactionAsync();

        try
        {
            var campaign = new Campaign
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = campaignName
            };

            db.Campaigns.Add(campaign);

            var characterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var characterDto in dto.Characters)
            {
                var characterName = characterDto.Name.Trim();
                if (string.IsNullOrWhiteSpace(characterName))
                {
                    await transaction.RollbackAsync();
                    return (null, null, "A character in the campaign file is missing a name.");
                }

                if (!characterNames.Add(characterName))
                {
                    await transaction.RollbackAsync();
                    return (null, null, $"The campaign file contains duplicate character names ('{characterName}').");
                }

                var character = new Character
                {
                    Id = Guid.NewGuid(),
                    CampaignId = campaign.Id,
                    Name = characterName
                };

                db.Characters.Add(character);

                var (itemError, updatedSummary) = AddItems(
                    campaign.Id,
                    character.Id,
                    characterDto.Items,
                    summary);
                summary = updatedSummary;
                if (itemError is not null)
                {
                    await transaction.RollbackAsync();
                    return (null, null, itemError);
                }

                var skillNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var skillDto in characterDto.Skills)
                {
                    var skillName = skillDto.Name.Trim();
                    if (string.IsNullOrWhiteSpace(skillName))
                    {
                        await transaction.RollbackAsync();
                        return (null, null, $"Character '{characterName}' contains a skill without a name.");
                    }

                    if (!skillNames.Add(skillName))
                    {
                        await transaction.RollbackAsync();
                        return (null, null, $"Character '{characterName}' contains duplicate skill names ('{skillName}').");
                    }

                    db.Skills.Add(new Skill
                    {
                        Id = Guid.NewGuid(),
                        CharacterId = character.Id,
                        Name = skillName,
                        Description = skillDto.Description.Trim(),
                        Notes = skillDto.Notes.Trim()
                    });
                }
            }

            var (unassignedError, finalSummary) = AddItems(
                campaign.Id,
                null,
                dto.UnassignedItems,
                summary);
            summary = finalSummary;
            if (unassignedError is not null)
            {
                await transaction.RollbackAsync();
                return (null, null, unassignedError);
            }

            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            return (campaign, summary, null);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    private (string? Error, CampaignImportSummary Summary) AddItems(
        Guid campaignId,
        Guid? characterId,
        IEnumerable<DesktopItemDto> itemDtos,
        CampaignImportSummary summary)
    {
        var itemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var itemDto in itemDtos)
        {
            var itemName = itemDto.Name.Trim();
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return (characterId.HasValue
                    ? "A character item in the campaign file is missing a name."
                    : "An unassigned item in the campaign file is missing a name.", summary);
            }

            if (!itemNames.Add(itemName))
            {
                return (characterId.HasValue
                    ? $"A character contains duplicate item names ('{itemName}')."
                    : $"The unassigned item pool contains duplicate item names ('{itemName}').", summary);
            }

            if (itemDto.ProvenanceEntries is { Count: > 0 })
            {
                summary = summary with
                {
                    ItemsWithProvenanceSkipped = summary.ItemsWithProvenanceSkipped + 1
                };
            }

            if (!string.IsNullOrWhiteSpace(itemDto.ImagePath))
            {
                summary = summary with
                {
                    ItemsWithImagesSkipped = summary.ItemsWithImagesSkipped + 1
                };
            }

            db.Items.Add(new Item
            {
                Id = Guid.NewGuid(),
                CampaignId = campaignId,
                CharacterId = characterId,
                Name = itemName,
                Description = itemDto.Description.Trim(),
                WhereFound = itemDto.WhereFound.Trim(),
                WhenFound = itemDto.WhenFound.Trim(),
                CurrentStatus = itemDto.CurrentStatus.Trim(),
                Notes = itemDto.Notes.Trim(),
                ImagePath = ""
            });
        }

        return (null, summary);
    }

    private static DesktopItemDto MapItemToDto(Item item) => new()
    {
        Name = item.Name,
        Description = item.Description,
        WhereFound = item.WhereFound,
        WhenFound = item.WhenFound,
        CurrentStatus = item.CurrentStatus,
        Notes = item.Notes,
        ImagePath = ""
    };

    private static string SanitizeFileName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(name.Select(ch => invalidChars.Contains(ch) ? '_' : ch)).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "campaign" : sanitized;
    }
}
