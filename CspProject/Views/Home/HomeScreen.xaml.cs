using System.Windows;
using System.Windows.Threading;
using CspProject.Data;
using CspProject.Data.Entities;
using CspProject.Services.Email;
using CspProject.Services.Infrastructure;
using CspProject.Views.Approvals;
using CspProject.Views.Audit;
using CspProject.Views.Documents;
using CspProject.Views.Settings;
using CspProject.Views.Templates;
using DevExpress.Mvvm;
using Button = System.Windows.Controls.Button;

namespace CspProject.Views.Home
{
    public partial class HomeScreen : UserControl,IDisposable 
    {
        private readonly User _currentUser;
        private readonly ApplicationDbContext _dbContext = new ApplicationDbContext();
        private readonly DispatcherTimer _backgroundTimer;
        
        private bool _disposed = false;

        // ✅ Cache views to prevent multiple instances
       //private readonly Dictionary<string, UserControl> _cachedViews = new();
        
        public event EventHandler? RequestBlankSpreadsheet;
        public event EventHandler? RequestNewFmeaDocument;
        public event EventHandler<string>? RequestNewDocumentFromFile;
        public event EventHandler<string>? RequestNewDocumentFromTemplate;
        public event EventHandler? RequestNewDocument;
        public event EventHandler<int>? RequestOpenDocument;
        public event EventHandler<string>? RequestNavigate;

        public HomeScreen(User currentUser)
        {
            InitializeComponent();
            _currentUser = currentUser;
            CurrentUserTextBlock.Text = _currentUser.Name;
            VersionTextBlock.Text = $"Version: {App.AppVersion}";

            NavigateTo("Home");

            var intervalMinutes = ConfigurationService.GetAutoCheckInterval();

            
            _backgroundTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(intervalMinutes)
            };
            _backgroundTimer.Tick += BackgroundTimer_Tick;
            _backgroundTimer.Start();
            
            this.Unloaded += HomeScreen_Unloaded;

        }
        
        
        
        private void HomeScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            // Event subscription'ı temizle
            this.Unloaded -= HomeScreen_Unloaded;
            
            // Dispose pattern kullan
            Dispose();
            
            SentrySdk.AddBreadcrumb("HomeScreen unloaded", "lifecycle");
        }

        private async void BackgroundTimer_Tick(object? sender, EventArgs e)
        {
            await CheckForApprovals(isSilent: true);
        }

        private async Task CheckForApprovals(bool isSilent)
        {
            var receiverService = new EmailReceiverService(_dbContext);
            string resultMessage = string.Empty;
            try
            {
                resultMessage = await receiverService.CheckForApprovalEmailsAsync();
                if (resultMessage.Contains("processed"))
                {
                    // ✅ Refresh current view instead of navigating
                    RefreshCurrentView();
                }
            }
            catch (Exception ex)
            {
                resultMessage = $"Error: {ex.GetType().Name}";
                SentrySdk.CaptureException(ex);
            }
            finally
            { 
                if (!isSilent && resultMessage.Contains("processed"))
                {
                    var notificationService = ServiceContainer.Default.GetService<INotificationService>();
            
                    if (notificationService != null)
                    {
                        var notification = notificationService.CreatePredefinedNotification(
                            "Approvals Processed", resultMessage, "");
                        await notification.ShowAsync();
                    }
                }
            }
        }

        private void NavigationButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string pageTag)
            {
                NavigateTo(pageTag);
            }
        }

        private void NavigateTo(string pageTag)
        {
            // ✅ Eski view'ı dispose et
            if (PageContentControl.Content is IDisposable oldView)
            {
                oldView.Dispose();
            }
            
            // ✅ Her seferinde yeni view oluştur
            var view = CreateView(pageTag);
            if (view != null)
            {
                PageContentControl.Content = view;
            }
        }

        private UserControl? CreateView(string pageTag)
        {
            switch (pageTag)
            {
                case "Home":
                    var homeContent = new HomeContentView();
                    homeContent.EnsureInitialized(); // ✅ DbContext'i başlat

                    homeContent.RequestNewDocument += (s, e) => RequestNewFmeaDocument?.Invoke(s, e);
                    homeContent.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return homeContent;
                    
                case "MyDocuments":
                    var myDocsView = new MyDocumentsView();
                    myDocsView.EnsureInitialized(); // ✅ DbContext'i başlat

                    myDocsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return myDocsView;
                    
                case "Approvals":
                    var approvalsView = new ApprovalsView();
                    approvalsView.EnsureInitialized(); // ✅ DbContext'i başlat

                    approvalsView.RequestOpenDocument += (s, id) => RequestOpenDocument?.Invoke(s, id);
                    return approvalsView;
                    
                case "Templates":
                    var templatesView = new TemplatesView();
                    templatesView.RequestNewFmeaDocument += (s, e) => 
                        RequestNewFmeaDocument?.Invoke(this, e);
                    templatesView.RequestNewDocumentFromFile += (s, filePath) => 
                        RequestNewDocumentFromFile?.Invoke(this, filePath);
                    templatesView.RequestBlankSpreadsheet += (s, e) =>
                        RequestBlankSpreadsheet?.Invoke(this, e);
                    return templatesView;
                    
                case "ChangeLog":
                    var changeLogView = new ChangeLogView();
                    changeLogView.EnsureInitialized(); // ✅ DbContext'i başlat
                    return changeLogView;
                    
                case "Settings":
                    var settingsView = new SettingsView(_currentUser);
                    settingsView.EnsureInitialized(); // ✅ DbContext'i başlat
                    return settingsView;
                    
                default:
                    return null;
            }
        }

        /// <summary>
        /// Refreshes the currently displayed view
        /// </summary>
        private void RefreshCurrentView()
        {
            string? currentTag = null;
            
            if (PageContentControl.Content is HomeContentView)
                currentTag = "Home";
            else if (PageContentControl.Content is MyDocumentsView)
                currentTag = "MyDocuments";
            else if (PageContentControl.Content is ApprovalsView)
                currentTag = "Approvals";
            
            // Varsa yenile
            if (currentTag != null)
            {
                NavigateTo(currentTag);
            }
        }

        /// <summary>
        /// Cleanup all cached views
        /// </summary>
        private void CleanupResources()
        {
            try
            {
                // ✅ Şu anki view'ı dispose et
                if (PageContentControl.Content is IDisposable currentView)
                {
                    currentView.Dispose();
                    PageContentControl.Content = null; // ✅ Reference'ı temizle
                }
        
                // ✅ Timer'ı durdur ve dispose et
                if (_backgroundTimer != null)
                {
                    _backgroundTimer.Stop();
                    _backgroundTimer.Tick -= BackgroundTimer_Tick; // ✅ Event subscription temizle
                }
        
                // ✅ DbContext'i dispose et
                _dbContext?.Dispose();
        
                SentrySdk.AddBreadcrumb("HomeScreen resources cleaned up successfully", "cleanup");
            }
            catch (Exception ex)
            {
                // Cleanup sırasında hata olursa logla ama crash etme
                SentrySdk.CaptureException(ex);
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    CleanupResources();
                }
                _disposed = true;
            }
        }
    }
}