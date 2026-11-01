using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Package format of Android release builds. Previews are always APKs, which install much faster.
    /// </summary>
    public enum AndroidReleaseFormat
    {
        /// <summary>
        /// Android App Bundle, required by Google Play.
        /// </summary>
        [InspectorName("App Bundle (.aab)")]
        AppBundle,

        /// <summary>
        /// Android package for other stores and direct installs.
        /// </summary>
        [InspectorName("APK (.apk)")]
        Apk
    }
}
