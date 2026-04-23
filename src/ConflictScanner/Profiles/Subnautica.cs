using System.IO;
using ConflictScanner.Reflection;

namespace ConflictScanner.Profiles
{
    public class Subnautica : GameProfile
    {
        public override string GameName => "Subnautica";

        public override bool MatchesGame(string gamePath)
        {
            string exe = Path.Combine(gamePath, "Subnautica.exe");
            return File.Exists(exe);
        }

        public override void RegisterAnalyzers(AnalyzerPipeline pipeline)
        {
            pipeline.Add(new SMLHelperAnalyzer());
            pipeline.Add(new HarmonyAnalyzer());
            pipeline.Add(new NautilusAnalyzer());
            pipeline.Add(new QModAnalyzer());
            pipeline.Add(new FileOverrideAnalyzer());
            pipeline.Add(new PatcherAnalyzer());

            // Deep Scan reflection analyzers
            pipeline.Add(new NautilusReflectionAnalyzer());
            pipeline.Add(new HarmonyReflectionAnalyzer());
        }
    }
}
