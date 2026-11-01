### Android signing

Android releases must be signed with your own keystore, because Google Play and every other store identify an app by its signing key. Previews may use Unity's debug keystore.

### Setting up the keystore

1. Create or select a keystore in **Player Settings ▸ Publishing Settings ▸ Keystore Manager**, then pick the key alias.
2. Keep the keystore out of the repository's `Assets` folder. A path relative to the project folder works, e.g. `../app.keystore` for a keystore next to it, and so do Unity's own keystore locations.

AutoBuildPipeline never writes passwords into the project. Unity keeps the passwords in Player Settings in memory only, and the package fills them in right before each build.

### Where passwords come from

For every signed build, the package looks for the passwords in this order and uses the first complete pair:

1. **Environment variables** `ANDROID_KEYSTORE_PASS` and `ANDROID_KEYALIAS_PASS`. The alias password defaults to the keystore password when only the first is set.
2. **Command line arguments** `-androidKeystorePass` and `-androidKeyaliasPass`, the names GameCI passes. Prefer environment variables, because arguments can be visible to other processes on the same machine.
3. **The operating system's credential vault**, filled once per machine.
4. **The passwords typed into Player Settings** during the current editor session.
5. **A prompt** that asks for the passwords and can remember them in the credential vault. Batch mode never prompts; it fails with a message naming the environment variables instead.

### The credential vault

The prompt's **Remember on this machine** option stores both passwords as one entry, encrypted by the operating system for the current user:

| Editor | Vault | Entry |
| --- | --- | --- |
| Windows | Windows Credential Manager | `reromanlee.AutoBuildPipeline/<keystore file>/<alias>` under Windows Credentials |
| macOS | Login Keychain, through `/usr/bin/security` | Service `reromanlee.AutoBuildPipeline`, account `<keystore file>/<alias>` |
| Linux | Secret Service keyring (GNOME Keyring, KWallet), through `secret-tool` from libsecret | Attributes `service` and `account` as on macOS |

Entries are keyed by the keystore's file name and the alias, so moving the keystore file keeps its passwords. On Linux without `secret-tool`, the option is disabled and passwords last for the editor session only.

1. **Build ▸ Keystore Passwords…** asks for the passwords again and replaces the stored entry, e.g. after a password change.
2. **Build ▸ Forget Keystore Passwords** removes the stored entry and clears the passwords from the editor session.

Both items are enabled while Android is the active platform.

### Security notes

1. On Windows, passwords pass through the Credential Manager API in memory, and the unmanaged copy is wiped after writing.
2. On Linux, the secret is written to `secret-tool` through standard input, so it never appears in process listings.
3. On macOS, `security` accepts the secret only as an argument, so it is visible to local process listings for the few milliseconds the tool runs.
4. When a signed release fails, the error message points to **Build ▸ Keystore Passwords…**, because a wrong stored password is the most common cause.

### Signing in CI

See [CI.md](CI.md) for passing the keystore and its passwords to a CI build.
