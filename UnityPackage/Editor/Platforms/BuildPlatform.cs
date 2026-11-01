using System.IO;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// What differs between build targets: folder names, the player file, the build number,
    /// and settings that must change temporarily for a build.
    /// </summary>
    internal abstract class BuildPlatform
    {
        protected BuildPlatform(BuildTarget target, string displayName, string folderName)
        {
            Target = target;
            DisplayName = displayName;
            FolderName = folderName;
        }

        /// <summary>
        /// Build target this platform builds.
        /// </summary>
        public BuildTarget Target { get; }

        /// <summary>
        /// Name shown in messages, e.g. <c>Android</c> or <c>Windows</c>.
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// Subfolder of the output folder that receives this platform's builds, e.g. <c>Builds/Windows</c>.
        /// </summary>
        public string FolderName { get; }

        /// <summary>
        /// Whether the platform has a build number that releases increment, e.g. the Android version code.
        /// </summary>
        public virtual bool HasBuildNumber
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Name of the build number in messages, e.g. <c>version code</c>.
        /// </summary>
        public virtual string BuildNumberName
        {
            get
            {
                return "build number";
            }
        }

        /// <summary>
        /// Lowest build number the platform's stores accept.
        /// </summary>
        public virtual int MinimumBuildNumber
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Highest build number the platform's stores accept.
        /// </summary>
        public virtual int MaximumBuildNumber
        {
            get
            {
                return int.MaxValue;
            }
        }

        /// <summary>
        /// Whether the platform can package releases as an Android App Bundle.
        /// </summary>
        public virtual bool SupportsAppBundle
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Build number as stored in Player Settings, or null when the platform has none.
        /// </summary>
        public virtual string ReadBuildNumber()
        {
            return null;
        }

        /// <summary>
        /// Writes the build number into Player Settings; does nothing when the platform has none.
        /// </summary>
        public virtual void WriteBuildNumber(string buildNumber)
        {
            // Platforms without a build number have nothing to write.
        }

        /// <summary>
        /// Base of the build's folder and file names, e.g. the sanitized product name <c>My Game</c>.
        /// </summary>
        public virtual string GetNameBase(string productName, string applicationIdentifier)
        {
            return FileNames.Sanitize(productName);
        }

        /// <summary>
        /// Player location inside the build folder, e.g. <c>Builds/Windows/My Game-1.6.0/My Game.exe</c>.
        /// </summary>
        /// <param name="buildFolder">Folder that holds this build and its build-info.json.</param>
        /// <param name="buildName">Name of the build folder, e.g. <c>My Game-1.6.0</c>.</param>
        /// <param name="nameBase">Result of <see cref="GetNameBase"/>.</param>
        /// <param name="isAppBundle">Whether an Android App Bundle is built.</param>
        public abstract string GetPlayerPath(string buildFolder, string buildName, string nameBase, bool isAppBundle);

        /// <summary>
        /// Captures platform settings the build may change, before anything changes.
        /// </summary>
        public virtual void Capture(BuildSettingsRestorePoint restorePoint)
        {
            // Platforms without extra settings capture nothing.
        }

        /// <summary>
        /// Applies platform settings for the build. Runs inside the restore point, after the version is resolved.
        /// </summary>
        /// <returns>False when the user cancelled, e.g. a password prompt.</returns>
        public virtual bool TryPrepare(BuildPlan plan, BuildOverrides overrides)
        {
            return true;
        }

        /// <summary>
        /// Puts back the settings captured by <see cref="Capture"/>.
        /// </summary>
        public virtual void Restore(BuildSettingsRestorePoint restorePoint)
        {
            // Platforms without extra settings restore nothing.
        }

        /// <summary>
        /// Player file named after the product inside the build folder, e.g. <c>My Game.exe</c>.
        /// </summary>
        protected static string GetPlayerFilePath(string buildFolder, string nameBase, string extension)
        {
            return Path.Combine(buildFolder, nameBase + extension);
        }
    }
}
