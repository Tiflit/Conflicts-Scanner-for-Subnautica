using System;
using System.IO;
using System.Text;

namespace ConflictScanner
{
    public static class MimeDetector
    {
        public static string DetectMime(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return "application/octet-stream";

                byte[] header = new byte[16];
                int bytesRead;

                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bytesRead = stream.Read(header, 0, header.Length);
                }

                if (bytesRead == 0)
                    return "application/octet-stream";

                var span = header.AsSpan(0, bytesRead);

                // PNG (8 bytes: 89 50 4E 47 0D 0A 1A 0A)
                if (bytesRead >= 8 &&
                    span[0] == 0x89 && span[1] == 0x50 &&
                    span[2] == 0x4E && span[3] == 0x47)
                    return "image/png";

                // JPEG (2 bytes: FF D8)
                if (bytesRead >= 2 && span[0] == 0xFF && span[1] == 0xD8)
                    return "image/jpeg";

                // GIF (3 bytes: GIF)
                if (bytesRead >= 3 && Encoding.ASCII.GetString(header, 0, 3) == "GIF")
                    return "image/gif";

                // OGG (4 bytes: OggS)
                if (bytesRead >= 4 && Encoding.ASCII.GetString(header, 0, 4) == "OggS")
                    return "audio/ogg";

                // WAV (RIFF....WAVE)
                if (bytesRead >= 12 &&
                    Encoding.ASCII.GetString(header, 0, 4) == "RIFF" &&
                    Encoding.ASCII.GetString(header, 8, 4) == "WAVE")
                    return "audio/wav";

                // ZIP (PK)
                if (bytesRead >= 2 && span[0] == 0x50 && span[1] == 0x4B)
                    return "application/zip";

                // UnityFS
                if (bytesRead >= 6 && Encoding.ASCII.GetString(header, 0, 6) == "UnityF")
                    return "application/unityfs";

                // JSON or text
                string textStart = Encoding.UTF8.GetString(header, 0, bytesRead).TrimStart();
                if (textStart.StartsWith("{") || textStart.StartsWith("["))
                    return "application/json";

                // Plain text heuristic
                if (IsMostlyText(span))
                    return "text/plain";

                return "application/octet-stream";
            }
            catch
            {
                return "application/octet-stream";
            }
        }

        private static bool IsMostlyText(ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
                return false;

            int printable = 0;
            foreach (byte b in bytes)
            {
                if (b == 0)
                    return false;

                // Printable ASCII (32-126) or common whitespace (Tab: 9, LF: 10, CR: 13)
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                    printable++;
            }

            return (double)printable / bytes.Length >= 0.7;
        }
    }
}
