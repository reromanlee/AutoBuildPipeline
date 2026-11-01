using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Runs short-lived command line tools (<c>git</c>, <c>security</c>, <c>secret-tool</c>) without a console window.
    /// </summary>
    internal static class ExternalProcess
    {
        /// <summary>
        /// Default time a tool may take before it is stopped.
        /// </summary>
        public const int DefaultTimeoutMilliseconds = 15000;

        /// <summary>
        /// Runs the tool and returns true when it exits with code zero.
        /// Returns false when the tool is missing, cannot start or exceeds the timeout.
        /// </summary>
        /// <param name="fileName">Tool name looked up on the PATH, or a full path.</param>
        /// <param name="standardInput">Text written to the tool's input, or null to leave it unredirected.</param>
        /// <param name="workingDirectory">Folder the tool runs in, or null for the editor's current folder.</param>
        public static bool TryRun(string fileName, string arguments, string standardInput, string workingDirectory, int timeoutMilliseconds, out string standardOutput)
        {
            standardOutput = null;
            ProcessStartInfo startInfo = new(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = standardInput != null,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            if (!string.IsNullOrEmpty(workingDirectory))
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            try
            {
                using Process process = Process.Start(startInfo);
                if (process == null)
                {
                    return false;
                }

                // Both streams are drained asynchronously so a full pipe can never block the tool.
                Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                Task<string> errorTask = process.StandardError.ReadToEndAsync();
                if (standardInput != null)
                {
                    process.StandardInput.Write(standardInput);
                    process.StandardInput.Close();
                }

                if (!process.WaitForExit(timeoutMilliseconds))
                {
                    process.Kill();
                    return false;
                }

                standardOutput = outputTask.Result;
                errorTask.Wait();
                return process.ExitCode == 0;
            }
            catch (Exception exception) when (exception is Win32Exception || exception is InvalidOperationException || exception is AggregateException)
            {
                // The tool is not installed, could not be started, or exited while it was being stopped.
                return false;
            }
        }

        /// <summary>
        /// Wraps a value in double quotes, escaping quotes and backslashes, so it is passed as a single argument.
        /// </summary>
        public static string Quote(string value)
        {
            StringBuilder builder = new(value.Length + 2);
            builder.Append('"');
            foreach (char character in value)
            {
                if (character == '"' || character == '\\')
                {
                    builder.Append('\\');
                }

                builder.Append(character);
            }

            builder.Append('"');
            return builder.ToString();
        }
    }
}
