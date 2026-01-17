using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Inkwell_Kunal.Data;
using System.Text;

namespace Inkwell_Kunal.Services;

public class PdfExportService
{
    public byte[] GenerateJournalPdf(List<JournalEntry> entries, string userName)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(QuestPDF.Helpers.Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .Text($"Journal Export - {userName}")
                    .SemiBold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.Blue.Medium);

                page.Content()
                    .Column(column =>
                    {
                        foreach (var entry in entries.OrderBy(e => e.Date))
                        {
                            column.Item().PageBreak(); // New page for each entry

                            // Entry header
                            column.Item().PaddingBottom(10).Row(row =>
                            {
                                row.RelativeItem().Text($"Date: {entry.Date:dddd, MMMM dd, yyyy}")
                                    .Bold().FontSize(14);

                                if (!string.IsNullOrEmpty(entry.PrimaryMood))
                                {
                                    row.ConstantItem(100).Text($"Mood: {entry.PrimaryMood}")
                                        .FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                                }
                            });

                            // Title
                            if (!string.IsNullOrEmpty(entry.Title))
                            {
                                column.Item().PaddingBottom(10).Text($"Title: {entry.Title}")
                                    .Bold().FontSize(16).FontColor(QuestPDF.Helpers.Colors.Black);
                            }

                            // Secondary moods
                            if (!string.IsNullOrEmpty(entry.SecondaryMood1) || !string.IsNullOrEmpty(entry.SecondaryMood2))
                            {
                                column.Item().PaddingBottom(5).Text(
                                    $"Additional Moods: {string.Join(", ", new[] { entry.SecondaryMood1, entry.SecondaryMood2 }.Where(m => !string.IsNullOrEmpty(m)))}")
                                    .FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            }

                            // Tags
                            if (!string.IsNullOrEmpty(entry.Tags))
                            {
                                column.Item().PaddingBottom(10).Text($"Tags: {entry.Tags}")
                                    .FontSize(10).Italic().FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            }

                            // Content
                            column.Item().PaddingBottom(20).Text(FormatContent(entry.Content))
                                .FontSize(11).LineHeight(1.5f);

                            // Separator
                            column.Item().PaddingVertical(10).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generated on ").FontSize(8);
                        x.Span(DateTime.Now.ToString("f")).FontSize(8);
                        x.Span(" | Inkwell Journal").FontSize(8).FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    });
            });
        });

        return document.GeneratePdf();
    }

    private string FormatContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "No content";

        // Simple text formatting - remove markdown syntax for PDF
        return content
            .Replace("*", "")  // Remove bold/italic
            .Replace("_", "")
            .Replace("#", "")  // Remove headers
            .Replace("`", "")  // Remove code
            .Replace("[", "").Replace("]", "").Replace("(", "").Replace(")", "") // Remove links
            .Trim();
    }
}