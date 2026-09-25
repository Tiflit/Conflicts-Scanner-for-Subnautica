using System;
using System.IO;
using ConflictScanner.Profiles;
using Xunit;

namespace ConflictScanner.Tests
{
    public class ProfileManagerTests : IDisposable
    {
        private readonly string _tempDir;

        public ProfileManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_ProfileTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                try { Directory.Delete(_tempDir, true); } catch { }
            }
        }

        [Fact]
        public void DetectProfile_SubnauticaExe_DetectsSubnautica()
        {
            string exe = Path.Combine(_tempDir, "Subnautica.exe");
            File.WriteAllBytes(exe, Array.Empty<byte>());

            var profile = ProfileManager.DetectProfile(_tempDir);
            Assert.NotNull(profile);
            Assert.Equal("Subnautica", profile.GameName);
        }

        [Fact]
        public void DetectProfile_SubnauticaZeroExe_DetectsBelowZero()
        {
            string exe = Path.Combine(_tempDir, "SubnauticaZero.exe");
            File.WriteAllBytes(exe, Array.Empty<byte>());

            var profile = ProfileManager.DetectProfile(_tempDir);
            Assert.NotNull(profile);
            Assert.Equal("Subnautica: Below Zero", profile.GameName);
        }

        [Fact]
        public void DetectProfile_NoExecutable_ReturnsNull()
        {
            var profile = ProfileManager.DetectProfile(_tempDir);
            Assert.Null(profile);
        }
    }
}
