using System.Collections.Generic;

namespace ConflictScanner
{
    public class AnalyzerPipeline
    {
        private readonly List<IAnalyzer> _analyzers = new();

        public void Add(IAnalyzer analyzer) => _analyzers.Add(analyzer);

        public IEnumerable<IAnalyzer> GetAnalyzers() => _analyzers;
    }
}
