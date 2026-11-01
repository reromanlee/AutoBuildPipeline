using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Converts between absolute paths and paths relative to the Unity project folder.
    /// </summary>
    internal static class ProjectPaths
    {
        private const string ParentSegment = "..";

        /// <summary>
        /// Folder that holds <c>Assets</c>, <c>Packages</c> and <c>ProjectSettings</c>.
        /// </summary>
        public static string ProjectFolder
        {
            get
            {
                return Path.GetDirectoryName(Application.dataPath);
            }
        }

        /// <summary>
        /// Whether paths on the editor's file system differ only by letter case (Windows and macOS defaults).
        /// </summary>
        public static bool IsCaseInsensitive
        {
            get
            {
                return Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.OSXEditor;
            }
        }

        /// <summary>
        /// Absolute path of a path that is either absolute or relative to the project folder.
        /// </summary>
        public static string ToAbsolute(string path)
        {
            return ToAbsolute(ProjectFolder, path);
        }

        /// <summary>
        /// Absolute path of a path that is either absolute or relative to <paramref name="baseFolder"/>.
        /// </summary>
        public static string ToAbsolute(string baseFolder, string path)
        {
            return Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseFolder, path));
        }

        /// <summary>
        /// Path relative to the project folder with forward slashes, e.g. <c>../Builds</c>,
        /// or the absolute path when it is on another drive.
        /// </summary>
        public static string ToProjectRelative(string absolutePath)
        {
            return ToRelative(ProjectFolder, absolutePath, IsCaseInsensitive);
        }

        /// <summary>
        /// Path of <paramref name="targetPath"/> relative to <paramref name="baseFolder"/> with forward slashes,
        /// <c>.</c> when both are the same folder, or the absolute target path when the two share no root.
        /// </summary>
        public static string ToRelative(string baseFolder, string targetPath, bool isCaseInsensitive)
        {
            string[] baseSegments = SplitSegments(Path.GetFullPath(baseFolder));
            string[] targetSegments = SplitSegments(Path.GetFullPath(targetPath));
            StringComparison comparison = isCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            int commonCount = 0;
            int maximumCount = Math.Min(baseSegments.Length, targetSegments.Length);
            while (commonCount < maximumCount && string.Equals(baseSegments[commonCount], targetSegments[commonCount], comparison))
            {
                commonCount++;
            }

            // Windows paths on different drives or shares cannot be expressed relative to each other.
            bool isWindowsRooted = targetSegments.Length > 0 && targetSegments[0].EndsWith(":", StringComparison.Ordinal);
            if (commonCount == 0 && (isWindowsRooted || targetPath.StartsWith(@"\\", StringComparison.Ordinal)))
            {
                return Path.GetFullPath(targetPath).Replace('\\', '/');
            }

            StringBuilder builder = new();
            for (int index = commonCount; index < baseSegments.Length; index++)
            {
                AppendSegment(builder, ParentSegment);
            }

            for (int index = commonCount; index < targetSegments.Length; index++)
            {
                AppendSegment(builder, targetSegments[index]);
            }

            return builder.Length == 0 ? "." : builder.ToString();
        }

        private static string[] SplitSegments(string fullPath)
        {
            return fullPath.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static void AppendSegment(StringBuilder builder, string segment)
        {
            if (builder.Length > 0)
            {
                builder.Append('/');
            }

            builder.Append(segment);
        }
    }
}
