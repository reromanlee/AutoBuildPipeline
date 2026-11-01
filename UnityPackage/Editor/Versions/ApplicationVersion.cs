using System;
using System.Globalization;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Immutable <c>major.minor.patch</c> version, e.g. <c>1.6.0</c>.
    /// </summary>
    public readonly struct ApplicationVersion : IEquatable<ApplicationVersion>
    {
        private const char Separator = '.';
        private const int MaximumPartCount = 3;

        /// <summary>
        /// Major part, incremented for incompatible or milestone releases.
        /// </summary>
        public readonly int Major;

        /// <summary>
        /// Minor part, incremented for releases with new features.
        /// </summary>
        public readonly int Minor;

        /// <summary>
        /// Patch part, incremented for releases with fixes only.
        /// </summary>
        public readonly int Patch;

        /// <summary>
        /// Creates the version <c>major.minor.patch</c>.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">A part is negative.</exception>
        public ApplicationVersion(int major, int minor, int patch)
        {
            if (major < 0 || minor < 0 || patch < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(major), "Version parts must not be negative.");
            }

            Major = major;
            Minor = minor;
            Patch = patch;
        }

        /// <summary>
        /// Returns a new version with the given part incremented and every lower part reset to zero.
        /// </summary>
        /// <exception cref="OverflowException">The incremented part exceeds <see cref="int.MaxValue"/>.</exception>
        public ApplicationVersion Increment(VersionIncrement increment)
        {
            switch (increment)
            {
                case VersionIncrement.None:
                {
                    return this;
                }
                case VersionIncrement.Patch:
                {
                    return new ApplicationVersion(Major, Minor, checked(Patch + 1));
                }
                case VersionIncrement.Minor:
                {
                    return new ApplicationVersion(Major, checked(Minor + 1), 0);
                }
                case VersionIncrement.Major:
                {
                    return new ApplicationVersion(checked(Major + 1), 0, 0);
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(increment), increment, null);
                }
            }
        }

        /// <summary>
        /// Parses <c>major[.minor[.patch]]</c> with an optional <c>v</c> prefix, e.g. <c>1.6</c> or <c>v1.6.0</c>.
        /// Missing parts are treated as zero; pre-release and build suffixes such as <c>-dev</c> are rejected.
        /// </summary>
        public static bool TryParse(string text, out ApplicationVersion version)
        {
            version = default;
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            string trimmed = text.Trim();
            if (trimmed[0] == 'v' || trimmed[0] == 'V')
            {
                trimmed = trimmed.Substring(1);
            }

            string[] parts = trimmed.Split(Separator);
            if (parts.Length > MaximumPartCount)
            {
                return false;
            }

            int major = 0;
            int minor = 0;
            int patch = 0;
            if (!TryParsePart(parts, 0, ref major) ||
                !TryParsePart(parts, 1, ref minor) ||
                !TryParsePart(parts, 2, ref patch))
            {
                return false;
            }

            version = new ApplicationVersion(major, minor, patch);
            return true;
        }

        /// <summary>
        /// Parses <c>major[.minor[.patch]]</c> or throws a <see cref="FormatException"/> describing the expected format.
        /// </summary>
        /// <exception cref="FormatException">The text is not a version in the expected format.</exception>
        public static ApplicationVersion Parse(string text)
        {
            if (!TryParse(text, out ApplicationVersion version))
            {
                throw new FormatException($"Version '{text}' is not in the 'major.minor.patch' format, e.g. 1.6.0.");
            }

            return version;
        }

        /// <summary>
        /// Formats the version as <c>major.minor.patch</c>, e.g. <c>1.6.0</c>.
        /// </summary>
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch);
        }

        /// <summary>
        /// Whether all three parts are equal.
        /// </summary>
        public bool Equals(ApplicationVersion other)
        {
            return Major == other.Major && Minor == other.Minor && Patch == other.Patch;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            return other is ApplicationVersion version && Equals(version);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                return (((Major * 397) ^ Minor) * 397) ^ Patch;
            }
        }

        /// <summary>
        /// Whether all three parts are equal.
        /// </summary>
        public static bool operator ==(ApplicationVersion left, ApplicationVersion right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Whether any part differs.
        /// </summary>
        public static bool operator !=(ApplicationVersion left, ApplicationVersion right)
        {
            return !left.Equals(right);
        }

        private static bool TryParsePart(string[] parts, int index, ref int value)
        {
            if (index >= parts.Length)
            {
                return true;
            }

            return int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out value);
        }
    }
}
