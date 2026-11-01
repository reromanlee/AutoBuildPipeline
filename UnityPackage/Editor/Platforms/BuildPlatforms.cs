using System;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Build targets the package supports.
    /// </summary>
    internal static class BuildPlatforms
    {
        /// <summary>
        /// Supported platforms for messages, e.g. when the active platform is not one of them.
        /// </summary>
        public const string SupportedNames = "Android, WebGL, Windows, macOS and Linux";

        private static readonly BuildPlatform[] All =
        {
            new AndroidPlatform(),
            new WebGLPlatform(),
            new StandalonePlatform(BuildTarget.StandaloneWindows64, "Windows", "Windows", ".exe"),
            new StandalonePlatform(BuildTarget.StandaloneWindows, "Windows (32-bit)", "Windows-x86", ".exe"),
            new StandalonePlatform(BuildTarget.StandaloneLinux64, "Linux", "Linux", ".x86_64"),
            new MacPlatform()
        };

        /// <summary>
        /// Finds the platform that builds the given target.
        /// </summary>
        public static bool TryGet(BuildTarget target, out BuildPlatform platform)
        {
            foreach (BuildPlatform candidate in All)
            {
                if (candidate.Target == target)
                {
                    platform = candidate;
                    return true;
                }
            }

            platform = null;
            return false;
        }

        /// <summary>
        /// Finds the platform by its target name, e.g. <c>Android</c>, as stored in a restore point.
        /// </summary>
        public static bool TryGet(string targetName, out BuildPlatform platform)
        {
            foreach (BuildPlatform candidate in All)
            {
                if (string.Equals(candidate.Target.ToString(), targetName, StringComparison.Ordinal))
                {
                    platform = candidate;
                    return true;
                }
            }

            platform = null;
            return false;
        }

        /// <summary>
        /// Whether the target can be built from the Build menu.
        /// </summary>
        public static bool IsSupported(BuildTarget target)
        {
            return TryGet(target, out _);
        }
    }
}
