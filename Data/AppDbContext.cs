using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Inkwell_Kunal.Data;

public class AppDbContext : DbContext
{
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<User> Users => Set<User>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=journal.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(e => new { e.Date, e.UserId })
            .IsUnique(); // Ensures only ONE entry per day per user at the database level

        // Optional: Improve query performance for common lookups
        modelBuilder.Entity<JournalEntry>()
            .HasIndex(e => e.CreatedAt);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<JournalEntry>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(e => e.UserId);

        base.OnModelCreating(modelBuilder);
    }
}

public class JournalEntry
{
    [Key]
    public int Id { get; set; } // Explicit [Key] for clarity (EF would infer it anyway)

    public DateTime Date { get; set; }                   // The day of the entry (date only, e.g., 2026-01-03 00:00:00)

    public string? Title { get; set; } // Optional title for the entry

    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }              // When first created

    public DateTime? UpdatedAt { get; set; }             // When last modified (null if never updated)

    public string? PrimaryMood { get; set; }

    public string? SecondaryMood1 { get; set; }

    public string? SecondaryMood2 { get; set; }

    public string? Tags { get; set; } // Comma-separated tags
    public int? UserId { get; set; }
    public bool IsLocked { get; set; }
    public string? LockPasswordHash { get; set; }
}

public class User
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    public string? PinHash { get; set; } // Optional PIN

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class StreakInfo
{
    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public List<DateTime> MissedDates { get; set; } = new();

    public StreakInfo(int current, int longest, List<DateTime> missed)
    {
        CurrentStreak = current;
        LongestStreak = longest;
        MissedDates = missed;
    }
}