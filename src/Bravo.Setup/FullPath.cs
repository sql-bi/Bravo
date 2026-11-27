namespace Bravo.Setup
{
    /// <summary>
    /// Whether a path names a file or a folder whatever the current folder is. .NET Framework has no
    /// <c>Path.IsPathFullyQualified</c>: this is its rule for Windows, which reads characters only and throws for
    /// none.
    /// </summary>
    internal static class FullPath
    {
        public static bool IsFullyQualified(string path)
        {
            if (path == null || path.Length < 2)
            {
                return false;
            }

            // Two separators start a network path or a device path; one names a folder of the current drive.
            if (IsSeparator(path[0]))
            {
                return path[1] == '?' || IsSeparator(path[1]);
            }

            // A drive, a colon and a separator: without the separator, the path is relative to the current folder
            // of the drive.
            return path.Length >= 3 && IsDriveLetter(path[0]) && path[1] == ':' && IsSeparator(path[2]);
        }

        private static bool IsSeparator(char character)
        {
            return character == '\\' || character == '/';
        }

        private static bool IsDriveLetter(char character)
        {
            return (character >= 'A' && character <= 'Z') || (character >= 'a' && character <= 'z');
        }
    }
}
