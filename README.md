### About AutoBuildPipeline

One-click release and preview builds for Unity, straight from a **Build** menu, for Android, WebGL, Windows, macOS and Linux.

Choose **Build ▸ Patch Release** and the package increments the version and build number, signs Android builds with passwords kept in your operating system's credential vault, builds the active platform into a folder of its own, writes a `build-info.json` next to it, and saves the new version only when the build succeeds. **Build ▸ Preview and Run** builds a preview with a version you can recognize on the device, such as `1.6.4-dev+261101.1432.3f2a9c1`, and launches it while the project stays untouched.

It is built to be safe to click. Versions, paths and passwords are resolved and checked before any setting changes, every temporary setting is restored afterwards, even after an editor crash, and a failed build puts the previous build back. The same pipeline runs in CI through one command line entry point that also understands GameCI's arguments.

### Features

1. Build menu for the active platform
2. Patch, minor, major and exact releases
3. Previews with recognizable versions
4. Android, WebGL, Windows, macOS, Linux
5. Automatic version codes and build numbers
6. Android signing from the credential vault
7. Crash-safe restore of temporary settings
8. Failed builds restore the previous build
9. `build-info.json` next to every build
10. One command line entry point for CI
11. GameCI arguments and GitHub outputs
12. Settings page that previews the next build

Detailed about features - see [USAGE.md](Documentation/USAGE.md). How Android signing works - see [SIGNING.md](Documentation/SIGNING.md). How to build in CI - see [CI.md](Documentation/CI.md). How the package is tested - see [TESTING.md](Documentation/TESTING.md).

### Requirements

1. Unity 2021.3 LTS or newer, including Unity 6.
2. The build support module of each platform you build.
3. No package dependencies; `git` on the PATH is optional and adds commit hashes.

### Limitations

1. Builds the active platform; switch platforms first.
2. iOS and consoles are not supported yet.
3. One version is shared by every platform.
4. Folder and file names follow a fixed layout.
5. Solution and Xcode project outputs must be off.

### How to install

1. **Git URL.** In Unity, open **Window ▸ Package Manager ▸ + ▸ Add package from git URL…** and enter `https://github.com/reromanlee/AutoBuildPipeline.git?path=/UnityPackage#1.0.0`. Drop `#1.0.0` to track the latest `main` instead of a release.
2. **Manifest.** Add the same URL to `Packages/manifest.json`:

   ```json
   {
     "dependencies": {
       "com.reromanlee.autobuildpipeline": "https://github.com/reromanlee/AutoBuildPipeline.git?path=/UnityPackage#1.0.0"
     }
   }
   ```

3. **Tarball.** Download `com.reromanlee.autobuildpipeline-1.0.0.tgz` from the [latest release](https://github.com/reromanlee/AutoBuildPipeline/releases/latest), then choose **+ ▸ Add package from tarball…** in the Package Manager.

### How to use

**Set the version once, then build from the menu.** Give the project a `major.minor.patch` version in **Player Settings ▸ Other Settings**, e.g. `1.0.0`. Every item of the **Build** menu acts on the active platform:

| Menu item | Version, e.g. from `1.6.3 (7)` | Result |
| --- | --- | --- |
| Patch Release | `1.6.4 (8)`, saved | `Builds/Android/com.company.app-1.6.4-8/com.company.app-1.6.4-8.aab` |
| Minor Release | `1.7.0 (8)`, saved | Release folder named after the new version |
| Major Release | `2.0.0 (8)`, saved | Release folder named after the new version |
| Exact Release | `1.6.3 (7)`, unchanged | Rebuilds the current version, e.g. for a second platform |
| Preview and Run | `1.6.4-dev+261101.1432`, temporary | Builds into `…-dev` and launches it |
| Preview | `1.6.4-dev+261101.1432`, temporary | Builds into `…-dev` without launching |

The number in parentheses is the Android version code or the macOS build number; Windows, Linux and WebGL have none. Releases ask for confirmation first and are never development builds. **Build ▸ Settings…** opens **Project Settings ▸ Auto Build Pipeline**, which sets the output folder, the preview label and the Android release format, and shows where the next release and preview will go. Any menu item can be bound to a key in **Edit ▸ Shortcuts ▸ Main Menu ▸ Build**.

**Android releases need a custom keystore.** Select it in **Player Settings ▸ Publishing Settings**. The first release asks for the passwords once and can remember them in Windows Credential Manager, the macOS Keychain or the Linux Secret Service keyring, so they never reach the project files.

**Build in CI with one method.** It builds the platform given by `-buildTarget`, never shows a dialog, and exits with a non-zero code when the build fails:

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android \
  -executeMethod reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run -buildType patch
```

**Build from your own scripts** with the same pipeline:

```csharp
using reromanlee.AutoBuildPipeline.Editor;

AutoBuilder.Build(BuildRequest.PreviewAndRun);
```

Detailed about usage - see [USAGE.md](Documentation/USAGE.md).

### License

Made by Roman Likhadievski (reromanlee.com).

Licensed under MIT - see [LICENSE.md](LICENSE.md).
