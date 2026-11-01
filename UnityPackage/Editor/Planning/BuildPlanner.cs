using System;
using System.Globalization;
using System.IO;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Resolves a request into a <see cref="BuildPlan"/>. Everything that can fail on bad input fails here,
    /// before any setting changes, and planning reads nothing but its arguments.
    /// </summary>
    internal static class BuildPlanner
    {
        // GameCI passes this when its versioning is turned off.
        private const string NoValue = "none";

        /// <summary>
        /// Resolves the version, build number, packaging and paths of a build.
        /// </summary>
        /// <exception cref="BuildFailedException">A version, build number, label or path is invalid.</exception>
        public static BuildPlan Create(BuildPlatform platform, in BuildRequest request, BuildOverrides overrides, BuildInputs inputs)
        {
            ApplicationVersion version = ResolveVersion(request, inputs.CurrentVersion, overrides.Version);
            string previewLabel = ResolvePreviewLabel(request, inputs.PreviewLabel);
            bool hasBuildNumber = platform.HasBuildNumber && (request.IsRelease || IsSet(overrides.BuildNumber));

            BuildPlan plan = new()
            {
                Platform = platform,
                Request = request,
                CurrentVersion = inputs.CurrentVersion,
                Version = version,
                VersionName = request.IsRelease ? version.ToString() : PreviewVersion.Format(version, previewLabel, inputs.Timestamp, inputs.Commit),
                CurrentBuildNumber = inputs.CurrentBuildNumber,
                HasBuildNumber = hasBuildNumber,
                BuildNumber = hasBuildNumber ? ResolveBuildNumber(platform, request, inputs.CurrentBuildNumber, overrides.BuildNumber) : 0,
                IsDevelopment = !request.IsRelease && inputs.IsDevelopmentEnabled,
                Commit = inputs.Commit,
                ProductName = inputs.ProductName,
                ApplicationIdentifier = inputs.ApplicationIdentifier
            };

            ResolveLocation(plan, overrides, inputs, previewLabel);
            return plan;
        }

        private static ApplicationVersion ResolveVersion(in BuildRequest request, string currentVersion, string overrideVersion)
        {
            if (IsSet(overrideVersion))
            {
                if (!ApplicationVersion.TryParse(overrideVersion, out ApplicationVersion exactVersion))
                {
                    throw new BuildFailedException($"The version '{overrideVersion}' from the command line is not in the 'major.minor.patch' format, e.g. 1.6.0.");
                }

                return exactVersion;
            }

            if (!ApplicationVersion.TryParse(currentVersion, out ApplicationVersion version))
            {
                throw new BuildFailedException($"The version '{currentVersion}' in Player Settings is not in the 'major.minor.patch' format. Set it to a version such as 1.0.0 in Player Settings ▸ Other Settings.");
            }

            try
            {
                return version.Increment(request.VersionIncrement);
            }
            catch (OverflowException)
            {
                throw new BuildFailedException($"The version '{currentVersion}' cannot be incremented any further.");
            }
        }

        private static string ResolvePreviewLabel(in BuildRequest request, string previewLabel)
        {
            if (request.IsRelease)
            {
                return null;
            }

            if (!PreviewVersion.IsValidLabel(previewLabel))
            {
                throw new BuildFailedException($"The preview label '{previewLabel}' must consist of letters, digits, hyphens and dots, e.g. dev or beta.2. Change it in Project Settings ▸ Auto Build Pipeline.");
            }

            return previewLabel;
        }

        private static int ResolveBuildNumber(BuildPlatform platform, in BuildRequest request, string currentBuildNumber, string overrideBuildNumber)
        {
            string name = platform.BuildNumberName;
            int parsedNumber;
            long buildNumber;
            if (IsSet(overrideBuildNumber))
            {
                if (!TryParseBuildNumber(overrideBuildNumber, out parsedNumber))
                {
                    throw new BuildFailedException($"The {name} '{overrideBuildNumber}' from the command line is not a whole number.");
                }

                buildNumber = parsedNumber;
            }
            else
            {
                if (!TryParseBuildNumber(string.IsNullOrWhiteSpace(currentBuildNumber) ? "0" : currentBuildNumber, out parsedNumber))
                {
                    throw new BuildFailedException($"The {name} '{currentBuildNumber}' in Player Settings is not a whole number.");
                }

                // Counted in a long, so incrementing the largest whole number cannot overflow.
                buildNumber = request.IsBuildNumberIncremented ? parsedNumber + 1L : parsedNumber;
            }

            if (buildNumber < platform.MinimumBuildNumber || buildNumber > platform.MaximumBuildNumber)
            {
                throw new BuildFailedException($"The {name} must be between {platform.MinimumBuildNumber} and {platform.MaximumBuildNumber}, but would be {buildNumber}.");
            }

            return (int)buildNumber;
        }

        private static void ResolveLocation(BuildPlan plan, BuildOverrides overrides, BuildInputs inputs, string previewLabel)
        {
            BuildPlatform platform = plan.Platform;
            bool isReleaseAppBundle = platform.SupportsAppBundle && plan.Request.IsRelease && inputs.AndroidReleaseFormat == AndroidReleaseFormat.AppBundle;

            // An exact location is used as given, and its extension decides the Android package format.
            if (IsSet(overrides.PlayerLocation))
            {
                plan.PlayerLocation = ProjectPaths.ToAbsolute(inputs.ProjectFolder, overrides.PlayerLocation.Trim());
                string extension = Path.GetExtension(plan.PlayerLocation);
                bool isAppBundleExtension = string.Equals(extension, AndroidPlatform.AppBundleExtension, StringComparison.OrdinalIgnoreCase);
                bool isPackageExtension = string.Equals(extension, AndroidPlatform.PackageExtension, StringComparison.OrdinalIgnoreCase);
                plan.IsAppBundle = platform.SupportsAppBundle && (isAppBundleExtension || (!isPackageExtension && isReleaseAppBundle));
                plan.BuildFolder = null;
                return;
            }

            string nameBase = platform.GetNameBase(inputs.ProductName, inputs.ApplicationIdentifier);
            string buildName;
            if (!plan.Request.IsRelease)
            {
                // Previews reuse one folder, so they never pile up on disk.
                buildName = $"{nameBase}-{previewLabel}";
            }
            else if (plan.HasBuildNumber)
            {
                buildName = string.Format(CultureInfo.InvariantCulture, "{0}-{1}-{2}", nameBase, plan.Version, plan.BuildNumber);
            }
            else
            {
                buildName = $"{nameBase}-{plan.Version}";
            }

            string outputFolder = IsSet(overrides.OutputFolder) ? overrides.OutputFolder.Trim() : inputs.OutputFolder;
            string platformFolder = Path.Combine(ProjectPaths.ToAbsolute(inputs.ProjectFolder, outputFolder), platform.FolderName);
            plan.IsAppBundle = isReleaseAppBundle;
            plan.BuildFolder = Path.Combine(platformFolder, buildName);
            plan.PlayerLocation = platform.GetPlayerPath(plan.BuildFolder, buildName, nameBase, plan.IsAppBundle);
        }

        private static bool TryParseBuildNumber(string text, out int buildNumber)
        {
            return int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out buildNumber);
        }

        private static bool IsSet(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && !string.Equals(value.Trim(), NoValue, StringComparison.OrdinalIgnoreCase);
        }
    }
}
