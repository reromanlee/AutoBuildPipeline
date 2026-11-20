using System.IO;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Resolved details of an automated build currently running through <see cref="AutoBuilder.Build"/>.
    /// Read from <see cref="AutoBuilder.CurrentBuild"/> inside Unity build callbacks such as
    /// <c>IPreprocessBuildWithReport</c> and <c>IPostprocessBuildWithReport</c>.
    /// </summary>
    public sealed class BuildContext
    {
        internal BuildContext(BuildPlan plan, bool isUnattended)
        {
            Request = plan.Request;
            Target = plan.Platform.Target;
            PlatformName = plan.Platform.DisplayName;
            Version = plan.Version;
            VersionName = plan.VersionName;
            HasBuildNumber = plan.HasBuildNumber;
            BuildNumber = plan.BuildNumber;
            IsDevelopment = plan.IsDevelopment;
            IsAppBundle = plan.IsAppBundle;
            PlayerLocation = plan.PlayerLocation;
            BuildFolder = plan.BuildFolder ?? Path.GetDirectoryName(
                Path.GetFullPath(plan.PlayerLocation).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            Commit = plan.Commit;
            IsUnattended = isUnattended;
        }

        /// <summary>
        /// Request that started the build.
        /// </summary>
        public BuildRequest Request { get; }

        /// <summary>
        /// Unity build target being built.
        /// </summary>
        public BuildTarget Target { get; }

        /// <summary>
        /// Human-readable platform name, e.g. <c>Android</c> or <c>Windows</c>.
        /// </summary>
        public string PlatformName { get; }

        /// <summary>
        /// Resolved semantic version (the release version, or the upcoming version for a preview).
        /// </summary>
        public ApplicationVersion Version { get; }

        /// <summary>
        /// Full version string written to <c>PlayerSettings.bundleVersion</c>, e.g. <c>1.6.4</c> or <c>1.6.4-dev+261101.1432</c>.
        /// </summary>
        public string VersionName { get; }

        /// <summary>
        /// Whether this build has a resolved build number (Android version code or macOS build number).
        /// </summary>
        public bool HasBuildNumber { get; }

        /// <summary>
        /// Resolved build number when <see cref="HasBuildNumber"/> is true; <c>0</c> otherwise.
        /// </summary>
        public int BuildNumber { get; }

        /// <summary>
        /// Whether this is a release build (shortcut for <see cref="BuildRequest.IsRelease"/>).
        /// </summary>
        public bool IsRelease
        {
            get
            {
                return Request.IsRelease;
            }
        }

        /// <summary>
        /// Whether Development Build is active for this build.
        /// </summary>
        public bool IsDevelopment { get; }

        /// <summary>
        /// Whether an Android App Bundle (<c>.aab</c>) is being built.
        /// </summary>
        public bool IsAppBundle { get; }

        /// <summary>
        /// Output folder that contains the player and <c>build-info.json</c>.
        /// </summary>
        public string BuildFolder { get; }

        /// <summary>
        /// Exact player path passed to Unity (a file, or the site folder for WebGL).
        /// </summary>
        public string PlayerLocation { get; }

        /// <summary>
        /// Full git commit hash, or null when unknown.
        /// </summary>
        public string Commit { get; }

        /// <summary>
        /// Whether the build runs unattended (batch mode or CLI).
        /// </summary>
        public bool IsUnattended { get; }
    }
}
