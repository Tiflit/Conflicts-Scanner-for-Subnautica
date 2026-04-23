using System;
using System.Collections.Generic;

namespace ConflictScanner
{
    public static class IgnoreList
    {
        public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".md", ".txt", ".rtf", ".license", ".gitkeep", ".gitattributes",
            ".gitignore", ".ini", ".cfg", ".log"
        };

        public static readonly HashSet<string> FileNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "readme", "changelog", "license", "credits"
        };

        public static bool ShouldIgnore(string relativePath)
        {
            string ext = System.IO.Path.GetExtension(relativePath);
            if (Extensions.Contains(ext))
                return true;

            string name = System.IO.Path.GetFileNameWithoutExtension(relativePath);
            if (FileNames.Contains(name))
                return true;

            return false;
        }
    }
}
