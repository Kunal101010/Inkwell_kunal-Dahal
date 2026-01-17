using System;
using System.Threading.Tasks;
using Inkwell_Kunal.Data.Models;

namespace Inkwell_Kunal.Data;

public interface IJournalService
{
    Task<JournalEntry?> GetEntryForDateAsync(DateTime dateUtc);
    Task<JournalEntry[]> GetAllEntriesAsync();
    Task<JournalEntry[]> GetEntriesForRangeAsync(DateTime startUtc, DateTime endUtc);
    Task<(JournalEntry[] Entries, int TotalCount)> GetEntriesPageAsync(int pageIndex, int pageSize);
    Task<(JournalEntry[] Entries, int TotalCount)> SearchEntriesAsync(string? query, DateTime? startUtc, DateTime? endUtc, string[]? moods, string[]? tags, int pageIndex, int pageSize);
    Task<JournalEntry> CreateEntryForTodayAsync(string title, string? content, string primaryMood, string[]? secondaryMoods, string[]? tags);
    Task<JournalEntry> UpdateEntryAsync(int id, string title, string? content, string primaryMood, string[]? secondaryMoods, string[]? tags);
    Task DeleteEntryAsync(int id);
    Task DeleteEntryForDateAsync(DateTime dateUtc);
}
