using System;
using System.Threading.Tasks;

namespace Inkwell_Kunal.Data
{
    public interface IExportService
    {
        Task<byte[]> GeneratePdfAsync(DateTime startUtc, DateTime endUtc);
    }
}
