using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Publishes build results as GitHub Actions step outputs, e.g. <c>${{ steps.build.outputs.version }}</c>,
    /// so a workflow can tag the release, commit the version change back or upload the build.
    /// Does nothing outside GitHub Actions.
    /// </summary>
    internal static class GitHubActionsOutput
    {
        /// <summary>
        /// Environment variable GitHub Actions sets to the step's output file.
        /// </summary>
        public const string OutputFileVariable = "GITHUB_OUTPUT";

        /// <summary>
        /// Appends the outputs to the file named by <c>GITHUB_OUTPUT</c>, when it is set.
        /// </summary>
        public static void Write(BuildPlan plan, string buildInfoPath)
        {
            string outputFilePath = Environment.GetEnvironmentVariable(OutputFileVariable);
            if (string.IsNullOrEmpty(outputFilePath))
            {
                return;
            }

            try
            {
                File.AppendAllText(outputFilePath, Format(plan, buildInfoPath));
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                BuildLog.Warning($"Could not write GitHub Actions outputs: {exception.Message}");
            }
        }

        /// <summary>
        /// Formats the <c>version</c>, <c>build-number</c>, <c>platform</c>, <c>output-path</c> and <c>build-info-path</c> outputs,
        /// one <c>name=value</c> line each. The build number is empty when the build has none.
        /// </summary>
        public static string Format(BuildPlan plan, string buildInfoPath)
        {
            StringBuilder builder = new(256);
            AppendLine(builder, "version", plan.VersionName);
            AppendLine(builder, "build-number", plan.HasBuildNumber ? plan.BuildNumber.ToString(CultureInfo.InvariantCulture) : string.Empty);
            AppendLine(builder, "platform", plan.Platform.DisplayName);
            AppendLine(builder, "output-path", plan.PlayerLocation);
            AppendLine(builder, "build-info-path", buildInfoPath ?? string.Empty);
            return builder.ToString();
        }

        private static void AppendLine(StringBuilder builder, string name, string value)
        {
            // A line break inside a value would start a new output, so values are kept on one line.
            builder.Append(name).Append('=').Append(value.Replace('\r', ' ').Replace('\n', ' ')).Append('\n');
        }
    }
}
