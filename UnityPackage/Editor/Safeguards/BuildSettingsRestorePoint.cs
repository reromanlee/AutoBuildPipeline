using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Snapshot of every setting a build changes temporarily. The snapshot is also written to
    /// <c>Library/AutoBuildPipeline/RestorePoint.json</c>, so the settings are restored on the next editor start
    /// if the editor crashes or is killed in the middle of a build.
    /// </summary>
    [Serializable]
    internal sealed class BuildSettingsRestorePoint
    {
        private const string FolderName = "AutoBuildPipeline";
        private const string FileName = "RestorePoint.json";

        // Public fields are required by JsonUtility.
        public string Target;
        public string BundleVersion;
        public string BuildNumber;
        public bool Development;
        public bool BuildAppBundle;
        public bool ExportAsGoogleAndroidProject;
        public bool UseCustomKeystore;
        public string KeystoreName;
        public string KeyaliasName;

        /// <summary>
        /// Captures the current settings of the platform and saves them to disk before anything is changed.
        /// </summary>
        public static BuildSettingsRestorePoint Create(BuildPlatform platform)
        {
            BuildSettingsRestorePoint restorePoint = new()
            {
                Target = platform.Target.ToString(),
                BundleVersion = PlayerSettings.bundleVersion,
                BuildNumber = platform.ReadBuildNumber(),
                Development = EditorUserBuildSettings.development
            };

            platform.Capture(restorePoint);

            try
            {
                string filePath = GetFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                File.WriteAllText(filePath, JsonUtility.ToJson(restorePoint, true));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                // The build still works; only crash recovery is unavailable.
                BuildLog.Warning($"Could not save the build restore point: {exception.Message}");
            }

            return restorePoint;
        }

        /// <summary>
        /// Puts the captured settings back and removes the snapshot from disk.
        /// </summary>
        /// <param name="isVersionKept">Keeps the version and build number the build used, e.g. after a successful release.</param>
        public void Restore(bool isVersionKept)
        {
            // Writing only on change avoids dirtying Player Settings and build profiles for nothing.
            if (!isVersionKept && PlayerSettings.bundleVersion != BundleVersion)
            {
                PlayerSettings.bundleVersion = BundleVersion;
            }

            if (EditorUserBuildSettings.development != Development)
            {
                EditorUserBuildSettings.development = Development;
            }

            if (BuildPlatforms.TryGet(Target, out BuildPlatform platform))
            {
                if (!isVersionKept && BuildNumber != null)
                {
                    platform.WriteBuildNumber(BuildNumber);
                }

                platform.Restore(this);
            }

            try
            {
                File.Delete(GetFilePath());
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                BuildLog.Warning($"Could not delete the build restore point: {exception.Message}");
            }
        }

        [InitializeOnLoadMethod]
        private static void RegisterRecovery()
        {
            // Deferred until the editor finished loading, so Player Settings are ready to be written.
            EditorApplication.delayCall += RecoverInterruptedBuild;
        }

        private static void RecoverInterruptedBuild()
        {
            string filePath = GetFilePath();
            if (BuildPipeline.isBuildingPlayer || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                BuildSettingsRestorePoint restorePoint = JsonUtility.FromJson<BuildSettingsRestorePoint>(File.ReadAllText(filePath));
                if (restorePoint == null)
                {
                    File.Delete(filePath);
                    return;
                }

                restorePoint.Restore(isVersionKept: false);
                AssetDatabase.SaveAssets();
                BuildLog.Warning($"The previous build was interrupted; its temporary settings were reverted (version {restorePoint.BundleVersion}).");
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException || exception is ArgumentException)
            {
                BuildLog.Warning($"Could not recover settings from '{filePath}': {exception.Message}");
            }
        }

        private static string GetFilePath()
        {
            return Path.Combine(ProjectPaths.ProjectFolder, "Library", FolderName, FileName);
        }
    }
}
