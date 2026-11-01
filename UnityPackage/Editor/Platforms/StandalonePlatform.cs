using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Windows and Linux builds: the player executable named after the product inside the build folder,
    /// e.g. <c>Builds/Windows/My Game-1.6.0/My Game.exe</c>.
    /// </summary>
    internal class StandalonePlatform : BuildPlatform
    {
        private readonly string _extension;

        /// <param name="extension">Player file extension, e.g. <c>.exe</c>.</param>
        public StandalonePlatform(BuildTarget target, string displayName, string folderName, string extension) : base(target, displayName, folderName)
        {
            _extension = extension;
        }

        /// <inheritdoc/>
        public override string GetPlayerPath(string buildFolder, string buildName, string nameBase, bool isAppBundle)
        {
            return GetPlayerFilePath(buildFolder, nameBase, _extension);
        }
    }
}
