using System.Collections.Generic;

namespace ConflictScanner
{
    /// <summary>
    /// Holds analyzers in the order they should run.
    /// Profiles populate this.
    /// </summary>
    public class AnalyzerPipeline
    {
        private readonly List<object> _analyzers = new();

        public void Add(object analyzer) => _analyzers.Add(analyzer);

        public IEnumerable<object> GetAnalyzers() => _analyzers;
    }
}
