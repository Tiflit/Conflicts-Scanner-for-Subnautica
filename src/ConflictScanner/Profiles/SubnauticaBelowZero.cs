using System.IO;
using ConflictScanner.Analysis;

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
        }
    }
}
