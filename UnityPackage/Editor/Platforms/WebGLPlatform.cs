using System.IO;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// WebGL builds: a web folder named after the product inside the build folder,
    /// so <c>build-info.json</c> sits next to the site instead of being published with it.
    /// </summary>
    internal sealed class WebGLPlatform : BuildPlatform
    {
        public WebGLPlatform() : base(BuildTarget.WebGL, "WebGL", "WebGL")
        {
            // Every WebGL build uses the same target, name and folder.
        }

        /// <inheritdoc/>
        /// <remarks>Returns the site folder, e.g. <c>Builds/WebGL/My Game-1.6.0/My Game</c>.</remarks>
        public override string GetPlayerPath(string buildFolder, string buildName, string nameBase, bool isAppBundle)
        {
            return Path.Combine(buildFolder, nameBase);
        }
    }
}
