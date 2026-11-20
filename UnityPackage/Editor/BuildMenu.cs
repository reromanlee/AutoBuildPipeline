using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// The top-level <c>Build</c> menu. Every item acts on the active platform and can be bound to a key
    /// in <c>Edit ▸ Shortcuts ▸ Main Menu ▸ Build</c>.
    /// </summary>
    internal static class BuildMenu
    {
        private const string MenuRoot = "Build/";
        private const string SwitchPlatformRoot = MenuRoot + "Switch Platform/";
        private const string PatchReleaseItem = MenuRoot + "Patch Release";
        private const string MinorReleaseItem = MenuRoot + "Minor Release";
        private const string MajorReleaseItem = MenuRoot + "Major Release";
        private const string ExactReleaseItem = MenuRoot + "Exact Release";
        private const string PreviewAndRunItem = MenuRoot + "Preview and Run";
        private const string PreviewItem = MenuRoot + "Preview";
        private const string SwitchAndroidItem = SwitchPlatformRoot + "Android";
        private const string SwitchWebGLItem = SwitchPlatformRoot + "WebGL";
        private const string SwitchWindowsItem = SwitchPlatformRoot + "Windows";
        private const string SwitchMacItem = SwitchPlatformRoot + "macOS";
        private const string SwitchLinuxItem = SwitchPlatformRoot + "Linux";
        private const string OpenBuildsFolderItem = MenuRoot + "Open Builds Folder";
        private const string KeystorePasswordsItem = MenuRoot + "Keystore Passwords...";
        private const string ForgetKeystorePasswordsItem = MenuRoot + "Forget Keystore Passwords";
        private const string SettingsItem = MenuRoot + "Settings...";

        // Unity draws a separator between items whose priorities differ by more than ten.
        private const int ReleasePriority = 0;
        private const int PreviewPriority = 20;
        private const int SwitchPlatformPriority = 40;
        private const int ToolsPriority = 60;

        [MenuItem(PatchReleaseItem, priority = ReleasePriority)]
        private static void BuildPatchRelease()
        {
            BuildFromEditor(BuildRequest.PatchRelease);
        }

        [MenuItem(MinorReleaseItem, priority = ReleasePriority + 1)]
        private static void BuildMinorRelease()
        {
            BuildFromEditor(BuildRequest.MinorRelease);
        }

        [MenuItem(MajorReleaseItem, priority = ReleasePriority + 2)]
        private static void BuildMajorRelease()
        {
            BuildFromEditor(BuildRequest.MajorRelease);
        }

        [MenuItem(ExactReleaseItem, priority = ReleasePriority + 3)]
        private static void BuildExactRelease()
        {
            BuildFromEditor(BuildRequest.ExactRelease);
        }

        [MenuItem(PreviewAndRunItem, priority = PreviewPriority)]
        private static void BuildPreviewAndRun()
        {
            BuildFromEditor(BuildRequest.PreviewAndRun);
        }

        [MenuItem(PreviewItem, priority = PreviewPriority + 1)]
        private static void BuildPreview()
        {
            BuildFromEditor(BuildRequest.Preview);
        }

        [MenuItem(PatchReleaseItem, true)]
        [MenuItem(MinorReleaseItem, true)]
        [MenuItem(MajorReleaseItem, true)]
        [MenuItem(ExactReleaseItem, true)]
        [MenuItem(PreviewAndRunItem, true)]
        [MenuItem(PreviewItem, true)]
        private static bool CanBuild()
        {
            return !BuildPipeline.isBuildingPlayer &&
                   !EditorApplication.isCompiling &&
                   !EditorApplication.isPlayingOrWillChangePlaymode &&
                   BuildPlatforms.IsSupported(EditorUserBuildSettings.activeBuildTarget);
        }

        [MenuItem(SwitchAndroidItem, priority = SwitchPlatformPriority)]
        private static void SwitchToAndroid()
        {
            SwitchPlatform(BuildTarget.Android, "Android");
        }

        [MenuItem(SwitchAndroidItem, true)]
        private static bool CanSwitchToAndroid()
        {
            return CanSwitchPlatform(SwitchAndroidItem, BuildTarget.Android);
        }

        [MenuItem(SwitchWebGLItem, priority = SwitchPlatformPriority + 1)]
        private static void SwitchToWebGL()
        {
            SwitchPlatform(BuildTarget.WebGL, "WebGL");
        }

        [MenuItem(SwitchWebGLItem, true)]
        private static bool CanSwitchToWebGL()
        {
            return CanSwitchPlatform(SwitchWebGLItem, BuildTarget.WebGL);
        }

        [MenuItem(SwitchWindowsItem, priority = SwitchPlatformPriority + 2)]
        private static void SwitchToWindows()
        {
            SwitchPlatform(BuildTarget.StandaloneWindows64, "Windows");
        }

        [MenuItem(SwitchWindowsItem, true)]
        private static bool CanSwitchToWindows()
        {
            return CanSwitchPlatform(SwitchWindowsItem, BuildTarget.StandaloneWindows64);
        }

        [MenuItem(SwitchMacItem, priority = SwitchPlatformPriority + 3)]
        private static void SwitchToMac()
        {
            SwitchPlatform(BuildTarget.StandaloneOSX, "macOS");
        }

        [MenuItem(SwitchMacItem, true)]
        private static bool CanSwitchToMac()
        {
            return CanSwitchPlatform(SwitchMacItem, BuildTarget.StandaloneOSX);
        }

        [MenuItem(SwitchLinuxItem, priority = SwitchPlatformPriority + 4)]
        private static void SwitchToLinux()
        {
            SwitchPlatform(BuildTarget.StandaloneLinux64, "Linux");
        }

        [MenuItem(SwitchLinuxItem, true)]
        private static bool CanSwitchToLinux()
        {
            return CanSwitchPlatform(SwitchLinuxItem, BuildTarget.StandaloneLinux64);
        }

        [MenuItem(OpenBuildsFolderItem, priority = ToolsPriority)]
        private static void OpenBuildsFolder()
        {
            string outputFolder = ProjectPaths.ToAbsolute(AutoBuildPipelineSettings.instance.OutputFolder);
            string folder = outputFolder;
            if (BuildPlatforms.TryGet(EditorUserBuildSettings.activeBuildTarget, out BuildPlatform platform))
            {
                string platformFolder = Path.Combine(outputFolder, platform.FolderName);
                if (Directory.Exists(platformFolder))
                {
                    folder = platformFolder;
                }
            }

            if (!Directory.Exists(folder))
            {
                BuildLog.Warning($"There are no builds in '{outputFolder}' yet.");
                return;
            }

            EditorUtility.OpenWithDefaultApp(folder);
        }

        [MenuItem(KeystorePasswordsItem, priority = ToolsPriority + 1)]
        private static void SetKeystorePasswords()
        {
            try
            {
                AndroidSigning.PromptAndRemember();
            }
            catch (BuildFailedException exception)
            {
                BuildLog.Error(exception.Message);
            }
        }

        [MenuItem(ForgetKeystorePasswordsItem, priority = ToolsPriority + 2)]
        private static void ForgetKeystorePasswords()
        {
            try
            {
                AndroidSigning.Forget();
            }
            catch (BuildFailedException exception)
            {
                BuildLog.Error(exception.Message);
            }
        }

        [MenuItem(KeystorePasswordsItem, true)]
        [MenuItem(ForgetKeystorePasswordsItem, true)]
        private static bool IsAndroidActive()
        {
            return EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android && !BuildPipeline.isBuildingPlayer;
        }

        [MenuItem(SettingsItem, priority = ToolsPriority + 3)]
        private static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(AutoBuildPipelineSettingsProvider.SettingsPath);
        }

        /// <summary>
        /// Builds and reports an expected failure as a single Console error without a stack trace;
        /// unexpected exceptions still surface with their stack trace.
        /// </summary>
        private static void BuildFromEditor(in BuildRequest request)
        {
            try
            {
                AutoBuilder.Build(request);
            }
            catch (BuildFailedException exception)
            {
                BuildLog.Error(exception.Message);
            }
        }

        private static bool CanSwitchPlatform(string itemPath, BuildTarget target)
        {
            bool isActive = EditorUserBuildSettings.activeBuildTarget == target;
            Menu.SetChecked(itemPath, isActive);
            if (isActive || BuildPipeline.isBuildingPlayer || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return false;
            }

            return BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target);
        }

        private static void SwitchPlatform(BuildTarget target, string displayName)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
            {
                return;
            }

            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(target);
            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
            {
                BuildLog.Error($"Cannot switch to {displayName}: the {displayName} Build Support module is not installed in this Unity Editor.");
                return;
            }

            string currentName = BuildPlatforms.TryGet(EditorUserBuildSettings.activeBuildTarget, out BuildPlatform currentPlatform)
                ? currentPlatform.DisplayName
                : EditorUserBuildSettings.activeBuildTarget.ToString();

            string message = $"Switch active platform from {currentName} to {displayName}?\n\nUnity will reimport assets for the new platform, which may take a while.";
            if (!EditorUtility.DisplayDialog("Auto Build Pipeline", message, "Switch", "Cancel"))
            {
                return;
            }

            if (!EditorUserBuildSettings.SwitchActiveBuildTargetAsync(targetGroup, target))
            {
                BuildLog.Error($"Could not switch active platform to {displayName}.");
            }
        }
    }
}
