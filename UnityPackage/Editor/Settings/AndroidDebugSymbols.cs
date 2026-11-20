using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Native debug symbols generated alongside Android release builds (e.g. <c>symbols.zip</c> for Google Play).
    /// Previews leave the editor's symbol setting untouched.
    /// </summary>
    public enum AndroidDebugSymbols
    {
        /// <summary>
        /// Do not generate native debug symbols.
        /// </summary>
        [InspectorName("Off")]
        Off = 0,

        /// <summary>
        /// Public symbol table (<c>Public</c> / <c>SymbolTable</c>), recommended for Google Play crash symbolication.
        /// </summary>
        [InspectorName("Public")]
        Public = 1,

        /// <summary>
        /// Full debugging symbols (<c>Debugging</c> / <c>Full</c>).
        /// </summary>
        [InspectorName("Debugging")]
        Debugging = 2
    }
}
