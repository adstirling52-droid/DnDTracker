using DnDTracker.Web.Data;
using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Services;

public class RollTableService(DnDTrackerDbContext db)
{
    public async Task<List<RollTable>> GetAllAsync(string userId) =>
        await db.RollTables
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Name)
            .ToListAsync();

    public async Task<RollTable?> GetWithRowsAsync(string userId, Guid rollTableId) =>
        await db.RollTables
            .AsNoTracking()
            .Include(t => t.Rows.OrderBy(r => r.Number))
            .FirstOrDefaultAsync(t => t.Id == rollTableId && t.UserId == userId);

    public async Task<(RollTable? Table, string? Error)> ImportFromCsvAsync(
        string userId,
        string tableName,
        string tableType,
        string csvContent)
    {
        var trimmedName = tableName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return (null, "Please provide a table name.");
        }

        if (await NameExistsAsync(userId, trimmedName))
        {
            return (null, "A rollable table with that name already exists.");
        }

        try
        {
            var rows = ParseCsvRows(csvContent);
            if (rows.Count == 0)
            {
                return (null, "The CSV file must contain a header row and at least one data row.");
            }

            var table = new RollTable
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Name = trimmedName,
                Category = "",
                TableType = NormalizeTableType(tableType)
            };

            foreach (var row in rows)
            {
                row.Id = Guid.NewGuid();
                row.RollTableId = table.Id;
                table.Rows.Add(row);
            }

            db.RollTables.Add(table);
            await db.SaveChangesAsync();
            return (table, null);
        }
        catch (InvalidOperationException ex)
        {
            return (null, ex.Message);
        }
    }

    public async Task<bool> DeleteAsync(string userId, Guid rollTableId)
    {
        var table = await db.RollTables
            .FirstOrDefaultAsync(t => t.Id == rollTableId && t.UserId == userId);

        if (table is null)
        {
            return false;
        }

        db.RollTables.Remove(table);
        await db.SaveChangesAsync();
        return true;
    }

    public static ItemInput CreateItemInputFromRoll(RollTableRow row, string currentStatus) =>
        new(
            row.Name,
            row.PhysicalDescription,
            "",
            "",
            currentStatus,
            row.SpecialCharacteristics);

    public static (string Name, string Description, string Notes) CreateSkillInputFromRoll(RollTableRow row) =>
        (row.Name, row.PhysicalDescription, row.SpecialCharacteristics);

    private static List<RollTableRow> ParseCsvRows(string csvContent)
    {
        var lines = csvContent
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 2)
        {
            throw new InvalidOperationException("The CSV file must contain a header row and at least one data row.");
        }

        var rows = new List<RollTableRow>();

        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < 4)
            {
                throw new InvalidOperationException($"Row {i + 1} does not contain 4 columns.");
            }

            if (!int.TryParse(parts[0].Trim(), out var number))
            {
                throw new InvalidOperationException($"Row {i + 1} has an invalid Number value.");
            }

            rows.Add(new RollTableRow
            {
                Number = number,
                Name = parts[1].Trim(),
                PhysicalDescription = parts[2].Trim(),
                SpecialCharacteristics = parts[3].Trim()
            });
        }

        return rows;
    }

    private static string NormalizeTableType(string tableType)
    {
        if (string.Equals(tableType, "Item", StringComparison.OrdinalIgnoreCase))
        {
            return "Item";
        }

        if (string.Equals(tableType, "Skill", StringComparison.OrdinalIgnoreCase))
        {
            return "Skill";
        }

        return "Generic";
    }

    private async Task<bool> NameExistsAsync(string userId, string name, Guid? excludeTableId = null)
    {
        var query = db.RollTables.Where(t => t.UserId == userId);
        if (excludeTableId.HasValue)
        {
            query = query.Where(t => t.Id != excludeTableId.Value);
        }

        var existingNames = await query.Select(t => t.Name).ToListAsync();
        return existingNames.Any(existingName =>
            string.Equals(existingName, name, StringComparison.OrdinalIgnoreCase));
    }
}
