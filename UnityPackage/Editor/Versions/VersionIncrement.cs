namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Semantic version part to increment before a build. Incrementing a part resets every lower part to zero.
    /// </summary>
    public enum VersionIncrement
    {
        /// <summary>
        /// Keeps the version as it is, e.g. <c>1.6.3</c> stays <c>1.6.3</c>.
        /// </summary>
        None,

        /// <summary>
        /// Increments the patch part, e.g. <c>1.6.3</c> becomes <c>1.6.4</c>.
        /// </summary>
        Patch,

        /// <summary>
        /// Increments the minor part, e.g. <c>1.6.3</c> becomes <c>1.7.0</c>.
        /// </summary>
        Minor,

        /// <summary>
        /// Increments the major part, e.g. <c>1.6.3</c> becomes <c>2.0.0</c>.
        /// </summary>
        Major
    }
}
