using System;
using System.Text;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Turns product names and identifiers into file and folder names that are valid on Windows, macOS and Linux.
    /// </summary>
    internal static class FileNames
    {
        /// <summary>
        /// Name used when nothing valid is left of the original, e.g. for a product named <c>???</c>.
        /// </summary>
        public const string FallbackName = "Player";

        private const string InvalidCharacters = "<>:\"/\\|?*";

        private static readonly string[] ReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        /// <summary>
        /// Removes characters that are invalid in file names on any supported system, trims the trailing dots and spaces
        /// Windows rejects, and avoids reserved Windows device names, e.g. <c>My Game: Deluxe</c> becomes <c>My Game Deluxe</c>.
        /// </summary>
        public static string Sanitize(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return FallbackName;
            }

            StringBuilder builder = new(name.Length);
            bool isPreviousSpace = false;
            foreach (char character in name.Trim())
            {
                if (character < ' ' || InvalidCharacters.IndexOf(character) >= 0)
                {
                    continue;
                }

                // Removing a character can leave two spaces next to each other.
                bool isSpace = character == ' ';
                if (isSpace && isPreviousSpace)
                {
                    continue;
                }

                builder.Append(character);
                isPreviousSpace = isSpace;
            }

            string sanitized = builder.ToString().TrimEnd('.', ' ').Trim();
            if (sanitized.Length == 0)
            {
                return FallbackName;
            }

            return IsReserved(sanitized) ? sanitized + "_" : sanitized;
        }

        private static bool IsReserved(string name)
        {
            int extensionIndex = name.IndexOf('.');
            string stem = extensionIndex < 0 ? name : name.Substring(0, extensionIndex);
            foreach (string reservedName in ReservedNames)
            {
                if (string.Equals(stem, reservedName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
