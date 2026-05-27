using AppBlocker.Models;
using Microsoft.EntityFrameworkCore;

namespace AppBlocker.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<BlockedApp> BlockedApps => Set<BlockedApp>();
    public DbSet<BlockSchedule> BlockSchedules => Set<BlockSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BlockedApp>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(260);
            entity.HasMany(e => e.Schedules)
                  .WithOne(s => s.BlockedApp)
                  .HasForeignKey(s => s.BlockedAppId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlockSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CronExpression).IsRequired().HasMaxLength(100);
        });
    }
}
