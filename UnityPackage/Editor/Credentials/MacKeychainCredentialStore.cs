using System;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Stores secrets in the macOS login keychain through the built-in <c>security</c> tool.
    /// </summary>
    internal sealed class MacKeychainCredentialStore : ICredentialStore
    {
        private const string ToolName = "/usr/bin/security";

        private readonly string _service;

        public MacKeychainCredentialStore(string service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public string DisplayName
        {
            get
            {
                return "macOS Keychain";
            }
        }

        /// <inheritdoc/>
        public bool IsPersistent
        {
            get
            {
                return true;
            }
        }

        /// <inheritdoc/>
        public bool TryRead(string account, out string secret)
        {
            secret = null;
            string arguments = $"find-generic-password -s {ExternalProcess.Quote(_service)} -a {ExternalProcess.Quote(account)} -w";
            if (!ExternalProcess.TryRun(ToolName, arguments, null, null, ExternalProcess.DefaultTimeoutMilliseconds, out string output) ||
                string.IsNullOrEmpty(output))
            {
                return false;
            }

            // The tool terminates the password with a single line feed.
            secret = output.EndsWith("\n", StringComparison.Ordinal) ? output.Substring(0, output.Length - 1) : output;
            return secret.Length > 0;
        }

        /// <inheritdoc/>
        public bool TryWrite(string account, string secret)
        {
            // The secret is passed as an argument because the tool has no standard input option;
            // it is only visible to local process listings for the few milliseconds the tool runs.
            string arguments = $"add-generic-password -U -s {ExternalProcess.Quote(_service)} -a {ExternalProcess.Quote(account)} -w {ExternalProcess.Quote(secret)}";
            return ExternalProcess.TryRun(ToolName, arguments, null, null, ExternalProcess.DefaultTimeoutMilliseconds, out _);
        }

        /// <inheritdoc/>
        public bool TryDelete(string account)
        {
            string arguments = $"delete-generic-password -s {ExternalProcess.Quote(_service)} -a {ExternalProcess.Quote(account)}";
            ExternalProcess.TryRun(ToolName, arguments, null, null, ExternalProcess.DefaultTimeoutMilliseconds, out _);
            return !TryRead(account, out _);
        }
    }
}
