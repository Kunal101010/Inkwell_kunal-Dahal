using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Inkwell_Kunal.Data.Models;

namespace Inkwell_Kunal.Data;

public class JournalService : IJournalService
{
    private readonly AppDbContext _db;

    public JournalService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<JournalEntry?> GetEntryForDateAsync(DateTime dateUtc)
    {
        var start = dateUtc.Date;
        var end = start.AddDays(1);
        return await _db.JournalEntries
            .Where(e => e.CreatedAt >= start && e.CreatedAt < end)
            .OrderByDescending(e => e.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<JournalEntry[]> GetAllEntriesAsync()
    {
        return await _db.JournalEntries
            .OrderByDescending(e => e.CreatedAt)
            .ToArrayAsync();
    }

    public async Task<JournalEntry[]> GetEntriesForRangeAsync(DateTime startUtc, DateTime endUtc)
    {
        return await _db.JournalEntries
            .Where(e => e.CreatedAt >= startUtc && e.CreatedAt < endUtc)
            .OrderBy(e => e.CreatedAt)
            .ToArrayAsync();
    }

    public async Task<(JournalEntry[] Entries, int TotalCount)> GetEntriesPageAsync(int pageIndex, int pageSize)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;
        var total = await _db.JournalEntries.CountAsync();
        var items = await _db.JournalEntries
            .OrderByDescending(e => e.CreatedAt)
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToArrayAsync();
        return (items, total);
    }

    public async Task<(JournalEntry[] Entries, int TotalCount)> SearchEntriesAsync(string? query, DateTime? startUtc, DateTime? endUtc, string[]? moods, string[]? tags, int pageIndex, int pageSize)
    {
        if (pageIndex < 0) pageIndex = 0;
        if (pageSize <= 0) pageSize = 10;

        IQueryable<JournalEntry> q = _db.JournalEntries;

        if (!string.IsNullOrWhiteSpace(query))
        {
            var qLower = query.Trim().ToLower();
            q = q.Where(e => (e.Title != null && e.Title.ToLower().Contains(qLower)) || (e.Content != null && e.Content.ToLower().Contains(qLower)));
        }

        if (startUtc.HasValue)
            q = q.Where(e => e.CreatedAt >= startUtc.Value);
        if (endUtc.HasValue)
            q = q.Where(e => e.CreatedAt < endUtc.Value);

        if (moods != null && moods.Length > 0)
        {
            var moodSet = moods.Select(m => m.Trim()).Where(m => !string.IsNullOrEmpty(m)).ToArray();
            if (moodSet.Length > 0)
            {
                // Build expression: moodSet.Contains(e.PrimaryMood) OR OR( EF.Functions.Like(","+e.SecondaryMoodsCsv+",","%,m,%") for each m )
                var param = System.Linq.Expressions.Expression.Parameter(typeof(JournalEntry), "e");
                // primary mood contains: moodSet.Contains(e.PrimaryMood)
                var primaryContains = System.Linq.Expressions.Expression.Call(
                    System.Linq.Expressions.Expression.Constant(moodSet),
                    typeof(string[]).GetMethod("Contains", new Type[] { typeof(string) })!,
                    System.Linq.Expressions.Expression.Property(param, nameof(JournalEntry.PrimaryMood)));

                System.Linq.Expressions.Expression? secondaryExpr = null;
                var efFunctions = System.Linq.Expressions.Expression.Property(null, typeof(Microsoft.EntityFrameworkCore.EF).GetProperty(nameof(Microsoft.EntityFrameworkCore.EF.Functions))!);
                var likeMethod = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions).GetMethod("Like", new Type[] { typeof(Microsoft.EntityFrameworkCore.DbFunctions), typeof(string), typeof(string) })!;
                var concatMethod = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string), typeof(string) })!;

                foreach (var m in moodSet)
                {
                    var pattern = System.Linq.Expressions.Expression.Constant("%," + m + ",%", typeof(string));
                    var csvWithCommas = System.Linq.Expressions.Expression.Call(concatMethod, System.Linq.Expressions.Expression.Constant(","), System.Linq.Expressions.Expression.Property(param, nameof(JournalEntry.SecondaryMoodsCsv)), System.Linq.Expressions.Expression.Constant(","));
                    var likeCall = System.Linq.Expressions.Expression.Call(likeMethod, efFunctions, csvWithCommas, pattern);
                    secondaryExpr = secondaryExpr == null ? likeCall : System.Linq.Expressions.Expression.OrElse(secondaryExpr, likeCall);
                }

                System.Linq.Expressions.Expression combined = System.Linq.Expressions.Expression.OrElse(primaryContains, secondaryExpr ?? System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda<Func<JournalEntry, bool>>(combined, param);
                q = q.Where(lambda);
            }
        }

        if (tags != null && tags.Length > 0)
        {
            var tagSet = tags.Select(t => t.Trim()).Where(t => !string.IsNullOrEmpty(t)).ToArray();
            if (tagSet.Length > 0)
            {
                // Build OR expression: EF.Functions.Like(","+e.TagsCsv+",","%,tag,%") for any tag
                var param = System.Linq.Expressions.Expression.Parameter(typeof(JournalEntry), "e");
                System.Linq.Expressions.Expression? tagExpr = null;
                var efFunctions = System.Linq.Expressions.Expression.Property(null, typeof(Microsoft.EntityFrameworkCore.EF).GetProperty(nameof(Microsoft.EntityFrameworkCore.EF.Functions))!);
                var likeMethod = typeof(Microsoft.EntityFrameworkCore.DbFunctionsExtensions).GetMethod("Like", new Type[] { typeof(Microsoft.EntityFrameworkCore.DbFunctions), typeof(string), typeof(string) })!;
                var concatMethod = typeof(string).GetMethod("Concat", new Type[] { typeof(string), typeof(string), typeof(string) })!;

                foreach (var t in tagSet)
                {
                    var pattern = System.Linq.Expressions.Expression.Constant("%," + t + ",%", typeof(string));
                    var csvWithCommas = System.Linq.Expressions.Expression.Call(concatMethod, System.Linq.Expressions.Expression.Constant(","), System.Linq.Expressions.Expression.Property(param, nameof(JournalEntry.TagsCsv)), System.Linq.Expressions.Expression.Constant(","));
                    var likeCall = System.Linq.Expressions.Expression.Call(likeMethod, efFunctions, csvWithCommas, pattern);
                    tagExpr = tagExpr == null ? likeCall : System.Linq.Expressions.Expression.OrElse(tagExpr, likeCall);
                }

                var lambda = System.Linq.Expressions.Expression.Lambda<Func<JournalEntry, bool>>(tagExpr!, param);
                q = q.Where(lambda);
            }
        }

        var total = await q.CountAsync();
        var items = await q.OrderByDescending(e => e.CreatedAt).Skip(pageIndex * pageSize).Take(pageSize).ToArrayAsync();
        return (items, total);
    }

    public async Task<JournalEntry> CreateEntryForTodayAsync(string title, string? content, string primaryMood, string[]? secondaryMoods, string[]? tags)
    {
        var now = DateTime.UtcNow;
        var start = now.Date;
        var end = start.AddDays(1);

        var existing = await _db.JournalEntries.FirstOrDefaultAsync(e => e.CreatedAt >= start && e.CreatedAt < end);
        if (existing != null)
            throw new InvalidOperationException("An entry for today already exists. Use update instead.");

        if (string.IsNullOrWhiteSpace(primaryMood)) throw new InvalidOperationException("Primary mood is required.");
        var filteredSecondary = (secondaryMoods ?? Array.Empty<string>())
            .Where(m => !string.IsNullOrWhiteSpace(m) && !string.Equals(m, primaryMood, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        var filteredTags = (tags ?? Array.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var entry = new JournalEntry
        {
            Title = title,
            Content = content,
            CreatedAt = now,
            UpdatedAt = null,
            PrimaryMood = primaryMood,
            SecondaryMoods = filteredSecondary,
            Tags = filteredTags
        };

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task<JournalEntry> UpdateEntryAsync(int id, string title, string? content, string primaryMood, string[]? secondaryMoods, string[]? tags)
    {
        var entry = await _db.JournalEntries.FindAsync(id);
        if (entry == null) throw new InvalidOperationException("Entry not found.");

        if (string.IsNullOrWhiteSpace(primaryMood)) throw new InvalidOperationException("Primary mood is required.");
        var filteredSecondary = (secondaryMoods ?? Array.Empty<string>())
            .Where(m => !string.IsNullOrWhiteSpace(m) && !string.Equals(m, primaryMood, StringComparison.OrdinalIgnoreCase))
            .Select(m => m.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(2)
            .ToArray();

        var filteredTags = (tags ?? Array.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        entry.Title = title;
        entry.Content = content;
        entry.PrimaryMood = primaryMood;
        entry.SecondaryMoods = filteredSecondary;
        entry.Tags = filteredTags;
        entry.UpdatedAt = DateTime.UtcNow;

        _db.JournalEntries.Update(entry);
        await _db.SaveChangesAsync();
        return entry;
    }

    public async Task DeleteEntryAsync(int id)
    {
        var entry = await _db.JournalEntries.FindAsync(id);
        if (entry == null) return;
        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteEntryForDateAsync(DateTime dateUtc)
    {
        var start = dateUtc.Date;
        var end = start.AddDays(1);
        var entry = await _db.JournalEntries.FirstOrDefaultAsync(e => e.CreatedAt >= start && e.CreatedAt < end);
        if (entry == null) return;
        _db.JournalEntries.Remove(entry);
        await _db.SaveChangesAsync();
    }
}
