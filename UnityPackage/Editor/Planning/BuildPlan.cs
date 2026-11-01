namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Everything a build was resolved to before any setting changes: version, build number, packaging and paths.
    /// </summary>
    internal sealed class BuildPlan
    {
        public BuildPlatform Platform;
        public BuildRequest Request;

        /// <summary>
        /// Version in Player Settings before the build, e.g. <c>1.6.3</c>.
        /// </summary>
        public string CurrentVersion;

        /// <summary>
        /// Version the build is released as, or the upcoming version a preview leads up to.
        /// </summary>
        public ApplicationVersion Version;

        /// <summary>
        /// Version written into Player Settings for the build, e.g. <c>1.6.4</c> or <c>1.6.4-dev+251025.1432</c>.
        /// </summary>
        public string VersionName;

        /// <summary>
        /// Build number in Player Settings before the build, or null when the platform has none.
        /// </summary>
        public string CurrentBuildNumber;

        /// <summary>
        /// Whether the build writes <see cref="BuildNumber"/>; false for previews, which keep the current build number.
        /// </summary>
        public bool HasBuildNumber;

        /// <summary>
        /// Build number of a release; meaningful only when <see cref="HasBuildNumber"/> is true.
        /// </summary>
        public int BuildNumber;

        public bool IsDevelopment;
        public bool IsAppBundle;

        /// <summary>
        /// Folder the package owns for this build, which is replaced as a whole, or null for an exact player location.
        /// </summary>
        public string BuildFolder;

        /// <summary>
        /// Path passed to Unity as the player location: a file, or a folder for WebGL.
        /// </summary>
        public string PlayerLocation;

        /// <summary>
        /// Full commit hash, or null when unknown.
        /// </summary>
        public string Commit;

        public string ProductName;
        public string ApplicationIdentifier;

        /// <summary>
        /// Version and build number for messages, e.g. <c>1.6.4 (8)</c>.
        /// </summary>
        public string DisplayVersion
        {
            get
            {
                return HasBuildNumber ? $"{VersionName} ({BuildNumber})" : VersionName;
            }
        }

        /// <summary>
        /// Version and build number before the build for messages, e.g. <c>1.6.3 (7)</c>.
        /// </summary>
        public string DisplayCurrentVersion
        {
            get
            {
                return HasBuildNumber ? $"{CurrentVersion} ({CurrentBuildNumber})" : CurrentVersion;
            }
        }
    }
}
