using System;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Stores secrets in the Secret Service keyring (GNOME Keyring, KWallet) through <c>secret-tool</c> from libsecret.
    /// </summary>
    internal sealed class LinuxSecretCredentialStore : ICredentialStore
    {
        private const string ToolName = "secret-tool";

        private readonly string _service;

        public LinuxSecretCredentialStore(string service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public string DisplayName
        {
            get
            {
                return "Secret Service keyring";
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
            if (!ExternalProcess.TryRun(ToolName, "lookup " + GetAttributes(account), null, null, ExternalProcess.DefaultTimeoutMilliseconds, out string output) ||
                string.IsNullOrEmpty(output))
            {
                return false;
            }

            secret = output.EndsWith("\n", StringComparison.Ordinal) ? output.Substring(0, output.Length - 1) : output;
            return secret.Length > 0;
        }

        /// <inheritdoc/>
        public bool TryWrite(string account, string secret)
        {
            // The secret goes through standard input, so it never appears in process listings.
            string arguments = $"store --label={ExternalProcess.Quote(_service + " " + account)} {GetAttributes(account)}";
            return ExternalProcess.TryRun(ToolName, arguments, secret, null, ExternalProcess.DefaultTimeoutMilliseconds, out _);
        }

        /// <inheritdoc/>
        public bool TryDelete(string account)
        {
            ExternalProcess.TryRun(ToolName, "clear " + GetAttributes(account), null, null, ExternalProcess.DefaultTimeoutMilliseconds, out _);
            return !TryRead(account, out _);
        }

        private string GetAttributes(string account)
        {
            return $"service {ExternalProcess.Quote(_service)} account {ExternalProcess.Quote(account)}";
        }
    }
}
