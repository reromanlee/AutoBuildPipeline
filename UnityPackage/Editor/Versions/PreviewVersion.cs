using System;
using System.Globalization;
using System.Text;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Semantic version of a preview build, e.g. <c>1.6.1-dev+251025.1432.3f2a9c1</c>: the upcoming version,
    /// a pre-release label, the local build time and, when known, the short commit hash.
    /// Every preview is recognizable on the device, and none can be mistaken for a release.
    /// </summary>
    internal static class PreviewVersion
    {
        /// <summary>
        /// Label used when the settings hold none, e.g. <c>1.6.1-dev+251025.1432</c>.
        /// </summary>
        public const string DefaultLabel = "dev";

        /// <summary>
        /// Number of commit hash characters in a preview version.
        /// </summary>
        public const int CommitLength = 7;

        private const string TimestampFormat = "yyMMdd.HHmm";

        /// <summary>
        /// Formats the preview version.
        /// </summary>
        /// <param name="upcoming">Version the preview leads up to.</param>
        /// <param name="label">Pre-release label; must pass <see cref="IsValidLabel"/>.</param>
        /// <param name="timestamp">Build time, written as <c>yyMMdd.HHmm</c>.</param>
        /// <param name="commit">Commit hash, shortened to <see cref="CommitLength"/> characters, or null to leave it out.</param>
        public static string Format(in ApplicationVersion upcoming, string label, DateTime timestamp, string commit)
        {
            StringBuilder builder = new(48);
            builder.Append(upcoming.ToString());
            builder.Append('-');
            builder.Append(label);
            builder.Append('+');
            builder.Append(timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            if (IsValidCommit(commit))
            {
                builder.Append('.');
                builder.Append(commit, 0, Math.Min(commit.Length, CommitLength));
            }

            return builder.ToString();
        }

        /// <summary>
        /// Whether the label is a valid semantic version pre-release, e.g. <c>dev</c>, <c>beta.2</c> or <c>rc-1</c>:
        /// dot-separated identifiers of letters, digits and hyphens, where numeric identifiers have no leading zero.
        /// </summary>
        public static bool IsValidLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return false;
            }

            int identifierStart = 0;
            for (int index = 0; index <= label.Length; index++)
            {
                if (index < label.Length && label[index] != '.')
                {
                    if (!IsIdentifierCharacter(label[index]))
                    {
                        return false;
                    }

                    continue;
                }

                // The end of the label or a dot closes the current identifier.
                if (!IsValidIdentifier(label, identifierStart, index - identifierStart))
                {
                    return false;
                }

                identifierStart = index + 1;
            }

            return true;
        }

        private static bool IsValidIdentifier(string label, int start, int length)
        {
            if (length == 0)
            {
                return false;
            }

            // Semantic versioning forbids leading zeros in numeric pre-release identifiers, e.g. "01".
            bool isNumeric = true;
            for (int index = start; index < start + length; index++)
            {
                if (label[index] < '0' || label[index] > '9')
                {
                    isNumeric = false;
                    break;
                }
            }

            return !isNumeric || length == 1 || label[start] != '0';
        }

        private static bool IsIdentifierCharacter(char character)
        {
            return (character >= '0' && character <= '9') ||
                   (character >= 'a' && character <= 'z') ||
                   (character >= 'A' && character <= 'Z') ||
                   character == '-';
        }

        private static bool IsValidCommit(string commit)
        {
            if (string.IsNullOrEmpty(commit))
            {
                return false;
            }

            foreach (char character in commit)
            {
                bool isHexadecimal = (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f');
                if (!isHexadecimal)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
