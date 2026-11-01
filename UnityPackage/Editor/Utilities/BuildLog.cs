using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Console messages of the package, prefixed with <c>[AutoBuildPipeline]</c> and written without a stack trace,
    /// because the call site of a build tool is never what the reader is looking for.
    /// </summary>
    internal static class BuildLog
    {
        public const string Prefix = "[AutoBuildPipeline]";

        public static void Info(string message)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0} {1}", Prefix, message);
        }

        public static void Warning(string message)
        {
            Debug.LogFormat(LogType.Warning, LogOption.NoStacktrace, null, "{0} {1}", Prefix, message);
        }

        public static void Error(string message)
        {
            Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "{0} {1}", Prefix, message);
        }
    }
}
