using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Inkwell_Kunal.Data;
using Microsoft.Maui.Storage;
using System.IO;

// For unhandled exception handlers
using System;
using System.Threading.Tasks;
using Inkwell_Kunal.Services;

namespace Inkwell_Kunal;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// Configure EF Core with SQLite using a file in the app data directory
		var dbPath = System.IO.Path.Combine(FileSystem.AppDataDirectory, "journal.db");
		builder.Services.AddDbContext<AppDbContext>(options =>
			options.UseSqlite($"Data Source={dbPath}"));

		// Register DB initializer
		builder.Services.AddTransient<DbInitializer>();

		// Register journal service
		builder.Services.AddScoped<IJournalService, JournalService>();

		// Register export service (PDF generation)
		builder.Services.AddScoped<IExportService, ExportService>();

		// Register theme service (used by Blazor components)
		builder.Services.AddScoped<ThemeService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		var app = builder.Build();

		// Register global unhandled exception handlers to aid debugging
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			try { LogUnhandled(e.ExceptionObject as Exception); } catch { }
		};

		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			try { LogUnhandled(e.Exception); e.SetObserved(); } catch { }
		};

		// Ensure DB created at startup (safe: catch and log any errors)
		using (var scope = app.Services.CreateScope())
		{
			var initializer = scope.ServiceProvider.GetRequiredService<DbInitializer>();
			try
			{
				initializer.Initialize();
			}
			catch (Exception ex)
			{
				LogUnhandled(ex);
			}
		}

		return app;
	}

	private static void LogUnhandled(Exception? ex)
	{
		try
		{
			var dataDir = FileSystem.AppDataDirectory;
			var logPath = Path.Combine(dataDir, "unhandled-errors.log");
			var msg = $"[{DateTime.UtcNow:u}] Unhandled: {ex?.Message}\n{ex?.StackTrace}\n";
			File.AppendAllText(logPath, msg);
			System.Diagnostics.Debug.WriteLine(msg);
		}
		catch { }
	}
}
