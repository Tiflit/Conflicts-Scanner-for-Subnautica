using System;
using System.IO;
using System.Text;
using ConflictScanner;
using Xunit;

namespace ConflictScanner.Tests
{
    public class MimeDetectorTests : IDisposable
    {
        private readonly string _tempDir;

        public MimeDetectorTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "ConflictScanner_Tests_" + Guid.NewGuid().ToString("N"));
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
        public void DetectMime_EmptyFile_ReturnsOctetStream()
        {
            string file = Path.Combine(_tempDir, "empty.bin");
            File.WriteAllBytes(file, Array.Empty<byte>());

            string mime = MimeDetector.DetectMime(file);
            Assert.Equal("application/octet-stream", mime);
        }

        [Fact]
        public void DetectMime_ShortTextFile_ReturnsTextPlain()
        {
            // Specifically testing files under 16 bytes to prevent buffer underread bug
            string file = Path.Combine(_tempDir, "short.txt");
            File.WriteAllText(file, "Hello!");

            string mime = MimeDetector.DetectMime(file);
            Assert.Equal("text/plain", mime);
        }

        [Fact]
        public void DetectMime_PngHeader_ReturnsImagePng()
        {
            string file = Path.Combine(_tempDir, "image.png");
            byte[] pngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            File.WriteAllBytes(file, pngHeader);

            string mime = MimeDetector.DetectMime(file);
            Assert.Equal("image/png", mime);
        }

        [Fact]
        public void DetectMime_JsonFile_ReturnsApplicationJson()
        {
            string file = Path.Combine(_tempDir, "data.json");
            File.WriteAllText(file, "{\"name\": \"TestMod\"}");

            string mime = MimeDetector.DetectMime(file);
            Assert.Equal("application/json", mime);
        }
    }
}
