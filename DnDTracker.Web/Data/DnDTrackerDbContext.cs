using DnDTracker.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace DnDTracker.Web.Data;

public class DnDTrackerDbContext : DbContext
{
    public DnDTrackerDbContext(DbContextOptions<DnDTrackerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<Character> Characters => Set<Character>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<Skill> Skills => Set<Skill>();

    public DbSet<RollTable> RollTables => Set<RollTable>();

    public DbSet<RollTableRow> RollTableRows => Set<RollTableRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasIndex(c => new { c.UserId, c.Name }).IsUnique();
            entity.HasMany(c => c.Characters)
                .WithOne(c => c.Campaign)
                .HasForeignKey(c => c.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(c => c.Items)
                .WithOne(i => i.Campaign)
                .HasForeignKey(i => i.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Character>(entity =>
        {
            entity.HasMany(c => c.Items)
                .WithOne(i => i.Character)
                .HasForeignKey(i => i.CharacterId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(c => c.Skills)
                .WithOne(s => s.Character)
                .HasForeignKey(s => s.CharacterId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RollTable>(entity =>
        {
            entity.HasIndex(r => new { r.UserId, r.Name }).IsUnique();
            entity.HasMany(r => r.Rows)
                .WithOne(r => r.RollTable)
                .HasForeignKey(r => r.RollTableId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
