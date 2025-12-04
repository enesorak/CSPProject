using Microsoft.Extensions.Configuration;
using System.IO;

namespace CspProject.Services.Infrastructure
{
    public static class ConfigurationService
    {
        private static IConfiguration? _configuration;
        
        public static IConfiguration Configuration
        {
            get
            {
                if (_configuration == null)
                {
                    Initialize();
                }
                return _configuration!;
            }
        }

        public static void Initialize()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

            // ✅ Önce base appsettings.json
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json")))
            {
                builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            }

            // ✅ Sonra environment-specific (Development, Production)
#if DEBUG
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.Development.json")))
            {
                builder.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
            }
#endif

            _configuration = builder.Build();
        }

        // ✅ Helper metodlar
        public static string GetSentryDsn() => Configuration["Sentry:Dsn"] ?? string.Empty;
        
        public static string GetConnectionString() => Configuration["Database:ConnectionString"] ?? "Data Source=csp_database.db";
        
        public static int GetAutoCheckInterval() => int.TryParse(Configuration["Application:AutoCheckApprovalInterval"], out var interval) ? interval : 5;
    }
}