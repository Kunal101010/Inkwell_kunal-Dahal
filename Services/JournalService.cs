using Inkwell_Kunal.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Inkwell_Kunal.Services;

public class JournalService
{
    private readonly AppDbContext _db;
    private readonly AuthenticationService _auth;

    // This event is REQUIRED for the UI to refresh automatically
    public event Action? OnChange;

    public JournalService(AppDbContext db, AuthenticationService auth)
    {
        _db = db;
        _auth = auth;
        _db.Database.EnsureCreated(); // Creates the DB file if it doesn't exist
        // Ensure DB schema contains columns added in later versions.
        try
        {
            EnsureSchema();
        }
        catch
        {
            // If schema update fails, swallow to avoid crashing app on startup.
        }
    }

    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private void EnsureSchema()
    {
        var conn = _db.Database.GetDbConnection();
        try
        {
            if (conn.State != System.Data.ConnectionState.Open)
                conn.Open();

            // Check if Users table exists
            using var tableCmd = conn.CreateCommand();
            tableCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='Users';";
            var tableExists = tableCmd.ExecuteScalar() != null;

            if (!tableExists)
            {
                using var createCmd = conn.CreateCommand();
                createCmd.CommandText = @"
                    CREATE TABLE Users (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        Username TEXT NOT NULL UNIQUE,
                        PasswordHash TEXT NOT NULL,
                        PinHash TEXT,
                        CreatedAt TEXT NOT NULL
                    );";
                createCmd.ExecuteNonQuery();
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA table_info('JournalEntries');";
            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    // second column is the column name
                    existing.Add(reader.GetString(1));
                }
            }

            var alters = new List<string>();
            if (!existing.Contains("Title"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN Title TEXT;");
            if (!existing.Contains("PrimaryMood"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN PrimaryMood TEXT;");
            if (!existing.Contains("SecondaryMood1"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN SecondaryMood1 TEXT;");
            if (!existing.Contains("SecondaryMood2"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN SecondaryMood2 TEXT;");
            if (!existing.Contains("Tags"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN Tags TEXT;");
            if (!existing.Contains("UserId"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN UserId INTEGER NOT NULL DEFAULT 1;");
            if (!existing.Contains("IsLocked"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN IsLocked INTEGER NOT NULL DEFAULT 0;");
            if (!existing.Contains("LockPasswordHash"))
                alters.Add("ALTER TABLE JournalEntries ADD COLUMN LockPasswordHash TEXT;");

            foreach (var sql in alters)
            {
                using var a = conn.CreateCommand();
                a.CommandText = sql;
                a.ExecuteNonQuery();
            }

            // Drop old index if exists to allow new composite index
            using var dropIndexCmd = conn.CreateCommand();
            dropIndexCmd.CommandText = "DROP INDEX IF EXISTS IX_JournalEntries_Date;";
            dropIndexCmd.ExecuteNonQuery();
        }
        finally
        {
            try { conn.Close(); } catch { }
        }
    }

    public static readonly string[] AvailableMoods = new[]
    {
        "Happy", "Sad", "Angry", "Anxious", "Excited", "Calm", "Tired", "Motivated", "Frustrated", "Content", "Overwhelmed", "Peaceful"
    };

    public static readonly string[] SuggestedTags = new[]
    {
        "Work", "Family", "Health", "Travel", "Friends", "Hobbies", "Learning", "Goals", "Reflection", "Gratitude", "Challenges", "Achievements"
    };

    // Get entry for a specific date
    public async Task<JournalEntry?> GetEntryByIdAsync(int id)
{
    if (_auth.CurrentUser == null) return null;
    var entry = await _db.JournalEntries
        .FirstOrDefaultAsync(e => e.Id == id && e.UserId == _auth.CurrentUser.Id);
    if (entry != null && entry.IsLocked)
    {
        return new JournalEntry
        {
            Id = entry.Id,
            Date = entry.Date,
            Content = "[This entry is locked. Please unlock to view content.]",
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            PrimaryMood = entry.PrimaryMood,
            SecondaryMood1 = entry.SecondaryMood1,
            SecondaryMood2 = entry.SecondaryMood2,
            Tags = entry.Tags,
            UserId = entry.UserId,
            IsLocked = true
        };
    }
    return entry;
}

    // Create or Update entry
    public async Task SaveEntryAsync(DateTime date, string content, string? title, string? primaryMood, string? secondaryMood1, string? secondaryMood2, string? tags)
    {
        if (_auth.CurrentUser == null) 
            throw new InvalidOperationException("User not authenticated");

        try
        {
            var entry = await GetEntryAsync(date);

            if (entry == null)
            {
                // Create new entry
                entry = new JournalEntry
                {
                    Date = date.Date,
                    Content = content?.Trim() ?? "",
                    Title = title?.Trim(),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    PrimaryMood = primaryMood,
                    SecondaryMood1 = secondaryMood1,
                    SecondaryMood2 = secondaryMood2,
                    Tags = tags?.Trim(),
                    UserId = _auth.CurrentUser.Id,
                    IsLocked = false
                };
                _db.JournalEntries.Add(entry);
            }
            else
            {
                // Update existing entry
                entry.Content = content?.Trim() ?? "";
                entry.Title = title?.Trim();
                entry.PrimaryMood = primaryMood;
                entry.SecondaryMood1 = secondaryMood1;
                entry.SecondaryMood2 = secondaryMood2;
                entry.Tags = tags?.Trim();
                entry.UpdatedAt = DateTime.Now;
            }

            await _db.SaveChangesAsync();
            OnChange?.Invoke();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error saving entry: {ex.Message}", ex);
        }
    }

    public async Task<JournalEntry?> GetEntryAsync(DateTime date)
    {
        if (_auth.CurrentUser == null) return null;
        
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(e => e.Date.Date == date.Date && e.UserId == _auth.CurrentUser.Id);
        
        if (entry != null && entry.IsLocked)
        {
            return new JournalEntry
            {
                Id = entry.Id,
                Date = entry.Date,
                Content = "[This entry is locked. Please unlock to view content.]",
                CreatedAt = entry.CreatedAt,
                UpdatedAt = entry.UpdatedAt,
                PrimaryMood = entry.PrimaryMood,
                SecondaryMood1 = entry.SecondaryMood1,
                SecondaryMood2 = entry.SecondaryMood2,
                Tags = entry.Tags,
                UserId = entry.UserId,
                IsLocked = true
            };
        }
        return entry;
    }

    // Get mood counts across all entries
    public async Task<Dictionary<string, int>> GetMoodCountsAsync()
    {
        if (_auth.CurrentUser == null) return new Dictionary<string, int>();
        var entries = await _db.JournalEntries.Where(e => e.UserId == _auth.CurrentUser.Id).ToListAsync();
        var counts = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.PrimaryMood))
            {
                counts[entry.PrimaryMood] = counts.GetValueOrDefault(entry.PrimaryMood) + 1;
            }
            if (!string.IsNullOrEmpty(entry.SecondaryMood1))
            {
                counts[entry.SecondaryMood1] = counts.GetValueOrDefault(entry.SecondaryMood1) + 1;
            }
            if (!string.IsNullOrEmpty(entry.SecondaryMood2))
            {
                counts[entry.SecondaryMood2] = counts.GetValueOrDefault(entry.SecondaryMood2) + 1;
            }
        }
        return counts.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    // Get tag counts across all entries
    public async Task<Dictionary<string, int>> GetTagCountsAsync()
    {
        if (_auth.CurrentUser == null) return new Dictionary<string, int>();
        var entries = await _db.JournalEntries.Where(e => e.UserId == _auth.CurrentUser.Id && !string.IsNullOrEmpty(e.Tags)).ToListAsync();
        var counts = new Dictionary<string, int>();
        foreach (var entry in entries)
        {
            if (!string.IsNullOrEmpty(entry.Tags))
            {
                var tags = entry.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                foreach (var tag in tags)
                {
                    counts[tag] = counts.GetValueOrDefault(tag) + 1;
                }
            }
        }
        return counts.OrderByDescending(kv => kv.Value).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    // Delete entry
    public async Task DeleteEntryAsync(DateTime date)
    {
        if (_auth.CurrentUser == null) return;
        var entry = await GetEntryAsync(date);
        if (entry != null)
        {
            _db.JournalEntries.Remove(entry);
            await _db.SaveChangesAsync();
            OnChange?.Invoke(); // Notify UI
        }
    }

    public async Task DeleteEntryAsync(int id)
    {
        var entry = await _db.JournalEntries.FindAsync(id);
        if (entry != null)
        {
            _db.JournalEntries.Remove(entry);
            await _db.SaveChangesAsync();
            OnChange?.Invoke();
        }
    }

    public async Task<List<JournalEntry>> GetAllEntriesAsync()
    {
        if (_auth.CurrentUser == null) return new List<JournalEntry>();
        return await _db.JournalEntries
            .Where(e => e.UserId == _auth.CurrentUser.Id)
            .OrderByDescending(e => e.Date)
            .ToListAsync();
    }

    // Get all dates that have entries
    public async Task<List<DateTime>> GetEntryDatesAsync()
    {
        if (_auth.CurrentUser == null) return new List<DateTime>();
        return await _db.JournalEntries
            .Where(e => e.UserId == _auth.CurrentUser.Id)
            .Select(e => e.Date).ToListAsync();
    }

    // Get streak information: current streak, longest streak, and missed dates (lookback window)
    public async Task<StreakInfo> GetStreakInfoAsync(int lookbackDays = 30)
    {
        if (_auth.CurrentUser == null) return new StreakInfo(0, 0, new List<DateTime>());
        var dates = await _db.JournalEntries
            .Where(e => e.UserId == _auth.CurrentUser.Id)
            .Select(e => e.Date.Date)
            .Distinct()
            .ToListAsync();

        var set = new HashSet<DateTime>(dates);

        // Current streak (count consecutive days up to today)
        int current = 0;
        var day = DateTime.Today;
        while (set.Contains(day))
        {
            current++;
            day = day.AddDays(-1);
        }

        // Longest streak (scan runs)
        int longest = 0;
        foreach (var d in set)
        {
            if (set.Contains(d.AddDays(-1)))
                continue; // not a run start

            int len = 0;
            var cur = d;
            while (set.Contains(cur))
            {
                len++;
                cur = cur.AddDays(1);
            }
            if (len > longest) longest = len;
        }

        // Missed days in lookback window (last N days)
        var missed = new List<DateTime>();
        for (int i = 0; i < lookbackDays; i++)
        {
            var check = DateTime.Today.AddDays(-i);
            if (!set.Contains(check))
                missed.Add(check);
        }
        missed.Reverse();

        return new StreakInfo(current, longest, missed);
    }

    // Get word count trends over time (grouped by month)
    public async Task<Dictionary<DateTime, int>> GetWordCountTrendsAsync(int monthsBack = 12)
    {
        if (_auth.CurrentUser == null) return new Dictionary<DateTime, int>();
        var entries = await _db.JournalEntries
            .Where(e => e.UserId == _auth.CurrentUser.Id && e.Date >= DateTime.Today.AddMonths(-monthsBack))
            .ToListAsync();

        var trends = new Dictionary<DateTime, int>();
        foreach (var entry in entries)
        {
            var monthKey = new DateTime(entry.Date.Year, entry.Date.Month, 1);
            var wordCount = CountWords(entry.Content);
            if (trends.ContainsKey(monthKey))
                trends[monthKey] += wordCount;
            else
                trends[monthKey] = wordCount;
        }
        return trends.OrderBy(kv => kv.Key).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    // Lock entry with password
    public async Task<bool> LockEntryAsync(DateTime date, string password)
    {
        if (_auth.CurrentUser == null) return false;
        var entry = await GetEntryAsync(date);
        if (entry == null) return false;

        entry.IsLocked = true;
        entry.LockPasswordHash = HashPassword(password);
        await _db.SaveChangesAsync();
        OnChange?.Invoke();
        return true;
    }

    // Unlock entry
    public async Task<bool> UnlockEntryAsync(DateTime date, string password)
    {
        if (_auth.CurrentUser == null) return false;
        var entry = await _db.JournalEntries
            .FirstOrDefaultAsync(e => e.Date.Date == date.Date && e.UserId == _auth.CurrentUser.Id);
        if (entry == null || !entry.IsLocked || !VerifyPassword(password, entry.LockPasswordHash ?? ""))
            return false;

        entry.IsLocked = false;
        entry.LockPasswordHash = null;
        await _db.SaveChangesAsync();
        OnChange?.Invoke();
        return true;
    }

    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}