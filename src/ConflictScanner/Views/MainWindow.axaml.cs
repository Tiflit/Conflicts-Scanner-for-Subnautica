using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
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

            var dialog = new OpenFolderDialog
            {
                Title = "Select Subnautica installation folder"
            };

            var result = await dialog.ShowAsync(this);
            if (!string.IsNullOrWhiteSpace(result) && Directory.Exists(result))
            {
                vm.GamePath = result;
                vm.Status = "Game path updated.";
            }
        }

        private async void OnSaveReportClick(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not MainWindowViewModel vm)
                return;

            if (string.IsNullOrWhiteSpace(vm.ReportText))
            {
                vm.Status = "No report to save.";
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Save scan report",
                InitialFileName = "ConflictScannerReport.txt"
            };

            dialog.Filters.Add(new FileDialogFilter
            {
                Name = "Text files",
                Extensions = { "txt" }
            });

            var path = await dialog.ShowAsync(this);
            if (!string.IsNullOrWhiteSpace(path))
            {
                await File.WriteAllTextAsync(path, vm.ReportText);
                vm.Status = $"Report saved to {path}.";
            }
        }
    }
}
