using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Inkwell_Kunal.Data
{
    public class ExportService : IExportService
    {
        private readonly IJournalService _journalService;

        public ExportService(IJournalService journalService)
        {
            _journalService = journalService;
        }

        public async Task<byte[]> GeneratePdfAsync(DateTime startUtc, DateTime endUtc)
        {
            var entries = (await _journalService.GetEntriesForRangeAsync(startUtc.Date, endUtc.Date.AddDays(1))).OrderBy(e => e.CreatedAt).ToArray();

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(12));
                    page.PageColor(QuestPDF.Helpers.Colors.White);

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Journal Export").FontSize(18).SemiBold();
                            col.Item().Text($"{startUtc:yyyy-MM-dd} — {endUtc:yyyy-MM-dd}").FontSize(10).FontColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                        });
                    });

                    page.Content().Column(col =>
                    {
                        col.Spacing(10);

                        foreach (var e in entries)
                        {
                            col.Item().LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten3);
                            col.Item().Text(e.Title ?? string.Empty).FontSize(14).Bold();
                            col.Item().Text($"Date: {e.CreatedAt:u}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            if (!string.IsNullOrWhiteSpace(e.PrimaryMood))
                                col.Item().Text($"Mood: {e.PrimaryMood}{(e.SecondaryMoods?.Length>0? (" ("+string.Join(", ", e.SecondaryMoods)+")") : string.Empty)}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                            if (e.Tags?.Length > 0)
                                col.Item().Text($"Tags: {string.Join(", ", e.Tags)}").FontSize(9).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);

                            col.Item().Text(e.Content ?? string.Empty).FontSize(11);
                        }

                        if (entries.Length == 0)
                        {
                            col.Item().Text("No entries in the selected range.").FontSize(12).FontColor(QuestPDF.Helpers.Colors.Grey.Darken1);
                        }
                    });

                    page.Footer().AlignCenter().Text($"Generated: {DateTime.UtcNow:u}").FontSize(9);
                });
            });

            await using var ms = new MemoryStream();
            doc.GeneratePdf(ms);
            return ms.ToArray();
        }
    }
}
