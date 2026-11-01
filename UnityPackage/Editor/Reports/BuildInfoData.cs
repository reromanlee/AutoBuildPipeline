using System;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Contents of <c>build-info.json</c>, written next to every build so it can be identified long after it was made.
    /// </summary>
    [Serializable]
    internal sealed class BuildInfoData
    {
        // Public fields are required by JsonUtility.
        public string Product;
        public string Identifier;
        public string Platform;
        public string Version;

        /// <summary>
        /// Android version code or macOS build number; 0 for platforms and previews without one.
        /// </summary>
        public int BuildNumber;

        public bool IsRelease;
        public bool IsDevelopment;

        /// <summary>
        /// Full commit hash; empty when the project is not in a git repository.
        /// </summary>
        public string Commit;

        public string UnityVersion;

        /// <summary>
        /// Build completion time in UTC, ISO 8601, e.g. <c>2026-11-01T14:32:05Z</c>.
        /// </summary>
        public string CreatedUtc;

        /// <summary>
        /// Player file or folder name next to this file, e.g. <c>My Game.exe</c>.
        /// </summary>
        public string Player;

        public long SizeInBytes;
    }
}
