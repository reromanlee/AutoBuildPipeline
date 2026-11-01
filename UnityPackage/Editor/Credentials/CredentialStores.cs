using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Picks the credential vault of the operating system the editor is running on.
    /// </summary>
    internal static class CredentialStores
    {
        /// <summary>
        /// Name every secret of the package is grouped under in the vault.
        /// </summary>
        public const string ServiceName = "reromanlee.AutoBuildPipeline";

        private static ICredentialStore _current;

        /// <summary>
        /// Credential vault for the current operating system, created on first use.
        /// </summary>
        public static ICredentialStore Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Create(ServiceName);
                }

                return _current;
            }
        }

        /// <summary>
        /// Creates the credential vault for the current operating system; secrets are grouped under the service name.
        /// </summary>
        public static ICredentialStore Create(string service)
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsEditor:
                {
                    return new WindowsCredentialStore(service);
                }
                case RuntimePlatform.OSXEditor:
                {
                    return new MacKeychainCredentialStore(service);
                }
                case RuntimePlatform.LinuxEditor:
                {
                    return new LinuxSecretCredentialStore(service);
                }
                default:
                {
                    return new UnavailableCredentialStore();
                }
            }
        }

        private sealed class UnavailableCredentialStore : ICredentialStore
        {
            public string DisplayName
            {
                get
                {
                    return "no credential vault";
                }
            }

            public bool IsPersistent
            {
                get
                {
                    return false;
                }
            }

            public bool TryRead(string account, out string secret)
            {
                secret = null;
                return false;
            }

            public bool TryWrite(string account, string secret)
            {
                return false;
            }

            public bool TryDelete(string account)
            {
                return true;
            }
        }
    }
}
