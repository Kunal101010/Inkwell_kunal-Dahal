using Microsoft.Extensions.Logging;
using Inkwell_Kunal.Data;
using Inkwell_Kunal.Services;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
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
		builder.Services.AddSingleton<Inkwell_Kunal.Services.ThemeService>();
		builder.Services.AddScoped<Inkwell_Kunal.Services.JournalService>();
		builder.Services.AddDbContext<AppDbContext>();                    // Database
		builder.Services.AddScoped<JournalService>();                    // Your service
		builder.Services.AddSingleton<ThemeService>();
		builder.Services.AddScoped<AuthenticationService>();
		builder.Services.AddScoped<PdfExportService>();
		builder.Services.AddMudServices();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
		
	}
	
}
