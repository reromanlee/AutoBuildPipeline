using System;
using System.Text;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Keystore and key alias passwords, serialized together so they are stored and read as a single vault entry.
    /// </summary>
    internal readonly struct KeystoreCredentials
    {
        // Not part of the Base64 alphabet, so it can never appear inside an encoded password.
        private const char Separator = '.';

        public readonly string KeystorePassword;
        public readonly string AliasPassword;

        public KeystoreCredentials(string keystorePassword, string aliasPassword)
        {
            KeystorePassword = keystorePassword;
            AliasPassword = aliasPassword;
        }

        /// <summary>
        /// Whether both passwords are present.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                return !string.IsNullOrEmpty(KeystorePassword) && !string.IsNullOrEmpty(AliasPassword);
            }
        }

        /// <summary>
        /// Uses the keystore password for the alias when no alias password is given, as most keystores share one password.
        /// Returns <see langword="default"/> when the keystore password is missing.
        /// </summary>
        public static KeystoreCredentials FromPasswords(string keystorePassword, string aliasPassword)
        {
            if (string.IsNullOrEmpty(keystorePassword))
            {
                return default;
            }

            return new KeystoreCredentials(keystorePassword, string.IsNullOrEmpty(aliasPassword) ? keystorePassword : aliasPassword);
        }

        /// <summary>
        /// Encodes both passwords as <c>base64.base64</c>, keeping the stored value free of quotes and line breaks.
        /// </summary>
        public string Serialize()
        {
            return Encode(KeystorePassword) + Separator + Encode(AliasPassword);
        }

        /// <summary>
        /// Decodes a value written by <see cref="Serialize"/>; returns false for malformed or incomplete values.
        /// </summary>
        public static bool TryDeserialize(string text, out KeystoreCredentials credentials)
        {
            credentials = default;
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            int separatorIndex = text.IndexOf(Separator);
            if (separatorIndex <= 0 || separatorIndex == text.Length - 1)
            {
                return false;
            }

            try
            {
                credentials = new KeystoreCredentials(Decode(text.Substring(0, separatorIndex)), Decode(text.Substring(separatorIndex + 1)));
                return credentials.IsComplete;
            }
            catch (FormatException)
            {
                credentials = default;
                return false;
            }
        }

        private static string Encode(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }
    }
}
