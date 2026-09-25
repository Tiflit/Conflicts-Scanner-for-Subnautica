using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using ConflictScanner.ViewModels;

namespace ConflictScanner.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void OnBrowseClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Subnautica installation folder",
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                var path = folders[0].TryGetLocalPath();
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    vm.GamePath = path;
                    vm.Status = "Game path updated.";
                }
            }
        }

        private async void OnSaveReportClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (string.IsNullOrWhiteSpace(vm.ReportText) && vm.Findings.Count == 0)
            {
                vm.Status = "No report to save.";
                return;
            }

            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save scan report",
                SuggestedFileName = "ConflictScannerReport.txt",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("Text files (*.txt)")
                    {
                        Patterns = new[] { "*.txt" }
                    },
                    new FilePickerFileType("JSON files (*.json)")
                    {
                        Patterns = new[] { "*.json" }
                    }
                }
            });

            if (file is not null)
            {
                string path = file.Path.LocalPath;
                await using var stream = await file.OpenWriteAsync();

                if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    await JsonSerializer.SerializeAsync(stream, vm.Findings, options);
                    vm.Status = $"JSON report saved ({vm.Findings.Count} findings).";
                }
                else
                {
                    await using var writer = new StreamWriter(stream);
                    await writer.WriteAsync(vm.ReportText);
                    vm.Status = "Text report saved.";
                }
            }
        }
    }
}
