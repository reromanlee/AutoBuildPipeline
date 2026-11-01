namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Persists secrets in the operating system's credential vault for the current user.
    /// </summary>
    internal interface ICredentialStore
    {
        /// <summary>
        /// Human readable vault name, e.g. "Windows Credential Manager".
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Whether secrets survive editor restarts.
        /// </summary>
        bool IsPersistent { get; }

        /// <summary>
        /// Reads the secret of the account; returns false when it does not exist or the vault is unavailable.
        /// </summary>
        bool TryRead(string account, out string secret);

        /// <summary>
        /// Creates or replaces the secret of the account; returns false when the vault rejected it.
        /// </summary>
        bool TryWrite(string account, string secret);

        /// <summary>
        /// Removes the secret; returns true when it is gone, including when it never existed.
        /// </summary>
        bool TryDelete(string account);
    }
}
