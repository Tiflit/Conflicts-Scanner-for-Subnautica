using System.IO;

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
            pipeline.Add(new HarmonyAnalyzer());
            pipeline.Add(new NautilusAnalyzer());
            pipeline.Add(new QModAnalyzer());
            pipeline.Add(new FileOverrideAnalyzer());
            
            // Deep Scan reflection analyzers
            pipeline.Add(new ConflictScanner.Reflection.NautilusReflectionAnalyzer());
        }
    }
}
