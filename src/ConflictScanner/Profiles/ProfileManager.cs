using System.Collections.Generic;

namespace ConflictScanner.Profiles
{
    public static class ProfileManager
    {
        private static readonly List<GameProfile> Profiles = new()
        {
            new Subnautica()
        };

        public static GameProfile DetectProfile(string gamePath)
        {
            foreach (var profile in Profiles)
            {
                if (profile.MatchesGame(gamePath))
                    return profile;
            }

            return null;
        }
    }
}
