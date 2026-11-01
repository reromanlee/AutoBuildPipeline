### Testing

The package ships its test suite in `UnityPackage/Tests`. It compiles only in projects that opt in, so it costs nothing otherwise.

### What is covered

1. **Edit Mode tests** (`Tests/Editor`): version parsing and increments, preview versions and labels, build planning for every platform (versions, build numbers, limits, folder and file names, package formats, overrides), command line parsing including GameCI's arguments, file name sanitizing, relative paths, keystore credential encoding, the folder backup on success, failure and interrupted builds, the restore point, `build-info.json` and GitHub outputs.
2. **Smoke builds** (`Tests/Editor/Builds`, category `Build`): real Standalone players of an empty scene for the editor's own operating system, built through the whole pipeline, checking that previews keep the version, releases save it, every build gets `build-info.json`, and a rebuild replaces the previous folder.

For 1.0.0, the Edit Mode tests passed on Unity 2021.3.45f2, 2022.3.62f3, 6000.0.85f1 and 6000.6.7f1, and the smoke builds passed on 2021.3.45f2 and 6000.6.7f1 on Windows. A WebGL release was built through `CommandLineBuild.Run` on 6000.0.85f1.

### Running the tests locally

Add the package to `testables` in your project's `Packages/manifest.json`, then open **Window ▸ General ▸ Test Runner**:

```json
{
  "dependencies": {
    "com.reromanlee.autobuildpipeline": "https://github.com/reromanlee/AutoBuildPipeline.git?path=/UnityPackage"
  },
  "testables": [
    "com.reromanlee.autobuildpipeline"
  ]
}
```

The smoke builds are explicit, because each one builds a player. Select them in the Test Runner, or run them from the command line with the category filter:

```bash
Unity -batchmode -projectPath . -runTests -testPlatform EditMode -testCategory Build -testResults results.xml
```

They need the Standalone platform of your operating system to be active, change Player Settings only temporarily, add a scene to `Assets/AutoBuildPipelineSmokeTest` for the duration of the run, and build into the system's temporary folder.

### Continuous integration

`.github/workflows/tests.yml` runs the Edit Mode tests and the smoke builds with [GameCI](https://game.ci) on Unity 2021.3, 2022.3, 6000.0 and 6000.6 for every push to `main` and every pull request; on the Linux runners, the smoke builds produce Linux players. It needs the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets; without them the job passes with a notice instead of failing, so forks stay green.
