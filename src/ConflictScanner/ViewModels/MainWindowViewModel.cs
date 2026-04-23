using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ConflictScanner.Profiles;

namespace ConflictScanner.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _gamePath = string.Empty;

        [ObservableProperty]
        private string _status = "Ready.";

        [ObservableProperty]
        private string _reportText = string.Empty;

        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private bool _deepScan;

        public MainWindowViewModel()
        {
            var saved = GameLocator.LoadSavedPath();
            if (!string.IsNullOrWhiteSpace(saved))
            {
                GamePath = saved;
                Status = "Loaded last game path from settings.";
            }
        }

        [RelayCommand]
        private void DetectGame()
        {
            if (GameLocator.TryAutoLocateSubnautica(out var path) && !string.IsNullOrWhiteSpace(path))
            {
                GamePath = path;
                Status = "Detected Subnautica installation via Steam.";
                GameLocator.SavePath(path);
            }
            else
            {
                Status = "Could not auto-detect Subnautica. Please browse manually.";
            }
        }

        [RelayCommand]
        private async Task RunScanAsync()
        {
            if (IsBusy)
                return;

            if (string.IsNullOrWhiteSpace(GamePath) || !Directory.Exists(GamePath))
            {
                Status = "Please select a valid game path.";
                return;
            }

            IsBusy = true;
            Status = "Scanning...";
            ReportText = string.Empty;

            try
            {
                var profile = ProfileManager.DetectProfile(GamePath);
                if (profile == null)
                {
                    Status = "No supported game detected at this path.";
                    IsBusy = false;
                    return;
                }

                var mode = DeepScan ? ScanMode.Deep : ScanMode.Quick;
                var context = new ScanContext(GamePath, mode);
                var pipeline = new AnalyzerPipeline();
                profile.RegisterAnalyzers(pipeline);

                var start = DateTime.UtcNow;

                await Task.Run(() =>
                {
                    foreach (var analyzer in pipeline.GetAnalyzers())
                    {
                        analyzer.Run(context);
                    }

                    SuggestionEngine.Generate(context);
                    context.ScanDuration = DateTime.UtcNow - start;
                });

                var report = ReportGenerator.Generate(context);
                ReportText = report;
                Status = $"Scan complete in {context.ScanDuration.TotalSeconds:F1} seconds.";

                GameLocator.SavePath(GamePath);
            }
            catch (Exception ex)
            {
                Status = "Scan failed.";
                ReportText = $"An error occurred:\n{ex}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
