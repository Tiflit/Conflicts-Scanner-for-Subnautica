using System.IO;

namespace ConflictScanner.Profiles
{
    public class SubnauticaProfile : GameProfile
    {
        public override string GameName => "Subnautica";

        public override bool MatchesGame(string gamePath)
        {
            // Simple heuristic: Subnautica.exe exists
            string exe = Path.Combine(gamePath, "Subnautica.exe");
            return File.Exists(exe);
        }

        public override void RegisterAnalyzers(AnalyzerPipeline pipeline)
        {
            pipeline.Add(new HarmonyAnalyzer());
            pipeline.Add(new NautilusAnalyzer());
            pipeline.Add(new QModAnalyzer());
            pipeline.Add(new FileOverrideAnalyzer());
        }
    }
}
