### Testing

The package ships its test suite in `UnityPackage/Tests`. It compiles only in projects that opt in, so it costs nothing otherwise.

### What is covered

1. **Edit Mode tests** (`Tests/Editor`): version parsing and increments, preview versions and labels, build planning for every platform (versions, build numbers, limits, folder and file names, package formats, overrides), command line parsing including GameCI's arguments, file name sanitizing, relative paths, keystore credential encoding, the folder backup on success, failure and interrupted builds, the restore point, `build-info.json` and GitHub outputs.
2. **Smoke builds** (`Tests/Editor/Builds`, category `Build`): real Standalone players of an empty scene for the editor's own operating system, built through the whole pipeline, checking that previews keep the version, releases save it, every build gets `build-info.json`, and a rebuild replaces the previous folder.

For 1.0.0, the Edit Mode tests passed on Unity 2021.3.45f2, 2022.3.62f3, 6000.0.85f1 and 6000.6.7f1. The smoke builds passed on Windows with 2021.3.45f2 and 6000.6.7f1, and on Linux in CI with 2021.3.45f2, 2022.3.62f3 and 6000.0.85f1. A WebGL release was built through `CommandLineBuild.Run` on 6000.0.85f1.

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

Three workflows in `.github/workflows` verify the package across Unity `2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1` and `6000.6.7f1` using the minimal `UnityProject/` test harness (which references `UnityPackage` directly in `testables` instead of GameCI's package mode) and sweep stale caches and artifacts:

| Workflow | File | Automatic triggers | Manual trigger | Notes |
| --- | --- | --- | --- | --- |
| `Continuous integration` | `continuous-integration.yml` | Pull requests and pushes to `main` (ignoring `**.md` and images) | `workflow_dispatch` | Tree-SHA gate skips `main` re-runs when the exact file tree already passed in a pull request; records a 7-day `passed-files-<tree>` marker. |
| `Tests` | `tests.yml` | Called by `continuous-integration.yml` (`workflow_call`) | — | Runs Edit Mode tests and Linux smoke builds across all 4 Unity versions (`ubuntu-latest`, 60 min timeout), verifies both NUnit XML reports strictly, writes pass/fail tables to `$GITHUB_STEP_SUMMARY`, fails on `warning CS` in package sources, caches `UnityProject/Library` and uploads `test-results-*` for 7 days on failure. |
| `Cleanup` | `cleanup.yml` | Nightly at `04:00 UTC` | `workflow_dispatch` (`olderThanHours`, default `24`) | Aborts untouched whenever another workflow run is active; sweeps caches/artifacts from deleted branches or older than the threshold while keeping active 7-day `passed-files-*` and `test-results-*` artifacts. |

The Unity jobs require the `UNITY_LICENSE`, `UNITY_EMAIL` and `UNITY_PASSWORD` repository secrets; without them the jobs pass with a notice so forks stay green. Omitting `githubToken` and `checkName` from `game-ci/unity-test-runner` eliminates GameCI's neutral Check Runs so each Unity version reports a single passing or failing check.
