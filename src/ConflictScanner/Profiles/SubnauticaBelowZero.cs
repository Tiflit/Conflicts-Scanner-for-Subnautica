using System.IO;
using ConflictScanner.Analysis;
using ConflictScanner.Reflection;

namespace ConflictScanner.Profiles
{
    public class SubnauticaBelowZero : GameProfile
    {
        public override string GameName => "Subnautica: Below Zero";

        public override bool MatchesGame(string gamePath)
        {
            string exe = Path.Combine(gamePath, "SubnauticaZero.exe");
            return File.Exists(exe);
        }

        public override void RegisterAnalyzers(AnalyzerPipeline pipeline)
        {
            pipeline.Add(new BepInPluginAnalyzer());
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
