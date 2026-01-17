using System;
using System.Collections.Generic;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace Inkwell_Kunal.Data;

public class DbInitializer
{
    private readonly AppDbContext _context;

    public DbInitializer(AppDbContext context)
    {
        _context = context;
    }

    public void Initialize()
    {
        // If database doesn't exist/create schema, create it.
        try
        {
            var conn = _context.Database.GetDbConnection();
            conn.Open();
            try
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='JournalEntries';";
                var exists = cmd.ExecuteScalar() != null;

                if (!exists)
                {
                    // Fresh DB: create schema
                    _context.Database.EnsureCreated();
                    return;
                }

                // Table exists — ensure new columns are present, add them if missing
                cmd.CommandText = "PRAGMA table_info('JournalEntries');";
                var existingCols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // column name is at index 1
                        existingCols.Add(reader.GetString(1));
                    }
                }

                if (!existingCols.Contains("PrimaryMood"))
                {
                    using var add = conn.CreateCommand();
                    add.CommandText = "ALTER TABLE JournalEntries ADD COLUMN PrimaryMood TEXT NOT NULL DEFAULT '';";
                    add.ExecuteNonQuery();
                }

                if (!existingCols.Contains("SecondaryMoodsCsv"))
                {
                    using var add2 = conn.CreateCommand();
                    add2.CommandText = "ALTER TABLE JournalEntries ADD COLUMN SecondaryMoodsCsv TEXT DEFAULT '';";
                    add2.ExecuteNonQuery();
                }

                if (!existingCols.Contains("TagsCsv"))
                {
                    using var add3 = conn.CreateCommand();
                    add3.CommandText = "ALTER TABLE JournalEntries ADD COLUMN TagsCsv TEXT DEFAULT '';";
                    add3.ExecuteNonQuery();
                }
            }
            finally
            {
                try { conn.Close(); } catch { }
            }
        }
        catch (Exception ex)
        {
            // Fallback: try EnsureCreated and log the exception through debug output
            try { _context.Database.EnsureCreated(); } catch { }
            System.Diagnostics.Debug.WriteLine($"DbInitializer error: {ex}");
        }
    }
}

