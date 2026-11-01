using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// The <c>Project Settings ▸ Auto Build Pipeline</c> page. Besides the settings, it shows where the next release
    /// and preview of the active platform will be written, and why a build would fail before it is started.
    /// </summary>
    internal static class AutoBuildPipelineSettingsProvider
    {
        internal const string SettingsPath = "Project/Auto Build Pipeline";

        private const float LabelWidth = 220f;
        private const float BrowseButtonWidth = 72f;

        private static readonly GUIContent OutputFolderLabel = new("Output Folder", "Root folder of all builds; each platform gets a subfolder. Relative paths start at the project folder.");
        private static readonly GUIContent PreviewLabelLabel = new("Preview Label", "Pre-release label of preview versions, e.g. dev in 1.6.1-dev+251025.1432.");
        private static readonly GUIContent ReleaseConfirmationLabel = new("Confirm Releases", "Asks before a release started from the editor, showing the version change.");
        private static readonly GUIContent BuildRevealLabel = new("Show Builds After Building", "Opens the build in Explorer or Finder after a build that is not launched.");
        private static readonly GUIContent AndroidReleaseFormatLabel = new("Android Release Format", "App Bundles are required by Google Play; APKs suit other stores and direct installs. Previews are always APKs.");

        // The next build paths are planned once per layout pass rather than on every repaint.
        private static string _nextReleaseText;
        private static string _nextPreviewText;
        private static string _planningProblem;

        [SettingsProvider]
        private static SettingsProvider Create()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Auto Build Pipeline",
                keywords = new HashSet<string> { "Build", "Release", "Preview", "Version", "Output", "Android", "App Bundle", "Keystore" },
                guiHandler = DrawSettings
            };
        }

        private static void DrawSettings(string searchContext)
        {
            AutoBuildPipelineSettings settings = AutoBuildPipelineSettings.instance;
            float previousLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = LabelWidth;

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(10f);
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawOutputSection(settings);
                    DrawVersionSection(settings);
                    DrawWorkflowSection(settings);
                    DrawAndroidSection(settings);
                }
            }

            EditorGUIUtility.labelWidth = previousLabelWidth;
        }

        private static void DrawOutputSection(AutoBuildPipelineSettings settings)
        {
            EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                string outputFolder = EditorGUILayout.DelayedTextField(OutputFolderLabel, settings.OutputFolder);
                if (outputFolder != settings.OutputFolder)
                {
                    settings.SetOutputFolder(outputFolder);
                }

                if (GUILayout.Button("Browse...", GUILayout.Width(BrowseButtonWidth)))
                {
                    string selectedFolder = EditorUtility.OpenFolderPanel("Output Folder", ProjectPaths.ToAbsolute(settings.OutputFolder), string.Empty);
                    if (!string.IsNullOrEmpty(selectedFolder))
                    {
                        settings.SetOutputFolder(ProjectPaths.ToProjectRelative(selectedFolder));
                    }

                    // The folder dialog interrupts the layout pass, so the rest of this frame is skipped.
                    GUIUtility.ExitGUI();
                }
            }

            if (Event.current.type == EventType.Layout)
            {
                PlanNextBuilds();
            }

            if (_planningProblem != null)
            {
                EditorGUILayout.HelpBox(_planningProblem, MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"Next release: {_nextReleaseText}\nNext preview: {_nextPreviewText}", MessageType.None);
            }

            EditorGUILayout.Space();
        }

        private static void DrawVersionSection(AutoBuildPipelineSettings settings)
        {
            EditorGUILayout.LabelField("Versions", EditorStyles.boldLabel);
            string previewLabel = EditorGUILayout.DelayedTextField(PreviewLabelLabel, settings.PreviewLabel);
            if (previewLabel != settings.PreviewLabel)
            {
                settings.SetPreviewLabel(previewLabel);
            }

            if (!PreviewVersion.IsValidLabel(settings.PreviewLabel))
            {
                EditorGUILayout.HelpBox("Use letters, digits, hyphens and dots, e.g. dev or beta.2.", MessageType.Warning);
            }

            EditorGUILayout.Space();
        }

        private static void DrawWorkflowSection(AutoBuildPipelineSettings settings)
        {
            EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
            bool isReleaseConfirmationEnabled = EditorGUILayout.Toggle(ReleaseConfirmationLabel, settings.IsReleaseConfirmationEnabled);
            if (isReleaseConfirmationEnabled != settings.IsReleaseConfirmationEnabled)
            {
                settings.SetReleaseConfirmationEnabled(isReleaseConfirmationEnabled);
            }

            bool isBuildRevealEnabled = EditorGUILayout.Toggle(BuildRevealLabel, settings.IsBuildRevealEnabled);
            if (isBuildRevealEnabled != settings.IsBuildRevealEnabled)
            {
                settings.SetBuildRevealEnabled(isBuildRevealEnabled);
            }

            EditorGUILayout.Space();
        }

        private static void DrawAndroidSection(AutoBuildPipelineSettings settings)
        {
            EditorGUILayout.LabelField("Android", EditorStyles.boldLabel);
            AndroidReleaseFormat format = (AndroidReleaseFormat)EditorGUILayout.EnumPopup(AndroidReleaseFormatLabel, settings.AndroidReleaseFormat);
            if (format != settings.AndroidReleaseFormat)
            {
                settings.SetAndroidReleaseFormat(format);
            }
        }

        private static void PlanNextBuilds()
        {
            _planningProblem = null;
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            if (!BuildPlatforms.TryGet(target, out BuildPlatform platform))
            {
                _planningProblem = $"The active platform {target} is not supported. Switch to {BuildPlatforms.SupportedNames} to build from the Build menu.";
                return;
            }

            try
            {
                // The commit only changes the preview version, never a path, so git is not asked on every layout pass.
                BuildInputs inputs = BuildInputs.Capture(platform, isCommitRead: false);
                BuildOverrides overrides = new();
                BuildPlan release = BuildPlanner.Create(platform, BuildRequest.PatchRelease, overrides, inputs);
                BuildPlan preview = BuildPlanner.Create(platform, BuildRequest.Preview, overrides, inputs);
                _nextReleaseText = $"{release.DisplayVersion} ▸ {ProjectPaths.ToProjectRelative(release.PlayerLocation)}";
                _nextPreviewText = $"{preview.VersionName} ▸ {ProjectPaths.ToProjectRelative(preview.PlayerLocation)}";
            }
            catch (BuildFailedException exception)
            {
                _planningProblem = exception.Message;
            }
        }
    }
}
