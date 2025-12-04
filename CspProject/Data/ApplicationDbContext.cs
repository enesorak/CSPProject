using CspProject.Data.Entities;
using CspProject.Services.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace CspProject.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<Document> Documents { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<EmailSetting> EmailSettings { get; set; }
        public DbSet<ApprovalToken> ApprovalTokens { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        // ✅ Parameterless constructor for DI
        public ApplicationDbContext()
        {
        }

        // ✅ Constructor that accepts DbContextOptions (DI pattern)
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // ✅ Only configure if not already configured by DI
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder
                    .UseSqlite("Data Source=csp_database.db")
                    .EnableSensitiveDataLogging(false)
                    .EnableDetailedErrors(false)
                    .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // ✅ Add indexes for better query performance
            modelBuilder.Entity<Document>()
                .HasIndex(d => d.Status);

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.ModifiedDate);

            modelBuilder.Entity<Document>()
                .HasIndex(d => d.AuthorId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.DocumentId);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.Timestamp);
            
            
            var defaultSmtpServer = ConfigurationService.Configuration["Email:DefaultSmtpServer"] ?? "smtp.gmail.com";
            var defaultSmtpPort = int.TryParse(ConfigurationService.Configuration["Email:DefaultSmtpPort"], out var smtpPort) ? smtpPort : 587;
            var defaultImapServer = ConfigurationService.Configuration["Email:DefaultImapServer"] ?? "imap.gmail.com";
            var defaultImapPort = int.TryParse(ConfigurationService.Configuration["Email:DefaultImapPort"], out var imapPort) ? imapPort : 993;
            var defaultEnableSsl = ConfigurationService.Configuration["Email:DefaultEnableSsl"] != "false"; // Default true
            var defaultSenderName = ConfigurationService.Configuration["Email:DefaultSenderName"] ?? "CSP Application";


            // ✅ Seed default email settings
            modelBuilder.Entity<EmailSetting>().HasData(
                new EmailSetting
                {
                    Id = 1,
                    SmtpServer = defaultSmtpServer,
                    SmtpPort = defaultSmtpPort,
                    ImapServer = defaultImapServer,
                    ImapPort = defaultImapPort,
                    SenderEmail = "", // ✅ Boş - Kullanıcı girecek
                    SenderName = defaultSenderName,
                    Password = "", // ✅ Boş - Kullanıcı girecek
                    EnableSsl = defaultEnableSsl
                }
            );
        }
    }
}