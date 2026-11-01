namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Values that replace project settings for a single build, e.g. from command line arguments.
    /// Empty fields leave the project settings in effect.
    /// </summary>
    internal sealed class BuildOverrides
    {
        /// <summary>
        /// Root folder of all builds, replacing the Output Folder setting. Relative paths start at the project folder.
        /// </summary>
        public string OutputFolder;

        /// <summary>
        /// Exact path the player is built to, e.g. <c>build/Android/Game.aab</c> or a folder for WebGL.
        /// Bypasses the folder layout and the replacement safeguard.
        /// </summary>
        public string PlayerLocation;

        /// <summary>
        /// Exact version, e.g. <c>1.7.0</c>, replacing the requested increment.
        /// </summary>
        public string Version;

        /// <summary>
        /// Exact build number (Android version code, macOS build number), replacing the automatic increment.
        /// </summary>
        public string BuildNumber;

        /// <summary>
        /// Android keystore file; relative paths start at the project folder. Enables custom keystore signing.
        /// </summary>
        public string KeystorePath;

        /// <summary>
        /// Android key alias name.
        /// </summary>
        public string AliasName;

        /// <summary>
        /// Android keystore password. Environment variables take precedence.
        /// </summary>
        public string KeystorePassword;

        /// <summary>
        /// Android key alias password; the keystore password is used when it is empty.
        /// </summary>
        public string AliasPassword;

        /// <summary>
        /// Shows no dialogs, prompts or file browser windows, as in batch mode.
        /// </summary>
        public bool IsUnattended;
    }
}
