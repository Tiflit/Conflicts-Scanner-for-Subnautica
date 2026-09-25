using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
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

        [ObservableProperty]
        private int _findingsCount;

        [ObservableProperty]
        private string _searchFilter = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "All";

        [ObservableProperty]
        private string _selectedMinImpact = "All";

        [ObservableProperty]
        private string _progressMessage = "Scanning...";

        private CancellationTokenSource? _scanCts;

        public ObservableCollection<Finding> Findings { get; } = new();
        public ObservableCollection<Finding> FilteredFindings { get; } = new();

        public IReadOnlyList<string> AvailableCategories { get; } = new[]
        {
            "All", "Metadata", "Dependencies", "Compatibility", "Harmony", "Nautilus", "Filesystem", "Patcher", "SMLHelper", "QMod"
        };

        public IReadOnlyList<string> AvailableImpacts { get; } = new[]
        {
            "All", "Critical only", "High+", "Medium+"
        };

        public MainWindowViewModel()
        {
            var saved = GameLocator.LoadSavedPath();
            if (!string.IsNullOrWhiteSpace(saved))
            {
                GamePath = saved;
                Status = "Loaded last game path from settings.";
            }
        }

        partial void OnSearchFilterChanged(string value) => ApplyFilter();
        partial void OnSelectedCategoryChanged(string value) => ApplyFilter();
        partial void OnSelectedMinImpactChanged(string value) => ApplyFilter();

        [RelayCommand]
        private void DetectGame()
        {
            if (IsBusy)
                return;

            if (GameLocator.TryAutoLocateSubnautica(out var path) && !string.IsNullOrWhiteSpace(path))
            {
                GamePath = path;
                Status = "Detected Subnautica installation.";
                GameLocator.SavePath(path);
            }
            else
            {
                Status = "Could not auto-detect Subnautica. Please browse manually.";
            }
        }

        [RelayCommand]
        private void CancelScan()
        {
            if (_scanCts != null && !_scanCts.IsCancellationRequested)
            {
                _scanCts.Cancel();
                Status = "Cancelling scan...";
                ProgressMessage = "Cancelling...";
            }
        }

        [RelayCommand]
        private void ResetFilters()
        {
            SearchFilter = string.Empty;
            SelectedCategory = "All";
            SelectedMinImpact = "All";
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
            ProgressMessage = "Scanning mod directories...";
            ReportText = string.Empty;
            Findings.Clear();
            FilteredFindings.Clear();
            FindingsCount = 0;

            _scanCts = new CancellationTokenSource();
            var token = _scanCts.Token;

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
                var context = new ScanContext(GamePath, mode, profile.GameName);
                var pipeline = new AnalyzerPipeline();
                profile.RegisterAnalyzers(pipeline);

                var start = DateTime.UtcNow;

                await Task.Run(() =>
                {
                    var analyzers = pipeline.GetAnalyzers().ToList();
                    for (int i = 0; i < analyzers.Count; i++)
                    {
                        token.ThrowIfCancellationRequested();
                        analyzers[i].Run(context);
                    }

                    token.ThrowIfCancellationRequested();
                    SuggestionEngine.Generate(context);
                    context.ScanDuration = DateTime.UtcNow - start;
                }, token);

                foreach (var finding in context.Findings)
                {
                    Findings.Add(finding);
                }
                FindingsCount = Findings.Count;
                ApplyFilter();

                var report = ReportGenerator.Generate(context);
                ReportText = report;
                Status = $"Scan complete in {context.ScanDuration.TotalSeconds:F1}s. Found {FindingsCount} finding(s).";

                GameLocator.SavePath(GamePath);
            }
            catch (OperationCanceledException)
            {
                Status = "Scan cancelled by user.";
            }
            catch (Exception ex)
            {
                Status = "Scan failed.";
                ReportText = $"An error occurred:\n{ex}";
            }
            finally
            {
                IsBusy = false;
                _scanCts?.Dispose();
                _scanCts = null;
            }
        }

        private void ApplyFilter()
        {
            FilteredFindings.Clear();

            foreach (var finding in Findings)
            {
                // 1. Category filter
                if (SelectedCategory != "All" && !finding.Category.Equals(SelectedCategory, StringComparison.OrdinalIgnoreCase))
                    continue;

                // 2. Minimum Impact filter
                if (SelectedMinImpact == "Critical only" && finding.Impact != Impact.Critical)
                    continue;
                if (SelectedMinImpact == "High+" && finding.Impact != Impact.Critical && finding.Impact != Impact.High)
                    continue;
                if (SelectedMinImpact == "Medium+" && finding.Impact != Impact.Critical && finding.Impact != Impact.High && finding.Impact != Impact.Medium)
                    continue;

                // 3. Search text filter
                if (!string.IsNullOrWhiteSpace(SearchFilter))
                {
                    bool match = finding.Explanation.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                 finding.Category.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                 finding.Evidence.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                                 (finding.ResourceKey != null && finding.ResourceKey.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)) ||
                                 finding.InvolvedMods.Any(m => m.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

                    if (!match)
                        continue;
                }

                FilteredFindings.Add(finding);
            }
        }
    }
}
