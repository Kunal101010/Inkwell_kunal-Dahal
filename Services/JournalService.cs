using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Inkwell_Kunal.Services  // ← THIS MUST MATCH EXACTLY
{
    public class JournalEntry
    {
        public DateTime Date { get; set; }
        public string Content { get; set; } = string.Empty;
    }

    public class JournalService
    {
        private List<JournalEntry> _entries = new();

        public event Action? OnChange;

        public Task<List<JournalEntry>> GetEntriesAsync()
        {
            return Task.FromResult(_entries);
        }

        public Task<JournalEntry?> GetEntryAsync(DateTime date)
        {
            var entry = _entries.FirstOrDefault(e => e.Date.Date == date.Date);
            return Task.FromResult(entry);
        }

        public Task SaveEntryAsync(DateTime date, string content)
        {
            var existing = _entries.FirstOrDefault(e => e.Date.Date == date.Date);
            if (existing != null)
            {
                existing.Content = content;
            }
            else
            {
                _entries.Add(new JournalEntry { Date = date.Date, Content = content });
            }

            OnChange?.Invoke();
            return Task.CompletedTask;
        }
    }
}