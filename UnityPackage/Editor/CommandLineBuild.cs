using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Command line entry point for batch mode and CI. Builds the platform given by <c>-buildTarget</c>,
    /// never shows a dialog, and exits Unity with a non-zero code when the build fails.
    /// GameCI's arguments are understood too, so it works as a GameCI <c>buildMethod</c> without extra parameters.
    /// </summary>
    /// <example>
    /// <c>Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run -buildType patch</c>
    /// </example>
    public static class CommandLineBuild
    {
        /// <summary>
        /// Kind of build: <c>patch</c>, <c>minor</c>, <c>major</c>, <c>exact</c> or <c>preview</c>. Required.
        /// </summary>
        public const string BuildTypeArgument = "-buildType";

        /// <summary>
        /// Root folder of all builds, replacing the Output Folder setting. Relative paths start at the project folder.
        /// </summary>
        public const string OutputFolderArgument = "-buildOutput";

        /// <summary>
        /// Exact player path, e.g. <c>build/Android/Game.aab</c>, bypassing the folder layout. GameCI passes it.
        /// </summary>
        public const string PlayerLocationArgument = "-customBuildPath";

        /// <summary>
        /// Exact version, e.g. <c>1.7.0</c>, replacing the increment of the build type. GameCI passes it; <c>none</c> is ignored.
        /// </summary>
        public const string VersionArgument = "-buildVersion";

        /// <summary>
        /// Exact build number (Android version code, macOS build number), replacing the automatic increment.
        /// </summary>
        public const string BuildNumberArgument = "-buildNumber";

        /// <summary>
        /// GameCI's name for the Android version code.
        /// </summary>
        public const string AndroidVersionCodeArgument = "-androidVersionCode";

        /// <summary>
        /// Android keystore file, relative to the project folder or absolute. Turns on custom keystore signing. GameCI passes it.
        /// </summary>
        public const string KeystoreArgument = "-androidKeystoreName";

        /// <summary>
        /// Android key alias name. GameCI passes it.
        /// </summary>
        public const string AliasNameArgument = "-androidKeyaliasName";

        /// <summary>
        /// Android keystore password. Prefer the <c>ANDROID_KEYSTORE_PASS</c> environment variable,
        /// because arguments can be visible to other processes on the machine.
        /// </summary>
        public const string KeystorePasswordArgument = "-androidKeystorePass";

        /// <summary>
        /// Android key alias password. Prefer the <c>ANDROID_KEYALIAS_PASS</c> environment variable.
        /// </summary>
        public const string AliasPasswordArgument = "-androidKeyaliasPass";

        // Option names of newer GameCI versions.
        private const string AliasNameAlternative = "-androidKeyAlias";
        private const string KeystorePasswordAlternative = "-androidKeystorePassword";
        private const string AliasPasswordAlternative = "-androidKeyAliasPassword";

        /// <summary>
        /// Builds as the command line arguments describe. Call it with
        /// <c>-executeMethod reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run</c>.
        /// </summary>
        /// <exception cref="BuildFailedException">The arguments are invalid or the build did not succeed.</exception>
        public static void Run()
        {
            CommandLineArguments arguments = CommandLineArguments.Current;
            BuildRequest request = ParseBuildType(arguments.GetValue(BuildTypeArgument));
            BuildReport report = AutoBuilder.Build(request, CreateOverrides(arguments));
            if (report == null || report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException("The build was cancelled.");
            }
        }

        /// <summary>
        /// Maps a <c>-buildType</c> value to a request, ignoring letter case.
        /// </summary>
        /// <exception cref="BuildFailedException">The value is missing or unknown.</exception>
        internal static BuildRequest ParseBuildType(string buildType)
        {
            switch (buildType?.Trim().ToLowerInvariant())
            {
                case "patch":
                {
                    return BuildRequest.PatchRelease;
                }
                case "minor":
                {
                    return BuildRequest.MinorRelease;
                }
                case "major":
                {
                    return BuildRequest.MajorRelease;
                }
                case "exact":
                {
                    return BuildRequest.ExactRelease;
                }
                case "preview":
                {
                    return BuildRequest.Preview;
                }
                default:
                {
                    string given = string.IsNullOrWhiteSpace(buildType) ? "nothing" : $"'{buildType}'";
                    throw new BuildFailedException($"{BuildTypeArgument} must be patch, minor, major, exact or preview, but was {given}.");
                }
            }
        }

        /// <summary>
        /// Collects the overrides from the arguments; package names win over their GameCI aliases.
        /// </summary>
        internal static BuildOverrides CreateOverrides(CommandLineArguments arguments)
        {
            return new BuildOverrides
            {
                OutputFolder = arguments.GetValue(OutputFolderArgument),
                PlayerLocation = arguments.GetValue(PlayerLocationArgument),
                Version = arguments.GetValue(VersionArgument),
                BuildNumber = arguments.GetValue(BuildNumberArgument) ?? GetGameCIVersionCode(arguments),
                KeystorePath = arguments.GetValue(KeystoreArgument),
                AliasName = arguments.GetValue(AliasNameArgument, AliasNameAlternative),
                KeystorePassword = arguments.GetValue(KeystorePasswordArgument, KeystorePasswordAlternative),
                AliasPassword = arguments.GetValue(AliasPasswordArgument, AliasPasswordAlternative),
                IsUnattended = true
            };
        }

        /// <summary>
        /// GameCI passes a version code of <c>0</c> when its versioning is turned off, meaning "not applied".
        /// </summary>
        private static string GetGameCIVersionCode(CommandLineArguments arguments)
        {
            string versionCode = arguments.GetValue(AndroidVersionCodeArgument);
            return versionCode != null && versionCode.Trim() == "0" ? null : versionCode;
        }
    }
}
