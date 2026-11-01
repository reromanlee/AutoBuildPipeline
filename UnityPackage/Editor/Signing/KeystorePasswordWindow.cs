using UnityEditor;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Modal prompt for keystore and key alias passwords.
    /// </summary>
    internal sealed class KeystorePasswordWindow : EditorWindow
    {
        private const float ButtonWidth = 90f;

        private static readonly Vector2 WindowSize = new(440f, 176f);

        private string _keystoreName;
        private string _aliasName;
        private string _storeName;
        private bool _isStorePersistent;

        private string _keystorePassword = string.Empty;
        private string _aliasPassword = string.Empty;
        private bool _isAliasSameAsKeystore = true;
        private bool _isRemembered = true;
        private bool _isConfirmed;
        private bool _isFocusPending = true;

        /// <summary>
        /// Blocks until the user confirms or cancels the prompt.
        /// </summary>
        /// <param name="initialCredentials">Passwords to prefill, e.g. the ones already typed into Player Settings.</param>
        /// <param name="isRemembered">Whether the user asked to keep the passwords in the credential vault.</param>
        /// <returns>False when the user cancelled.</returns>
        public static bool TryPrompt(string keystoreName, string aliasName, ICredentialStore store, KeystoreCredentials initialCredentials, out KeystoreCredentials credentials, out bool isRemembered)
        {
            KeystorePasswordWindow window = CreateInstance<KeystorePasswordWindow>();
            window.titleContent = new GUIContent("Keystore Passwords");
            window._keystoreName = keystoreName;
            window._aliasName = aliasName;
            window._storeName = store.DisplayName;
            window._isStorePersistent = store.IsPersistent;
            window._isRemembered = store.IsPersistent;
            window._keystorePassword = initialCredentials.KeystorePassword ?? string.Empty;
            window._aliasPassword = initialCredentials.AliasPassword ?? string.Empty;
            window._isAliasSameAsKeystore = window._aliasPassword.Length == 0 || window._aliasPassword == window._keystorePassword;
            window.minSize = WindowSize;
            window.maxSize = WindowSize;

            // The managed fields stay readable after the window closes and its native side is destroyed.
            window.ShowModalUtility();

            credentials = window.GetCredentials();
            isRemembered = window._isRemembered && window._isStorePersistent;
            return window._isConfirmed && credentials.IsComplete;
        }

        private KeystoreCredentials GetCredentials()
        {
            return new KeystoreCredentials(_keystorePassword, _isAliasSameAsKeystore ? _keystorePassword : _aliasPassword);
        }

        private void OnGUI()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.KeyDown)
            {
                if (currentEvent.keyCode == KeyCode.Escape)
                {
                    Close();
                    return;
                }

                if ((currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter) && GetCredentials().IsComplete)
                {
                    Confirm();
                    return;
                }
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Keystore", _keystoreName);
            EditorGUILayout.LabelField("Key Alias", _aliasName);
            EditorGUILayout.Space(4f);

            GUI.SetNextControlName(nameof(_keystorePassword));
            _keystorePassword = EditorGUILayout.PasswordField("Keystore Password", _keystorePassword);
            _isAliasSameAsKeystore = EditorGUILayout.Toggle("Alias Uses Same Password", _isAliasSameAsKeystore);
            using (new EditorGUI.DisabledScope(_isAliasSameAsKeystore))
            {
                _aliasPassword = EditorGUILayout.PasswordField("Alias Password", _isAliasSameAsKeystore ? _keystorePassword : _aliasPassword);
            }

            using (new EditorGUI.DisabledScope(!_isStorePersistent))
            {
                string rememberLabel = _isStorePersistent ? $"Remember on this machine ({_storeName})" : "Remember on this machine (no credential vault found)";
                _isRemembered = EditorGUILayout.ToggleLeft(rememberLabel, _isRemembered);
            }

            GUILayout.FlexibleSpace();
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(ButtonWidth)))
                {
                    Close();
                }

                using (new EditorGUI.DisabledScope(!GetCredentials().IsComplete))
                {
                    if (GUILayout.Button("Continue", GUILayout.Width(ButtonWidth)))
                    {
                        Confirm();
                    }
                }
            }

            EditorGUILayout.Space(6f);

            if (_isFocusPending)
            {
                _isFocusPending = false;
                EditorGUI.FocusTextInControl(nameof(_keystorePassword));
            }
        }

        private void Confirm()
        {
            _isConfirmed = true;
            Close();
        }
    }
}
