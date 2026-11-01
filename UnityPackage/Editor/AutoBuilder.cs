using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// One-click builds of the active platform that behave like the Build and Build And Run buttons of Unity's build window,
    /// without the save dialog, and with automatic versioning, Android signing and a <c>build-info.json</c> per build.
    /// </summary>
    /// <example>
    /// <code>
    /// [MenuItem("Tools/Ship Patch")]
    /// private static void ShipPatch()
    /// {
    ///     AutoBuilder.Build(BuildRequest.PatchRelease);
    /// }
    /// </code>
    /// </example>
    public static class AutoBuilder
    {
#if UNITY_6000_0_OR_NEWER
        internal const string BuildWindowName = "Build Profiles";
#else
        internal const string BuildWindowName = "Build Settings";
#endif

        /// <summary>
        /// Versions, signs and builds the active platform. Every setting the build changes is restored afterwards,
        /// except the new version of a successful release, which is saved.
        /// </summary>
        /// <returns>The build report, or null when the user cancelled before the build started.</returns>
        /// <exception cref="BuildFailedException">The build could not start or did not succeed. Batch mode exits with a non-zero code.</exception>
        public static BuildReport Build(in BuildRequest request)
        {
            return Build(request, new BuildOverrides());
        }

        /// <inheritdoc cref="Build(in BuildRequest)"/>
        /// <param name="overrides">Values that replace project settings for this build, e.g. from the command line.</param>
        internal static BuildReport Build(in BuildRequest request, BuildOverrides overrides)
        {
            BuildPlatform platform = EnsureReadyToBuild();
            bool isUnattended = overrides.IsUnattended || Application.isBatchMode;
            BuildPlan plan = BuildPlanner.Create(platform, request, overrides, BuildInputs.Capture(platform));

            if (!isUnattended && request.IsRelease && AutoBuildPipelineSettings.instance.IsReleaseConfirmationEnabled && !ConfirmRelease(plan))
            {
                return null;
            }

            // Same as the Build button: unsaved scene changes would otherwise be silently left out of the build.
            if (!isUnattended && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return null;
            }

            BuildFolderBackup backup = plan.BuildFolder != null ? BuildFolderBackup.Create(plan.BuildFolder) : default;
            BuildSettingsRestorePoint restorePoint = BuildSettingsRestorePoint.Create(platform);
            BuildReport report = null;
            bool isSucceeded = false;
            try
            {
                CreateParentFolder(plan.PlayerLocation);
                if (!platform.TryPrepare(plan, overrides))
                {
                    BuildLog.Info("Build cancelled.");
                    return null;
                }

                PlayerSettings.bundleVersion = plan.VersionName;
                if (plan.HasBuildNumber)
                {
                    platform.WriteBuildNumber(plan.BuildNumber.ToString(CultureInfo.InvariantCulture));
                }

                EditorUserBuildSettings.development = plan.IsDevelopment;

                report = RunBuildPipeline(plan, isUnattended);
                isSucceeded = IsSucceeded(report, plan);
            }
            finally
            {
                restorePoint.Restore(isVersionKept: isSucceeded && request.IsRelease);
                AssetDatabase.SaveAssets();
                backup.Complete(isSucceeded);
            }

            if (report.summary.result == BuildResult.Cancelled)
            {
                BuildLog.Info("Build cancelled.");
                return report;
            }

            if (!isSucceeded)
            {
                throw new BuildFailedException(GetFailureMessage(report, plan));
            }

            string buildInfoPath = BuildInfoFile.Write(BuildInfoFile.Create(plan, DateTime.UtcNow, (long)report.summary.totalSize), plan.PlayerLocation);
            GitHubActionsOutput.Write(plan, buildInfoPath);
            LogSuccess(report, plan);
            return report;
        }

        /// <exception cref="BuildFailedException">The editor is busy or the active platform is not supported.</exception>
        private static BuildPlatform EnsureReadyToBuild()
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                throw new BuildFailedException("Another build is already in progress.");
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new BuildFailedException("Exit Play Mode before building.");
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                throw new BuildFailedException("Wait until scripts finish compiling and assets finish importing, then build again.");
            }

            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (!BuildPlatforms.TryGet(target, out BuildPlatform platform))
            {
                throw new BuildFailedException($"The active platform {target} is not supported. Switch to {BuildPlatforms.SupportedNames} in the {BuildWindowName} window, or pass -buildTarget on the command line.");
            }

            return platform;
        }

        private static bool ConfirmRelease(BuildPlan plan)
        {
            string message = plan.Request.VersionIncrement == VersionIncrement.None
                ? $"{plan.Platform.DisplayName} release {plan.DisplayVersion}?\n\nThe version stays unchanged."
                : $"{plan.Platform.DisplayName} release {plan.DisplayCurrentVersion} → {plan.DisplayVersion}?\n\nThe new version is saved when the build succeeds.";

            return EditorUtility.DisplayDialog("Auto Build Pipeline", message, "Build", "Cancel");
        }

        /// <exception cref="BuildFailedException">The folder cannot be created.</exception>
        private static void CreateParentFolder(string playerLocation)
        {
            string parentFolder = Path.GetDirectoryName(playerLocation);
            if (string.IsNullOrEmpty(parentFolder))
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(parentFolder);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new BuildFailedException($"Could not create the build folder '{parentFolder}': {exception.Message}");
            }
        }

        private static BuildReport RunBuildPipeline(BuildPlan plan, bool isUnattended)
        {
            BuildOptions options = BuildOptions.None;
            if (plan.Request.IsRunAfterBuild)
            {
                options |= BuildOptions.AutoRunPlayer;
            }
            else if (!isUnattended && AutoBuildPipelineSettings.instance.IsBuildRevealEnabled)
            {
                options |= BuildOptions.ShowBuiltPlayer;
            }

#if UNITY_6000_0_OR_NEWER
            // A custom build profile carries its own scenes, scripting defines and development options.
            BuildProfile activeProfile = BuildProfile.GetActiveBuildProfile();
            if (activeProfile != null)
            {
                BuildPlayerWithProfileOptions profileOptions = new()
                {
                    buildProfile = activeProfile,
                    locationPathName = plan.PlayerLocation,
                    options = options
                };

                return BuildPipeline.BuildPlayer(profileOptions);
            }
#endif

            BuildPlayerOptions playerOptions = new()
            {
                scenes = GetEnabledScenePaths(),
                target = plan.Platform.Target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(plan.Platform.Target),
                locationPathName = plan.PlayerLocation,
                options = options | GetDevelopmentOptions()
            };

            return BuildPipeline.BuildPlayer(playerOptions);
        }

        /// <summary>
        /// Mirrors the development checkboxes of Unity's build window for the active platform.
        /// </summary>
        private static BuildOptions GetDevelopmentOptions()
        {
            if (!EditorUserBuildSettings.development)
            {
                return BuildOptions.None;
            }

            BuildOptions options = BuildOptions.Development;
            if (EditorUserBuildSettings.allowDebugging)
            {
                options |= BuildOptions.AllowDebugging;
            }

            if (EditorUserBuildSettings.connectProfiler)
            {
                options |= BuildOptions.ConnectWithProfiler;
            }

            if (EditorUserBuildSettings.buildWithDeepProfilingSupport)
            {
                options |= BuildOptions.EnableDeepProfilingSupport;
            }

            if (EditorUserBuildSettings.waitForPlayerConnection)
            {
                options |= BuildOptions.WaitForPlayerConnection;
            }

            return options;
        }

        /// <exception cref="BuildFailedException">No scene is enabled.</exception>
        private static string[] GetEnabledScenePaths()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            List<string> scenePaths = new(scenes.Length);
            foreach (EditorBuildSettingsScene scene in scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                {
                    scenePaths.Add(scene.path);
                }
            }

            if (scenePaths.Count == 0)
            {
                throw new BuildFailedException($"There are no enabled scenes in the scene list of the {BuildWindowName} window.");
            }

            return scenePaths.ToArray();
        }

        private static bool IsSucceeded(BuildReport report, BuildPlan plan)
        {
            if (report.summary.result != BuildResult.Succeeded)
            {
                return false;
            }

            // A custom build profile may still force development options; such a build must never be shipped.
            if (plan.Request.IsRelease && (report.summary.options & BuildOptions.Development) != 0)
            {
                BuildLog.Error("The release came out as a development build. Turn off Development Build in the active build profile.");
                return false;
            }

            return true;
        }

        private static string GetFailureMessage(BuildReport report, BuildPlan plan)
        {
            string message = $"{plan.Platform.DisplayName} build {plan.DisplayVersion} failed with {report.summary.totalErrors} error(s); see the Console for details.";
            if (plan.Platform is AndroidPlatform && PlayerSettings.Android.useCustomKeystore)
            {
                message += " If signing failed, update the passwords with Build ▸ Keystore Passwords.";
            }

            return message;
        }

        private static void LogSuccess(BuildReport report, BuildPlan plan)
        {
            BuildSummary summary = report.summary;
            double sizeInMegabytes = summary.totalSize / (1024d * 1024d);
            string duration = summary.totalTime.ToString(@"mm\:ss", CultureInfo.InvariantCulture);
            BuildLog.Info($"Built {plan.Platform.DisplayName} {plan.DisplayVersion} in {duration}, {sizeInMegabytes.ToString("0.0", CultureInfo.InvariantCulture)} MB: {plan.PlayerLocation}");
        }
    }
}
