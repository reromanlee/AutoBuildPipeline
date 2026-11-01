using System;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Writes <c>build-info.json</c> into the folder that holds the player.
    /// </summary>
    internal static class BuildInfoFile
    {
        public const string FileName = "build-info.json";

        /// <summary>
        /// Path of the file for a player location: next to the player file, or next to the WebGL site folder.
        /// </summary>
        public static string GetPath(string playerLocation)
        {
            string folder = Path.GetDirectoryName(Path.GetFullPath(playerLocation).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            return Path.Combine(folder ?? string.Empty, FileName);
        }

        /// <summary>
        /// Describes a successful build.
        /// </summary>
        public static BuildInfoData Create(BuildPlan plan, DateTime createdUtc, long sizeInBytes)
        {
            return new BuildInfoData
            {
                Product = plan.ProductName,
                Identifier = plan.ApplicationIdentifier,
                Platform = plan.Platform.DisplayName,
                Version = plan.VersionName,
                BuildNumber = plan.HasBuildNumber ? plan.BuildNumber : 0,
                IsRelease = plan.Request.IsRelease,
                IsDevelopment = plan.IsDevelopment,
                Commit = plan.Commit ?? string.Empty,
                UnityVersion = Application.unityVersion,
                CreatedUtc = createdUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture),
                Player = Path.GetFileName(plan.PlayerLocation.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)),
                SizeInBytes = sizeInBytes
            };
        }

        /// <summary>
        /// Writes the file; a failure is logged as a warning, because the build itself succeeded.
        /// </summary>
        /// <returns>The file path, or null when it could not be written.</returns>
        public static string Write(BuildInfoData data, string playerLocation)
        {
            string filePath = GetPath(playerLocation);
            try
            {
                File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
                return filePath;
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                BuildLog.Warning($"Could not write '{filePath}': {exception.Message}");
                return null;
            }
        }
    }
}
