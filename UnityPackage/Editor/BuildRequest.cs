namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Describes how a build versions the application and what happens once it is built.
    /// Only release builds change the project's version; previews use a temporary preview version.
    /// </summary>
    public readonly struct BuildRequest
    {
        /// <summary>
        /// Version part to increment. Releases save the new version; previews only show it,
        /// e.g. <c>1.6.4-dev+251025.1432</c> for a patch preview of <c>1.6.3</c>.
        /// </summary>
        public readonly VersionIncrement VersionIncrement;

        /// <summary>
        /// Release builds save the new version, increment the build number with the version, are never development builds,
        /// and on Android must be signed with a custom keystore. Previews leave the project untouched.
        /// </summary>
        public readonly bool IsRelease;

        /// <summary>
        /// Launches the build once it is built: installs it on the selected Android device, opens WebGL in the browser,
        /// or starts the Standalone player.
        /// </summary>
        public readonly bool IsRunAfterBuild;

        /// <summary>
        /// Describes a custom build; the static presets cover the Build menu.
        /// </summary>
        /// <param name="versionIncrement">Version part to increment, or <see cref="VersionIncrement.None"/> to keep the version.</param>
        /// <param name="isRelease">Whether the new version is saved and the build is signed and shipped as a release.</param>
        /// <param name="isRunAfterBuild">Whether the build is launched once it is built.</param>
        public BuildRequest(VersionIncrement versionIncrement, bool isRelease, bool isRunAfterBuild)
        {
            VersionIncrement = versionIncrement;
            IsRelease = isRelease;
            IsRunAfterBuild = isRunAfterBuild;
        }

        /// <summary>
        /// Whether the build number (Android version code, macOS build number) is incremented:
        /// for releases that change the version.
        /// </summary>
        public bool IsBuildNumberIncremented
        {
            get
            {
                return IsRelease && VersionIncrement != VersionIncrement.None;
            }
        }

        /// <summary>
        /// Release with a new patch version, e.g. <c>1.6.3 (7)</c> to <c>1.6.4 (8)</c>.
        /// </summary>
        public static BuildRequest PatchRelease
        {
            get
            {
                return new BuildRequest(VersionIncrement.Patch, isRelease: true, isRunAfterBuild: false);
            }
        }

        /// <summary>
        /// Release with a new minor version, e.g. <c>1.6.3 (7)</c> to <c>1.7.0 (8)</c>.
        /// </summary>
        public static BuildRequest MinorRelease
        {
            get
            {
                return new BuildRequest(VersionIncrement.Minor, isRelease: true, isRunAfterBuild: false);
            }
        }

        /// <summary>
        /// Release with a new major version, e.g. <c>1.6.3 (7)</c> to <c>2.0.0 (8)</c>.
        /// </summary>
        public static BuildRequest MajorRelease
        {
            get
            {
                return new BuildRequest(VersionIncrement.Major, isRelease: true, isRunAfterBuild: false);
            }
        }

        /// <summary>
        /// Release of the version currently set in Player Settings, e.g. <c>1.6.3 (7)</c> stays <c>1.6.3 (7)</c>.
        /// Rebuilds a release whose upload failed, ships a version typed in by hand,
        /// or ships the same version on a second platform.
        /// </summary>
        public static BuildRequest ExactRelease
        {
            get
            {
                return new BuildRequest(VersionIncrement.None, isRelease: true, isRunAfterBuild: false);
            }
        }

        /// <summary>
        /// Preview of the upcoming patch, e.g. <c>1.6.3 (7)</c> builds as <c>1.6.4-dev+251025.1432 (7)</c>
        /// while the project stays at <c>1.6.3 (7)</c>.
        /// </summary>
        public static BuildRequest Preview
        {
            get
            {
                return new BuildRequest(VersionIncrement.Patch, isRelease: false, isRunAfterBuild: false);
            }
        }

        /// <summary>
        /// <see cref="Preview"/> that is launched once it is built.
        /// </summary>
        public static BuildRequest PreviewAndRun
        {
            get
            {
                return new BuildRequest(VersionIncrement.Patch, isRelease: false, isRunAfterBuild: true);
            }
        }
    }
}
