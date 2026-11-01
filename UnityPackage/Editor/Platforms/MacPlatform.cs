using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// macOS builds: the application bundle named after the product, and the bundle build number
    /// (<c>CFBundleVersion</c>) as build number, e.g. <c>Builds/macOS/My Game-1.6.0-7/My Game.app</c>.
    /// </summary>
    internal sealed class MacPlatform : StandalonePlatform
    {
        public MacPlatform() : base(BuildTarget.StandaloneOSX, "macOS", "macOS", ".app")
        {
            // Every macOS build uses the same target, name and folder.
        }

        /// <inheritdoc/>
        public override bool HasBuildNumber
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public override string ReadBuildNumber()
        {
            return PlayerSettings.macOS.buildNumber;
        }

        /// <inheritdoc/>
        public override void WriteBuildNumber(string buildNumber)
        {
            if (PlayerSettings.macOS.buildNumber != buildNumber)
            {
                PlayerSettings.macOS.buildNumber = buildNumber;
            }
        }
    }
}
