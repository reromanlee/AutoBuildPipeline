using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Supplies the keystore and its passwords to Player Settings right before an Android build.
    /// Passwords are looked up in this order:
    /// 1. <c>ANDROID_KEYSTORE_PASS</c> and <c>ANDROID_KEYALIAS_PASS</c> environment variables (CI).
    /// 2. Command line arguments (CI, e.g. GameCI).
    /// 3. The operating system's credential vault (entered once per machine).
    /// 4. Passwords already typed into Player Settings during this editor session.
    /// 5. A modal prompt, which can save them into the credential vault.
    /// </summary>
    internal static class AndroidSigning
    {
        public const string KeystorePasswordVariable = "ANDROID_KEYSTORE_PASS";
        public const string AliasPasswordVariable = "ANDROID_KEYALIAS_PASS";

        // Location markers Unity writes into ProjectSettings.asset for keystores that are not stored by absolute path.
        private const string ProjectLocationPrefix = "{inproject}:";
        private const string DedicatedLocationPrefix = "{dedicated}:";
        private const string DedicatedLocationRootPreference = "AndroidKeystoresDedicatedLocationRoot";

        /// <summary>
        /// Makes sure Player Settings hold an absolute keystore path and the passwords for the upcoming build.
        /// Must run inside a restore point, because the keystore path and overrides change Player Settings.
        /// </summary>
        /// <param name="isRelease">Release builds fail when the debug keystore is used.</param>
        /// <returns>False when the user cancelled the password prompt.</returns>
        /// <exception cref="BuildFailedException">The keystore or its passwords are missing.</exception>
        public static bool TryPrepare(bool isRelease, BuildOverrides overrides)
        {
            if (!string.IsNullOrWhiteSpace(overrides.KeystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = overrides.KeystorePath.Trim();
            }

            if (!string.IsNullOrWhiteSpace(overrides.AliasName))
            {
                PlayerSettings.Android.keyaliasName = overrides.AliasName.Trim();
            }

            if (!PlayerSettings.Android.useCustomKeystore)
            {
                if (isRelease)
                {
                    throw new BuildFailedException("Android releases must be signed with a custom keystore. Enable it in Player Settings ▸ Publishing Settings.");
                }

                return true;
            }

            // Gradle runs from its own folder, so a relative keystore path is resolved up front.
            string keystorePath = ResolveKeystorePath();
            PlayerSettings.Android.keystoreName = keystorePath;

            string aliasName = GetAliasName();
            if (!TryResolveCredentials(keystorePath, aliasName, overrides, out KeystoreCredentials credentials))
            {
                return false;
            }

            Apply(credentials);
            return true;
        }

        /// <summary>
        /// Prompts for the passwords and saves them into the credential vault, replacing any stored ones.
        /// </summary>
        /// <exception cref="BuildFailedException">No custom keystore or key alias is selected.</exception>
        public static void PromptAndRemember()
        {
            string keystorePath = ResolveKeystorePath();
            string aliasName = GetAliasName();
            ICredentialStore store = CredentialStores.Current;
            KeystoreCredentials sessionCredentials = GetSessionCredentials();
            if (!KeystorePasswordWindow.TryPrompt(Path.GetFileName(keystorePath), aliasName, store, sessionCredentials, out KeystoreCredentials credentials, out bool isRemembered))
            {
                return;
            }

            Apply(credentials);
            if (isRemembered)
            {
                Remember(store, keystorePath, aliasName, credentials);
            }
        }

        /// <summary>
        /// Removes the stored passwords of the configured keystore from the credential vault and this editor session.
        /// </summary>
        /// <exception cref="BuildFailedException">No custom keystore or key alias is selected.</exception>
        public static void Forget()
        {
            ICredentialStore store = CredentialStores.Current;
            string account = GetAccount(ResolveKeystorePath(), GetAliasName());
            Apply(new KeystoreCredentials(string.Empty, string.Empty));
            if (store.TryDelete(account))
            {
                BuildLog.Info($"Keystore passwords for '{account}' were removed from {store.DisplayName}.");
            }
            else
            {
                BuildLog.Warning($"Could not remove keystore passwords for '{account}' from {store.DisplayName}.");
            }
        }

        /// <summary>
        /// Absolute path of the keystore selected in Player Settings. Relative paths start at the project folder,
        /// e.g. <c>../app.keystore</c> for a keystore next to the project folder. Unity's own location prefixes are
        /// understood too: <c>{inproject}: ../app.keystore</c> and <c>{dedicated}: app.keystore</c>.
        /// </summary>
        /// <exception cref="BuildFailedException">No keystore is selected or the file does not exist.</exception>
        public static string ResolveKeystorePath()
        {
            string keystoreName = PlayerSettings.Android.keystoreName;
            if (string.IsNullOrWhiteSpace(keystoreName))
            {
                throw new BuildFailedException("No custom keystore is selected in Player Settings ▸ Publishing Settings.");
            }

            string rootPath = ProjectPaths.ProjectFolder;
            if (TryRemovePrefix(ref keystoreName, DedicatedLocationPrefix))
            {
                rootPath = EditorPrefs.GetString(DedicatedLocationRootPreference, string.Empty);
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    throw new BuildFailedException("The keystore is in Unity's dedicated keystore folder, but that folder is not set in Preferences ▸ External Tools.");
                }
            }
            else
            {
                TryRemovePrefix(ref keystoreName, ProjectLocationPrefix);
            }

            string keystorePath = Path.GetFullPath(Path.Combine(rootPath, keystoreName));
            if (!File.Exists(keystorePath))
            {
                throw new BuildFailedException($"Keystore file was not found at '{keystorePath}'.");
            }

            return keystorePath;
        }

        private static bool TryResolveCredentials(string keystorePath, string aliasName, BuildOverrides overrides, out KeystoreCredentials credentials)
        {
            credentials = KeystoreCredentials.FromPasswords(
                Environment.GetEnvironmentVariable(KeystorePasswordVariable),
                Environment.GetEnvironmentVariable(AliasPasswordVariable));
            if (credentials.IsComplete)
            {
                return true;
            }

            credentials = KeystoreCredentials.FromPasswords(overrides.KeystorePassword, overrides.AliasPassword);
            if (credentials.IsComplete)
            {
                return true;
            }

            ICredentialStore store = CredentialStores.Current;
            string account = GetAccount(keystorePath, aliasName);
            if (store.TryRead(account, out string secret) && KeystoreCredentials.TryDeserialize(secret, out credentials))
            {
                return true;
            }

            KeystoreCredentials sessionCredentials = GetSessionCredentials();
            if (sessionCredentials.IsComplete)
            {
                credentials = sessionCredentials;
                return true;
            }

            if (overrides.IsUnattended || Application.isBatchMode)
            {
                throw new BuildFailedException($"Keystore passwords are missing. Set the {KeystorePasswordVariable} and {AliasPasswordVariable} environment variables.");
            }

            if (!KeystorePasswordWindow.TryPrompt(Path.GetFileName(keystorePath), aliasName, store, sessionCredentials, out credentials, out bool isRemembered))
            {
                return false;
            }

            if (isRemembered)
            {
                Remember(store, keystorePath, aliasName, credentials);
            }

            return true;
        }

        private static KeystoreCredentials GetSessionCredentials()
        {
            return new KeystoreCredentials(PlayerSettings.Android.keystorePass, PlayerSettings.Android.keyaliasPass);
        }

        private static void Remember(ICredentialStore store, string keystorePath, string aliasName, in KeystoreCredentials credentials)
        {
            string account = GetAccount(keystorePath, aliasName);
            if (store.TryWrite(account, credentials.Serialize()))
            {
                BuildLog.Info($"Keystore passwords for '{account}' were saved to {store.DisplayName}.");
            }
            else
            {
                BuildLog.Warning($"Could not save keystore passwords to {store.DisplayName}; they are kept for this editor session only.");
            }
        }

        private static void Apply(in KeystoreCredentials credentials)
        {
            // Unity keeps these in memory only and never writes them into ProjectSettings.asset.
            PlayerSettings.Android.keystorePass = credentials.KeystorePassword;
            PlayerSettings.Android.keyaliasPass = credentials.AliasPassword;
        }

        /// <summary>
        /// Vault entries are keyed by keystore file name and alias, so moving the keystore file keeps its passwords.
        /// </summary>
        private static string GetAccount(string keystorePath, string aliasName)
        {
            return Path.GetFileName(keystorePath) + "/" + aliasName;
        }

        private static bool TryRemovePrefix(ref string keystoreName, string prefix)
        {
            if (!keystoreName.StartsWith(prefix, StringComparison.Ordinal))
            {
                return false;
            }

            keystoreName = keystoreName.Substring(prefix.Length).Trim();
            return true;
        }

        private static string GetAliasName()
        {
            string aliasName = PlayerSettings.Android.keyaliasName;
            if (string.IsNullOrWhiteSpace(aliasName))
            {
                throw new BuildFailedException("No key alias is selected in Player Settings ▸ Publishing Settings.");
            }

            return aliasName;
        }
    }
}
