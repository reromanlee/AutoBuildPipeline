using System;
using System.Runtime.InteropServices;
using FileTime = System.Runtime.InteropServices.ComTypes.FILETIME;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Stores secrets in Windows Credential Manager, encrypted with DPAPI for the current Windows user.
    /// Entries are visible under Control Panel ▸ Credential Manager ▸ Windows Credentials.
    /// </summary>
    internal sealed class WindowsCredentialStore : ICredentialStore
    {
        private const uint GenericCredentialType = 1;
        private const uint PersistLocalMachine = 2;
        private const int NotFoundError = 1168;

        private readonly string _service;

        public WindowsCredentialStore(string service)
        {
            _service = service;
        }

        /// <inheritdoc/>
        public string DisplayName
        {
            get
            {
                return "Windows Credential Manager";
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
            if (!CredRead(GetTarget(account), GenericCredentialType, 0, out IntPtr credentialHandle))
            {
                LogUnexpectedError("read", Marshal.GetLastWin32Error());
                return false;
            }

            try
            {
                NativeCredential credential = Marshal.PtrToStructure<NativeCredential>(credentialHandle);
                if (credential.CredentialBlob == IntPtr.Zero || credential.CredentialBlobSize == 0)
                {
                    return false;
                }

                secret = Marshal.PtrToStringUni(credential.CredentialBlob, (int)credential.CredentialBlobSize / sizeof(char));
                return true;
            }
            finally
            {
                CredFree(credentialHandle);
            }
        }

        /// <inheritdoc/>
        public bool TryWrite(string account, string secret)
        {
            IntPtr secretHandle = Marshal.StringToCoTaskMemUni(secret);
            try
            {
                NativeCredential credential = new()
                {
                    Type = GenericCredentialType,
                    TargetName = GetTarget(account),
                    UserName = account,
                    CredentialBlob = secretHandle,
                    CredentialBlobSize = (uint)(secret.Length * sizeof(char)),
                    Persist = PersistLocalMachine
                };

                if (!CredWrite(ref credential, 0))
                {
                    LogUnexpectedError("write", Marshal.GetLastWin32Error());
                    return false;
                }

                return true;
            }
            finally
            {
                // Wipes the unmanaged copy of the secret before releasing it.
                Marshal.ZeroFreeCoTaskMemUnicode(secretHandle);
            }
        }

        /// <inheritdoc/>
        public bool TryDelete(string account)
        {
            if (CredDelete(GetTarget(account), GenericCredentialType, 0))
            {
                return true;
            }

            int error = Marshal.GetLastWin32Error();
            LogUnexpectedError("delete", error);
            return error == NotFoundError;
        }

        private string GetTarget(string account)
        {
            return _service + "/" + account;
        }

        private static void LogUnexpectedError(string operation, int error)
        {
            if (error != NotFoundError)
            {
                BuildLog.Warning($"Windows Credential Manager failed to {operation} a credential (error {error}).");
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string TargetName;
            public string Comment;
            public FileTime LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string TargetAlias;
            public string UserName;
        }

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredWrite(ref NativeCredential credential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll")]
        private static extern void CredFree(IntPtr buffer);
    }
}
