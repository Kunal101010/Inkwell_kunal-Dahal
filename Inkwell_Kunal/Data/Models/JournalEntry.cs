using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Inkwell_Kunal.Data.Models;

public class JournalEntry
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string? Content { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Primary mood required for analytics
    [Required]
    [MaxLength(50)]
    public string PrimaryMood { get; set; } = string.Empty;

    // Comma-separated secondary moods persisted; allows up to two secondary moods
    public string SecondaryMoodsCsv { get; set; } = string.Empty;

    [NotMapped]
    public string[] SecondaryMoods
    {
        get => string.IsNullOrWhiteSpace(SecondaryMoodsCsv) ? Array.Empty<string>() : SecondaryMoodsCsv.Split(',');
        set => SecondaryMoodsCsv = (value == null || value.Length == 0) ? string.Empty : string.Join(',', value);
    }

    // Tags: comma-separated storage and helper property
    public string TagsCsv { get; set; } = string.Empty;

    [NotMapped]
    public string[] Tags
    {
        get => string.IsNullOrWhiteSpace(TagsCsv) ? Array.Empty<string>() : TagsCsv.Split(',');
        set => TagsCsv = (value == null || value.Length == 0) ? string.Empty : string.Join(',', value);
    }
}
