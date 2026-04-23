using System;
using System.IO;
using System.Text;

namespace ConflictScanner
{
    public static class MimeDetector
    {
        public static string DetectMime(string filePath)
        {
            byte[] header = new byte[16];
            using (var stream = File.OpenRead(filePath))
                stream.Read(header, 0, header.Length);

            // PNG
            if (header.Length >= 8 &&
                header[0] == 0x89 && header[1] == 0x50 &&
                header[2] == 0x4E && header[3] == 0x47)
                return "image/png";

            // JPEG
            if (header[0] == 0xFF && header[1] == 0xD8)
                return "image/jpeg";

            // GIF
            if (Encoding.ASCII.GetString(header, 0, 3) == "GIF")
                return "image/gif";

            // OGG
            if (Encoding.ASCII.GetString(header, 0, 4) == "OggS")
                return "audio/ogg";

            // WAV
            if (Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                Encoding.ASCII.GetString(header, 8, 4) == "WAVE")
                return "audio/wav";

            // ZIP
            if (header[0] == 0x50 && header[1] == 0x4B)
                return "application/zip";

            // UnityFS
            if (Encoding.ASCII.GetString(header, 0, 6) == "UnityF")
                return "application/unityfs";

            // JSON or text
            string textStart = Encoding.UTF8.GetString(header).TrimStart();
            if (textStart.StartsWith("{") || textStart.StartsWith("["))
                return "application/json";

            // Plain text heuristic
            if (IsMostlyText(header))
                return "text/plain";

            return "application/octet-stream";
        }

        private static bool IsMostlyText(byte[] bytes)
        {
            int printable = 0;
            int total = bytes.Length;

            foreach (byte b in bytes)
            {
                if (b == 0) return false;
                if (b >= 32 && b <= 126) printable++;
            }

            return printable > total * 0.7;
        }
    }
}
