### Building in CI

`reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run` runs the same pipeline as the Build menu in batch mode. It builds the platform given by `-buildTarget`, never shows a dialog or prompt, and throws when the build fails, so Unity exits with a non-zero code.

```bash
Unity -batchmode -quit -projectPath . -buildTarget Android \
  -executeMethod reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run \
  -buildType patch -buildOutput ../Artifacts
```

### Arguments

| Argument | Meaning |
| --- | --- |
| `-buildType` | Required: `patch`, `minor`, `major`, `exact` or `preview`. |
| `-buildOutput` | Root folder of all builds, replacing the Output Folder setting. The folder layout below it stays the same. |
| `-customBuildPath` | Exact player path, e.g. `build/Android/Game.aab` or a folder for WebGL, bypassing the folder layout and the replacement safeguard. On Android, `.aab` or `.apk` decides the package format. |
| `-buildVersion` | Exact version, e.g. `1.7.0` or `v1.7.0`, replacing the increment of the build type. `none` is ignored. |
| `-buildNumber` | Exact Android version code or macOS build number, replacing the automatic increment. |
| `-androidVersionCode` | GameCI's name for the version code; `-buildNumber` wins when both are given, and `0` is ignored. |
| `-androidKeystoreName` | Keystore file, relative to the project folder or absolute. Turns on custom keystore signing. |
| `-androidKeyaliasName` | Key alias name. |
| `-androidKeystorePass`, `-androidKeyaliasPass` | Passwords; the `ANDROID_KEYSTORE_PASS` and `ANDROID_KEYALIAS_PASS` environment variables win when set. |

Argument names ignore letter case. The newer GameCI option names `-androidKeyAlias`, `-androidKeystorePassword` and `-androidKeyAliasPassword` are understood too.

### Keeping versions in step

A release build in CI saves the new version into `ProjectSettings/ProjectSettings.asset` in the CI workspace only. Pick one of two approaches:

1. **The repository owns the version.** Build with `-buildType patch`, `minor` or `major`, then commit `ProjectSettings/ProjectSettings.asset` back, so the next build continues from the new version.
2. **CI owns the version.** Pass `-buildVersion` and `-buildNumber`, e.g. from a git tag and the run number, with `-buildType exact`. Nothing needs to be committed back.

### GameCI

The method works as GameCI's `buildMethod`. GameCI passes the target, the player path, the version and the keystore as arguments, and the package reads them:

```yaml
- uses: game-ci/unity-builder@v4
  env:
    UNITY_LICENSE: ${{ secrets.UNITY_LICENSE }}
    UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
    UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
  with:
    targetPlatform: Android
    buildMethod: reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run
    customParameters: -buildType exact
    versioning: Semantic
    androidExportType: androidAppBundle
    androidKeystoreName: release.keystore
    androidKeystoreBase64: ${{ secrets.ANDROID_KEYSTORE_BASE64 }}
    androidKeystorePass: ${{ secrets.ANDROID_KEYSTORE_PASS }}
    androidKeyaliasName: ${{ secrets.ANDROID_KEYALIAS_NAME }}
    androidKeyaliasPass: ${{ secrets.ANDROID_KEYALIAS_PASS }}
```

1. With `versioning: Semantic` or `Tag`, GameCI decides the version and version code, so use `-buildType exact`.
2. With `versioning: None`, GameCI passes no usable version, and the build type increments the version in Player Settings as in the editor.
3. GameCI decodes `androidKeystoreBase64` into the project folder under `androidKeystoreName`, which the package then signs with.

### Outputs

Every successful build writes `build-info.json` next to the player, see [USAGE.md](USAGE.md#build-infojson). When the `GITHUB_OUTPUT` environment variable reaches Unity, for example on a self-hosted runner that runs Unity directly, the build also sets these step outputs:

| Output | Example |
| --- | --- |
| `version` | `1.6.4` |
| `build-number` | `8`, empty when the build has none |
| `platform` | `Android` |
| `output-path` | Path of the player file or WebGL folder |
| `build-info-path` | Path of `build-info.json` |

```yaml
- name: Build
  id: build
  run: unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run -buildType patch

- name: Tag release
  run: git tag "v${{ steps.build.outputs.version }}"
```

GameCI runs Unity inside a Docker container, where `GITHUB_OUTPUT` may not be available; read `build-info.json` from the build folder there.
