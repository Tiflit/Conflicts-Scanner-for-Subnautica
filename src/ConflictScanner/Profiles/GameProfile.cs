namespace ConflictScanner.Profiles
{
    public abstract class GameProfile
    {
        public abstract string GameName { get; }

        public abstract bool MatchesGame(string gamePath);

        public abstract void RegisterAnalyzers(AnalyzerPipeline pipeline);
    }
}
