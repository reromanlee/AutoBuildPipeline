using System;
using UnityEditor;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Snapshot of the project state a build plan is computed from, so planning itself reads nothing from the editor.
    /// </summary>
    internal sealed class BuildInputs
    {
        /// <summary>
        /// Version text in Player Settings, e.g. <c>1.6.3</c>.
        /// </summary>
        public string CurrentVersion;

        /// <summary>
        /// Build number text in Player Settings, or null when the platform has none.
        /// </summary>
        public string CurrentBuildNumber;

        public string ProductName;
        public string ApplicationIdentifier;
        public string ProjectFolder;

        /// <summary>
        /// Root folder of all builds from the settings, before command line overrides.
        /// </summary>
        public string OutputFolder;

        public string PreviewLabel;
        public AndroidReleaseFormat AndroidReleaseFormat;

        /// <summary>
        /// Whether Development Build is checked for the active platform.
        /// </summary>
        public bool IsDevelopmentEnabled;

        /// <summary>
        /// Local time the build starts, used in preview versions.
        /// </summary>
        public DateTime Timestamp;

        /// <summary>
        /// Full commit hash of the project, or null when it is not in a git repository.
        /// </summary>
        public string Commit;

        /// <summary>
        /// Reads the current state of the project and settings for the platform.
        /// </summary>
        /// <param name="isCommitRead">Whether git is asked for the commit, which takes a few milliseconds.</param>
        public static BuildInputs Capture(BuildPlatform platform, bool isCommitRead = true)
        {
            AutoBuildPipelineSettings settings = AutoBuildPipelineSettings.instance;
            string projectFolder = ProjectPaths.ProjectFolder;
            string commit = null;
            if (isCommitRead)
            {
                GitRepository.TryReadCommit(projectFolder, out commit);
            }

            return new BuildInputs
            {
                CurrentVersion = PlayerSettings.bundleVersion,
                CurrentBuildNumber = platform.ReadBuildNumber(),
                ProductName = PlayerSettings.productName,
                ApplicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.FromBuildTargetGroup(BuildPipeline.GetBuildTargetGroup(platform.Target))),
                ProjectFolder = projectFolder,
                OutputFolder = settings.OutputFolder,
                PreviewLabel = settings.PreviewLabel,
                AndroidReleaseFormat = settings.AndroidReleaseFormat,
                IsDevelopmentEnabled = EditorUserBuildSettings.development,
                Timestamp = DateTime.Now,
                Commit = commit
            };
        }
    }
}
