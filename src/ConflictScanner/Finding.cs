using System;
using System.Collections.Generic;

namespace ConflictScanner
{
    public enum Impact
    {
        Info,
        Low,
        Medium,
        High,
        Critical
    }

    public enum Confidence
    {
        Heuristic,
        Probable,
        Observed
    }

    public class Finding
    {
        public string FindingId { get; init; } = Guid.NewGuid().ToString("N")[..8];
        public string Category { get; init; } = string.Empty;
        public Impact Impact { get; init; }
        public Confidence Confidence { get; init; }
        public IReadOnlyList<string> InvolvedMods { get; init; } = Array.Empty<string>();
        public string? ResourceKey { get; init; }
        public string Evidence { get; init; } = string.Empty;
        public string Explanation { get; init; } = string.Empty;
        public string? SuggestedAction { get; init; }

        public override string ToString()
        {
            var mods = InvolvedMods.Count > 0 ? $" [{string.Join(", ", InvolvedMods)}]" : string.Empty;
            return $"[{Impact} | {Confidence}] ({Category}){mods} {Explanation}";
        }
    }
}
