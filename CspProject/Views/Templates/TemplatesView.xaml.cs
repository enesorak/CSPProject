using System.IO;
using System.Windows;
using CspProject.Models;

namespace CspProject.Views.Templates
{
    /// <summary>
    /// View for managing and selecting document templates.
    /// Supports built-in templates, file-based templates, and blank spreadsheets.
    /// </summary>
    public partial class TemplatesView : UserControl
    {
        #region Events
        
        /// <summary>
        /// Raised when user selects the built-in FMEA template
        /// </summary>
        public event EventHandler? RequestNewFmeaDocument;
        
        /// <summary>
        /// Raised when user selects a file-based template
        /// </summary>
        public event EventHandler<string>? RequestNewDocumentFromFile;
        
        /// <summary>
        /// Raised when user requests a blank spreadsheet
        /// </summary>
        public event EventHandler? RequestBlankSpreadsheet;
        
        #endregion

        #region Fields
        
        private readonly string _templateDirectory;
        
        #endregion

        #region Constructor
        
        public TemplatesView()
        {
            InitializeComponent();
            
            // Initialize template directory path
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _templateDirectory = Path.Combine(exePath, "Templates");
            
            LoadTemplates();
        }
        
        #endregion

        #region Private Methods
        
        /// <summary>
        /// Loads all available templates (built-in and file-based) into the grid
        /// </summary>
        private void LoadTemplates()
        {
            var templateList = new List<TemplateInfo>();

            // Add built-in FMEA template
            templateList.Add(new TemplateInfo 
            { 
                Name = "DFMEA Template (Built-in)", 
                Description = "A standard template for Design Failure Mode and Effects Analysis.",
                TemplateType = "BuiltIn",
                Identifier = "BUILTIN_FMEA"
            });

            // Ensure templates directory exists
            if (!Directory.Exists(_templateDirectory))
            {
                try
                {
                    Directory.CreateDirectory(_templateDirectory);
                }
                catch (Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                    MessageBox.Show(
                        $"Could not create templates directory: {ex.Message}", 
                        "Error", 
                        MessageBoxButton.OK, 
                        MessageBoxImage.Error);
                }
            }

            // Load file-based templates from directory
            try
            {
                var templateFiles = Directory.GetFiles(_templateDirectory, "*.xlsx")
                    .Select(filePath => new TemplateInfo
                    {
                        Name = Path.GetFileNameWithoutExtension(filePath),
                        Description = $"Custom template: {Path.GetFileName(filePath)}",
                        TemplateType = "File",
                        Identifier = filePath
                    })
                    .ToList();
                
                templateList.AddRange(templateFiles);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show(
                    $"Could not read templates directory: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }

            TemplatesGrid.ItemsSource = templateList;
        }
        
        #endregion

        #region Event Handlers
        
        /// <summary>
        /// Handles double-click on template grid to open selected template
        /// </summary>
        private void TemplatesGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                var grid = (DevExpress.Xpf.Grid.GridControl)sender;
    
                if (grid.SelectedItem is not TemplateInfo selectedTemplate)
                {
                    return;
                }

                // Log template selection for debugging
                SentrySdk.AddBreadcrumb($"Template selected: {selectedTemplate.Name}", "template");
                SentrySdk.AddBreadcrumb($"Template type: {selectedTemplate.TemplateType}", "template");
        
                // Trigger appropriate event based on template type
                if (selectedTemplate.TemplateType == "BuiltIn")
                {
                    RequestNewFmeaDocument?.Invoke(this, EventArgs.Empty);
                }
                else if (selectedTemplate.TemplateType == "File")
                {
                    RequestNewDocumentFromFile?.Invoke(this, selectedTemplate.Identifier);
                }
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show(
                    $"Failed to open template: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Handles click on blank spreadsheet button
        /// </summary>
        private void BlankSheetButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RequestBlankSpreadsheet?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show(
                    $"Failed to create blank spreadsheet: {ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
            }
        }
        
        /// <summary>
        /// Handles import template button click to add external template files
        /// </summary>
        private async void ImportTemplateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Select Excel Template",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string sourceFile = openFileDialog.FileName;
                string fileName = Path.GetFileName(sourceFile);
                string destFile = Path.Combine(_templateDirectory, fileName);

                // Check if template already exists
                if (File.Exists(destFile))
                {
                    var result = MessageBox.Show(
                        $"A template named '{fileName}' already exists. Do you want to replace it?",
                        "Confirm Replace",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                // Copy template file to templates directory
                await Task.Run(() => File.Copy(sourceFile, destFile, overwrite: true));

                MessageBox.Show(
                    $"Template '{fileName}' imported successfully!",
                    "Success",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // Refresh template list
                LoadTemplates();
                
                SentrySdk.AddBreadcrumb($"Template imported: {fileName}", "template");
            }
            catch (Exception ex)
            {
                SentrySdk.CaptureException(ex);
                MessageBox.Show(
                    $"Failed to import template.\n\nError: {ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        
        #endregion
    }
}