namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Reads the commit a project is built from through the <c>git</c> command line tool.
    /// </summary>
    internal static class GitRepository
    {
        private const int TimeoutMilliseconds = 5000;

        /// <summary>
        /// Reads the full hash of the checked out commit, e.g. for <c>build-info.json</c>.
        /// Returns false when git is not installed or the folder is not inside a repository.
        /// </summary>
        public static bool TryReadCommit(string folder, out string commit)
        {
            commit = null;
            if (!ExternalProcess.TryRun("git", "rev-parse HEAD", null, folder, TimeoutMilliseconds, out string output) ||
                string.IsNullOrEmpty(output))
            {
                return false;
            }

            string hash = output.Trim().ToLowerInvariant();
            if (!IsCommitHash(hash))
            {
                return false;
            }

            commit = hash;
            return true;
        }

        /// <summary>
        /// Whether the text is a SHA-1 or SHA-256 commit hash in lowercase hexadecimal.
        /// </summary>
        public static bool IsCommitHash(string text)
        {
            if (text == null || (text.Length != 40 && text.Length != 64))
            {
                return false;
            }

            foreach (char character in text)
            {
                bool isHexadecimal = (character >= '0' && character <= '9') || (character >= 'a' && character <= 'f');
                if (!isHexadecimal)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
