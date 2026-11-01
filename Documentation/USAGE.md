### Usage

AutoBuildPipeline builds the active platform the way Unity's Build and Build And Run buttons do, without the save dialog, and adds versioning, signing, safeguards and a description of every build.

### Supported platforms

| Platform | Release package | Build number | Run after building |
| --- | --- | --- | --- |
| Android | App Bundle (`.aab`) or APK, see settings | Version code, incremented by releases | Installs and launches on the device selected in the build window |
| WebGL | Site folder | None | Serves the site locally and opens the browser |
| Windows | `.exe` with its data folder | None | Starts the player |
| macOS | `.app` | Build number (`CFBundleVersion`), incremented by releases | Starts the player |
| Linux | `.x86_64` with its data folder | None | Starts the player |

Other platforms show the build items disabled. Builds use the platform that is active in the **Build Settings** window (Unity 2021.3 and 2022.3) or the **Build Profiles** window (Unity 6), including a custom build profile with its own scenes and scripting defines.

### Versions

The version lives in **Player Settings ▸ Other Settings ▸ Version** and must be `major.minor.patch`, e.g. `1.6.3`; `1.6` and `v1.6.3` are read as `1.6.0` and `1.6.3`.

1. **Patch, Minor and Major Release** increment that part, reset the lower parts, and increment the build number: `1.6.3 (7)` becomes `1.6.4 (8)`, `1.7.0 (8)` or `2.0.0 (8)`. The new version is saved only when the build succeeds.
2. **Exact Release** builds the current version and build number unchanged. Use it to rebuild a release whose upload failed, after typing a version in by hand, or to ship the version you just released on one platform on another one.
3. **Preview** and **Preview and Run** build the upcoming patch with a pre-release label, the local build time and the short commit hash, e.g. `1.6.4-dev+261101.1432.3f2a9c1`. The project keeps its version and build number, and a preview respects the Development Build checkbox, while a release never is a development build.

Every platform shares the one version in Player Settings. To ship `1.6.4` on Android and WebGL, run **Patch Release** on Android, switch to WebGL and run **Exact Release**.

### Output folders

Builds go into the output folder, `Builds` inside the project folder by default, with a subfolder per platform and a folder per build:

```
Builds/
├─ Android/
│  ├─ com.company.app-1.6.4-8/
│  │  ├─ com.company.app-1.6.4-8.aab
│  │  └─ build-info.json
│  └─ com.company.app-dev/
│     ├─ com.company.app-dev.apk
│     └─ build-info.json
├─ WebGL/
│  └─ My Game-1.6.4/
│     ├─ My Game/        (index.html, Build/, TemplateData/)
│     └─ build-info.json
├─ Windows/
│  └─ My Game-1.6.4/     (My Game.exe, My Game_Data/, …, build-info.json)
├─ Windows-x86/
├─ macOS/
│  └─ My Game-1.6.4-8/   (My Game.app, build-info.json)
└─ Linux/
   └─ My Game-1.6.4/     (My Game.x86_64, My Game_Data/, …, build-info.json)
```

1. Android builds are named after the application identifier, every other platform after the product name. Characters that are invalid in file names on any system are removed.
2. Release folders carry the version and, where the platform has one, the build number. Previews reuse one `…-dev` folder, so they never pile up.
3. A build replaces its folder as a whole: the previous folder is moved aside to `….previous`, deleted when the new build succeeds, and put back when it fails or is cancelled. Close a running player before rebuilding into its folder.
4. The WebGL site sits in a folder of its own, so `build-info.json` is not published with it.
5. **Build ▸ Open Builds Folder** opens the active platform's folder.

### build-info.json

Every successful build gets a `build-info.json` next to the player:

```json
{
    "Product": "My Game",
    "Identifier": "com.company.app",
    "Platform": "Android",
    "Version": "1.6.4",
    "BuildNumber": 8,
    "IsRelease": true,
    "IsDevelopment": false,
    "Commit": "3f2a9c1e5b7d9f0a1c3e5b7d9f0a1c3e5b7d9f0a",
    "UnityVersion": "6000.6.7f1",
    "CreatedUtc": "2026-11-01T14:32:05Z",
    "Player": "com.company.app-1.6.4-8.aab",
    "SizeInBytes": 48210944
}
```

`BuildNumber` is `0` for previews and for platforms without one, and `Commit` is empty when the project is not in a git repository or `git` is not on the PATH.

### Settings

**Project Settings ▸ Auto Build Pipeline** is stored as text in `ProjectSettings/AutoBuildPipelineSettings.asset`, so commit it with the project and every machine and CI builds the same way. Until a setting changes, the file does not exist and the defaults apply.

| Setting | Default | Meaning |
| --- | --- | --- |
| Output Folder | `Builds` | Root folder of all builds. Relative paths start at the project folder, e.g. `../Builds` for a folder next to it. |
| Preview Label | `dev` | Pre-release label of previews: letters, digits, hyphens and dots, e.g. `beta.2`. |
| Confirm Releases | On | Asks before a release started from the editor, e.g. "Android release 1.6.3 (7) → 1.6.4 (8)?". |
| Show Builds After Building | On | Opens a build that is not launched in Explorer or Finder. |
| Android Release Format | App Bundle | App Bundles are required by Google Play; APKs suit other stores and direct installs. Previews are always APKs, which install faster. |

The page also shows the version and path of the next release and preview of the active platform, or the reason a build would fail, e.g. a version that is not `major.minor.patch`.

### What a build changes temporarily

Before anything changes, the build saves a restore point to `Library/AutoBuildPipeline/RestorePoint.json`. It covers the version, the build number, the Development Build checkbox and, on Android, the App Bundle and Export Project checkboxes and the keystore selection. After the build, everything is put back, except the new version of a successful release. If the editor crashes or is killed mid-build, the next editor start restores the settings and logs a warning.

Android's **Export Project** is turned off for the build, so a package is always produced. The Windows **Create Visual Studio Solution** and macOS **Create Xcode Project** options are not changed and must be off.

### Scripting

`AutoBuilder.Build` runs the same pipeline as the menu and returns Unity's `BuildReport`, or `null` when the user cancelled before the build started. It throws `BuildFailedException` when the build cannot start or does not succeed.

```csharp
using reromanlee.AutoBuildPipeline.Editor;
using UnityEditor;

public static class ReleaseTools
{
    [MenuItem("Tools/Release Minor and Run")]
    private static void ReleaseMinorAndRun()
    {
        AutoBuilder.Build(new BuildRequest(VersionIncrement.Minor, isRelease: true, isRunAfterBuild: true));
    }
}
```

`AutoBuildPipelineSettings.instance` reads the settings, and `ApplicationVersion` parses and increments versions.

### Troubleshooting

1. **"The version '…' in Player Settings is not in the 'major.minor.patch' format."** Set Player Settings ▸ Version to a version such as `1.0.0`.
2. **"The active platform … is not supported."** Switch to Android, WebGL or a Standalone platform in the build window.
3. **"Could not replace '…'."** A file in the build folder is in use, usually by a running player; close it and build again.
4. **"Android releases must be signed with a custom keystore."** Select a keystore in Player Settings ▸ Publishing Settings, see [SIGNING.md](SIGNING.md).
5. **The build menu items are disabled.** A build is running, scripts are compiling, the editor is in Play Mode, or the active platform is not supported.
