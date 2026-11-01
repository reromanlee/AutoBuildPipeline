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
        private const string PatchReleaseItem = MenuRoot + "Patch Release";
        private const string MinorReleaseItem = MenuRoot + "Minor Release";
        private const string MajorReleaseItem = MenuRoot + "Major Release";
        private const string ExactReleaseItem = MenuRoot + "Exact Release";
        private const string PreviewAndRunItem = MenuRoot + "Preview and Run";
        private const string PreviewItem = MenuRoot + "Preview";
        private const string OpenBuildsFolderItem = MenuRoot + "Open Builds Folder";
        private const string KeystorePasswordsItem = MenuRoot + "Keystore Passwords...";
        private const string ForgetKeystorePasswordsItem = MenuRoot + "Forget Keystore Passwords";
        private const string SettingsItem = MenuRoot + "Settings...";

        // Unity draws a separator between items whose priorities differ by more than ten.
        private const int ReleasePriority = 0;
        private const int PreviewPriority = 20;
        private const int ToolsPriority = 40;

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
    }
}
