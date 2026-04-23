using System.Collections.Generic;

namespace ConflictScanner
{
    public class AnalyzerPipeline
    {
        private readonly List<object> _analyzers = new();

        public void Add(object analyzer) => _analyzers.Add(analyzer);

        public IEnumerable<object> GetAnalyzers() => _analyzers;
    }
}
