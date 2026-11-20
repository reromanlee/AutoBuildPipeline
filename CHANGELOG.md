# AutoBuildPipeline (Unreleased)

### What's new

1. Configurable release folder, preview folder and player file name patterns in Project Settings with tokens (`{name}`, `{product}`, `{identifier}`, `{version}`, `{code}`, `{platform}`, `{label}`, `{date}`, `{time}`), cross-platform sanitization and collision checks.
2. Configurable Android native debug symbols for releases (`Off`, `Public` or `Debugging`, defaulting to `Public`) across Unity 2021.3, 2022.3 and Unity 6, with crash-safe restore afterwards.
3. `AutoBuilder.CurrentBuild` (`BuildContext`) and `AutoBuilder.CurrentRequest` so Unity build callbacks (`IPreprocessBuildWithReport`, `IPostprocessBuildWithReport`) can inspect the active build, version, build number and output folder.
4. **Build ▸ Switch Platform** submenu for Android, WebGL, Windows, macOS and Linux with active-platform checkmarks, installed-module checks and confirmation before switching.
5. GitHub Actions CI upgraded with a dedicated test project covering Unity 2021.3, 2022.3, 6000.0 and 6000.6, clean pass/fail check reporting with step summaries, C# warning checks, tree-SHA gating and nightly cache/artifact cleanup.

### Bugfixes

1. Android release builds (`.apk` and `.aab`) now build with Unity's debug keystore when Custom Keystore is disabled in Player Settings, logging a warning (and noting Google Play rejects debug-signed App Bundles) instead of blocking projects without a keystore.

# AutoBuildPipeline 1.0.0

### What's new

1. Top-level **Build** menu for the active platform: Patch, Minor, Major and Exact Release, Preview and Run, Preview, Open Builds Folder, Keystore Passwords, Forget Keystore Passwords and Settings, each bindable to a key in Edit ▸ Shortcuts.
2. Releases increment the `major.minor.patch` version and the build number, ask for confirmation showing the version change, are never development builds, and save the new version only when the build succeeds.
3. Previews build the upcoming version with a label, build time and commit hash, e.g. `1.6.4-dev+261101.1432.3f2a9c1`, and leave the project's version untouched.
4. Android (App Bundle or APK, version code), WebGL, Windows, macOS (build number) and Linux, with Unity 6 custom build profiles honored.
5. Android signing with passwords from environment variables, command line arguments, Windows Credential Manager, the macOS Keychain or the Linux Secret Service keyring, entered once per machine.
6. Every build gets a folder of its own, `Builds/<Platform>/<name>-<version>[-<build number>]`, which is replaced as a whole and restored when a build fails or is cancelled.
7. Crash-safe restore point that reverts every temporary setting on the next editor start if the editor dies mid-build.
8. `build-info.json` next to every build with version, build number, platform, commit, Unity version, time and size.
9. `CommandLineBuild.Run` entry point for batch mode and CI with `-buildType`, output, version and build number overrides, GameCI's arguments, GitHub Actions step outputs and a non-zero exit code on failure.
10. Project Settings ▸ Auto Build Pipeline page for the output folder, preview label, release confirmation, revealing builds and the Android release format, previewing where the next release and preview go.
11. `AutoBuilder.Build` and `BuildRequest` for running the same pipeline from your own editor scripts.
12. Edit Mode tests, explicit smoke builds of real players, and a GameCI workflow on Unity 2021.3, 2022.3 and 6000.0.
13. Unity 2021.3 LTS, 2022.3 LTS and Unity 6 support.

### Known issues

1. iOS and console platforms are not supported yet.
2. Windows "Create Visual Studio Solution" and macOS "Create Xcode Project" must be turned off; they are not changed for the build.
3. Not tested with Unity 6 build profiles that override Player Settings.

### Important

1. The package lives in the `UnityPackage/` folder of the repository, so git URL installs must end with `?path=/UnityPackage`.
2. The version in Player Settings must be in the `major.minor.patch` format, e.g. `1.0.0`.
