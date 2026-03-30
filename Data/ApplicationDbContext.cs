using HabitTrackerWeb.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HabitTrackerWeb.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Habit> Habits => Set<Habit>();
    public DbSet<HabitLog> HabitLogs => Set<HabitLog>();
    public DbSet<HabitMetric> HabitMetrics => Set<HabitMetric>();
    public DbSet<Achievement> Achievements => Set<Achievement>();
    public DbSet<ExternalAccountLink> ExternalAccountLinks => Set<ExternalAccountLink>();
    public DbSet<EloRating> EloRatings => Set<EloRating>();
    public DbSet<EloRatingChange> EloRatingChanges => Set<EloRatingChange>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Category>()
            .HasIndex(c => c.Name)
            .IsUnique();

        builder.Entity<Habit>()
            .HasOne(h => h.Category)
            .WithMany(c => c.Habits)
            .HasForeignKey(h => h.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Habit>()
            .HasOne(h => h.ApplicationUser)
            .WithMany(u => u.Habits)
            .HasForeignKey(h => h.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Habit>()
            .HasIndex(h => new { h.ApplicationUserId, h.IsActive });

        builder.Entity<Habit>()
            .HasIndex(h => new { h.ApplicationUserId, h.AutoCompleteFromExternal, h.ExternalSource });

        builder.Entity<HabitLog>()
            .HasOne(l => l.Habit)
            .WithMany(h => h.HabitLogs)
            .HasForeignKey(l => l.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HabitLog>()
            .HasIndex(l => new { l.HabitId, l.ApplicationUserId, l.LoggedDate })
            .IsUnique();

        builder.Entity<HabitMetric>()
            .HasOne(m => m.Habit)
            .WithOne(h => h.HabitMetric)
            .HasForeignKey<HabitMetric>(m => m.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<HabitMetric>()
            .HasIndex(m => m.HabitId)
            .IsUnique();

        builder.Entity<Achievement>()
            .HasOne(a => a.Habit)
            .WithMany(h => h.Achievements)
            .HasForeignKey(a => a.HabitId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Achievement>()
            .HasIndex(a => new { a.HabitId, a.ApplicationUserId, a.Type })
            .IsUnique();

        builder.Entity<ExternalAccountLink>()
            .HasOne(x => x.ApplicationUser)
            .WithMany(u => u.ExternalAccountLinks)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ExternalAccountLink>()
            .HasIndex(x => new { x.ApplicationUserId, x.Source, x.ExternalUserName })
            .IsUnique();

        builder.Entity<EloRating>()
            .HasOne(x => x.ApplicationUser)
            .WithOne(u => u.EloRating)
            .HasForeignKey<EloRating>(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EloRating>()
            .HasIndex(x => x.ApplicationUserId)
            .IsUnique();

        builder.Entity<EloRatingChange>()
            .HasOne(x => x.ApplicationUser)
            .WithMany(u => u.EloRatingChanges)
            .HasForeignKey(x => x.ApplicationUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<EloRatingChange>()
            .HasOne(x => x.Habit)
            .WithMany(h => h.EloRatingChanges)
            .HasForeignKey(x => x.HabitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<EloRatingChange>()
            .HasIndex(x => new { x.ApplicationUserId, x.HabitId, x.OccurredDate, x.OutcomeType })
            .IsUnique();

        builder.Entity<EloRatingChange>()
            .HasIndex(x => new { x.ApplicationUserId, x.OccurredDate });
    }
}
