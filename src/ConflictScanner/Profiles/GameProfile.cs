namespace ConflictScanner.Profiles
{
    /// <summary>
    /// Base class for game-specific profiles.
    /// Profiles define which analyzers to run and any game-specific rules.
    /// </summary>
    public abstract class GameProfile
    {
        public abstract string GameName { get; }

        /// <summary>
        /// Returns true if the given game path matches this profile.
        /// </summary>
        public abstract bool MatchesGame(string gamePath);

        /// <summary>
        /// Allows profiles to register analyzers in the correct order.
        /// </summary>
        public abstract void RegisterAnalyzers(AnalyzerPipeline pipeline);
    }
}
