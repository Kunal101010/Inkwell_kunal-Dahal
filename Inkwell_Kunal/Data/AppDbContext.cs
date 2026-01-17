using Microsoft.EntityFrameworkCore;
using Inkwell_Kunal.Data.Models;

namespace Inkwell_Kunal.Data;

public class AppDbContext : DbContext
{
	public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
	{
	}

	public DbSet<JournalEntry> JournalEntries { get; set; } = null!;
}
