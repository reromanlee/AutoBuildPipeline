using System.Globalization;
using System.IO;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Android builds: App Bundles or APKs named by application identifier, the version code as build number,
    /// and keystore signing.
    /// </summary>
    internal sealed class AndroidPlatform : BuildPlatform
    {
        /// <summary>
        /// Highest version code Google Play accepts.
        /// </summary>
        public const int MaximumVersionCode = 2100000000;

        public const string AppBundleExtension = ".aab";
        public const string PackageExtension = ".apk";

        public AndroidPlatform() : base(BuildTarget.Android, "Android", "Android")
        {
            // Every Android build uses the same target, name and folder.
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
        public override string BuildNumberName
        {
            get
            {
                return "version code";
            }
        }

        /// <inheritdoc/>
        public override int MinimumBuildNumber
        {
            get
            {
                return 1;
            }
        }

        /// <inheritdoc/>
        public override int MaximumBuildNumber
        {
            get
            {
                return MaximumVersionCode;
            }
        }

        /// <inheritdoc/>
        public override bool SupportsAppBundle
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public override string ReadBuildNumber()
        {
            return PlayerSettings.Android.bundleVersionCode.ToString(CultureInfo.InvariantCulture);
        }

        /// <inheritdoc/>
        public override void WriteBuildNumber(string buildNumber)
        {
            if (int.TryParse(buildNumber, NumberStyles.None, CultureInfo.InvariantCulture, out int versionCode) &&
                PlayerSettings.Android.bundleVersionCode != versionCode)
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
            }
        }

        /// <inheritdoc/>
        /// <remarks>Android builds are named by application identifier, e.g. <c>com.company.app</c>.</remarks>
        public override string GetNameBase(string productName, string applicationIdentifier)
        {
            return FileNames.Sanitize(applicationIdentifier);
        }

        /// <inheritdoc/>
        /// <remarks>The package is named like its folder, e.g. <c>com.company.app-1.6.1-7/com.company.app-1.6.1-7.aab</c>.</remarks>
        public override string GetPlayerPath(string buildFolder, string buildName, string nameBase, bool isAppBundle)
        {
            return Path.Combine(buildFolder, buildName + (isAppBundle ? AppBundleExtension : PackageExtension));
        }

        /// <inheritdoc/>
        public override void Capture(BuildSettingsRestorePoint restorePoint)
        {
            restorePoint.BuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            restorePoint.ExportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            restorePoint.UseCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            restorePoint.KeystoreName = PlayerSettings.Android.keystoreName;
            restorePoint.KeyaliasName = PlayerSettings.Android.keyaliasName;
        }

        /// <inheritdoc/>
        public override bool TryPrepare(BuildPlan plan, BuildOverrides overrides)
        {
            // An exported Gradle project would replace the package the plan promises.
            EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
            EditorUserBuildSettings.buildAppBundle = plan.IsAppBundle;
            return AndroidSigning.TryPrepare(plan.Request.IsRelease, overrides);
        }

        /// <inheritdoc/>
        public override void Restore(BuildSettingsRestorePoint restorePoint)
        {
            // Writing only on change avoids dirtying Player Settings and build profiles for nothing.
            if (EditorUserBuildSettings.buildAppBundle != restorePoint.BuildAppBundle)
            {
                EditorUserBuildSettings.buildAppBundle = restorePoint.BuildAppBundle;
            }

            if (EditorUserBuildSettings.exportAsGoogleAndroidProject != restorePoint.ExportAsGoogleAndroidProject)
            {
                EditorUserBuildSettings.exportAsGoogleAndroidProject = restorePoint.ExportAsGoogleAndroidProject;
            }

            if (PlayerSettings.Android.useCustomKeystore != restorePoint.UseCustomKeystore)
            {
                PlayerSettings.Android.useCustomKeystore = restorePoint.UseCustomKeystore;
            }

            if (PlayerSettings.Android.keystoreName != restorePoint.KeystoreName)
            {
                PlayerSettings.Android.keystoreName = restorePoint.KeystoreName;
            }

            if (PlayerSettings.Android.keyaliasName != restorePoint.KeyaliasName)
            {
                PlayerSettings.Android.keyaliasName = restorePoint.KeyaliasName;
            }
        }
    }
}
