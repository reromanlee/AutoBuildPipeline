# Open Issues Plan

- **Status:** Complete (10/10 tasks complete | Next: None)
- **Total Estimate:** 3h 20m (Remaining: 0m)
- **Goal:** Resolve all open repository issues except #3 (iOS support) — specifically #2, #4, #5, #7, #8 and #9 — on branch `feature/open-issues` with zero compiler warnings and passing Edit Mode and smoke tests across Unity 2021.3, 2022.3, 6000.0 and 6000.6.

### Stack

- **Runtime & Language:** Unity Editor C# (`.NET Standard 2.1` / Unity 2021.3 LTS through Unity 6.6), `allowUnsafeCode: false`, zero external package dependencies.
- **Target Unity Versions:** `2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1`, `6000.6.7f1` (all installed locally with Android Build Support).
- **Test & Verification:** Unity Test Runner Edit Mode (`NUnit`), explicit `Build` category smoke tests (`BuildSmokeTests`), GitHub Actions (`game-ci/unity-test-runner`).
- **Git Workflow:** `RepositoryWorkflow` on branch `feature/open-issues`, logical commits (`subject: intent`, <=25 chars), `CHANGELOG.md` updated under `# AutoBuildPipeline (Unreleased)` (`package.json` stays `1.0.0`).

### Locked Decisions and Invariants

1. **Branching & Versioning:** Single working branch `feature/open-issues` with logical commits per issue; update `CHANGELOG.md` under `# AutoBuildPipeline (Unreleased)` and keep `UnityPackage/package.json` at `1.0.0` until release. Rejected separate per-issue branches to keep related settings/docs cohesive.
2. **Issue #9 (Optional Android Keystore):** Allow both `.apk` and `.aab` release builds when `PlayerSettings.Android.useCustomKeystore` is `false`. Log a clear `BuildLog.Warning` (explicitly noting Google Play rejects debug-signed `.aab` uploads) and show `Unsigned (debug keystore)` in the interactive release confirmation dialog. Still throw `BuildFailedException` if `useCustomKeystore` is `true` or `-androidKeystoreName` is supplied while keystore file, alias or passwords are missing. Rejected blocking `.aab` releases unconditionally so local testing and external signing workflows work.
3. **Issue #2 (Android Debug Symbols):** Add `AndroidDebugSymbols` (`Off`, `Public`, `Debugging`, default `Public`) in `AutoBuildPipelineSettings`. Apply on all Android release builds (`.apk` and `.aab`) and leave previews untouched. Use `EditorUserBuildSettings.androidCreateSymbols` on `< UNITY_6000_0_OR_NEWER` and reflection against `UnityEditor.Android.UserBuildSettings+DebugSymbols.level` (preserving `format`) on `UNITY_6000_0_OR_NEWER`, so projects without the Android module compile cleanly with zero warnings. Capture and restore in `BuildSettingsRestorePoint` for crash safety.
4. **Issue #5 (Current Build Context):** Add immutable `public sealed class BuildContext` (exposing `Request`, `Target`, `PlatformName`, `Version`, `VersionName`, `HasBuildNumber`, `BuildNumber`, `IsRelease`, `IsDevelopment`, `IsAppBundle`, `BuildFolder`, `PlayerLocation`, `Commit`, `IsUnattended`). Expose `AutoBuilder.CurrentBuild` (`BuildContext`, `null` when idle) and `AutoBuilder.CurrentRequest` (`BuildRequest?`, shortcut to `CurrentBuild?.Request`). Set inside `try` before platform preparation and `RunBuildPipeline`; clear unconditionally in `finally`.
5. **Issue #8 (Switch Platform Menu):** Add `Build ▸ Switch Platform ▸` submenu for `Android`, `WebGL`, `Windows`, `macOS`, `Linux` (`Windows (32-bit)` omitted to keep menu concise). Checkmark active target via `Menu.SetChecked`, disable already-active target, busy editor states, and targets whose module is absent (`BuildPipeline.IsBuildTargetSupported`). Always confirm via `EditorUtility.DisplayDialog` before invoking `EditorUserBuildSettings.SwitchActiveBuildTargetAsync`.
6. **Issue #4 (Configurable Name Patterns):** Add 3 single-segment patterns in `AutoBuildPipelineSettings` under Output (always under `<OutputFolder>/<Platform>/`; forbid `/`, `\`, `..` to guarantee `BuildFolderBackup` isolation):
   - `ReleaseFolderPattern` (default `{name}-{version}-{code}`): auto-collapses adjacent separators around empty tokens (e.g. empty `{code}` on Windows produces `My Game-1.6.4`); must contain `{version}` so releases never share a folder.
   - `PreviewFolderPattern` (default `{name}-{label}`): must contain `{label}` and cannot resolve to the same folder as the corresponding release.
   - `PlayerNamePattern` (default empty = platform default: build folder name on Android, `{product}` on other platforms; extension always appended automatically).
   - Supported tokens: `{name}`, `{product}`, `{identifier}`, `{version}`, `{code}`, `{platform}`, `{label}`, `{date}` (`yyyy-MM-dd`), `{time}` (`HHmm`). Unknown tokens fail planning with a clear error; every resolved name passes `FileNames.Sanitize`.
7. **Issue #7 (CI Neutral Checks & Unity 6.6):** Replace `packageMode: true` with a minimal `UnityProject/` (`Packages/manifest.json` referencing `file:../../UnityPackage` in `testables`). Add `6000.6.7f1` to the matrix alongside `2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1`. Drop `githubToken` and `checkName` from GameCI steps, verify NUnit XML strictly in a bash step (fail on any failed/error/inconclusive test or missing XML) and write a Markdown summary to `$GITHUB_STEP_SUMMARY`. Apply full `RepositoryWorkflow` CI layout: `continuous-integration.yml` (tree-SHA gate + `paths-ignore`) calling `tests.yml` (`workflow_call`), `cleanup.yml`, `Library` caching, C# warning check, `timeout-minutes` and `retention-days`. Keep `Documentation/CI.md` user-facing and document repo GitHub Actions in `Documentation/TESTING.md`.
8. **Unity Package Discipline:** Every newly created `.cs` file in `UnityPackage/` must include a matching `.cs.meta` file with a unique 32-char lowercase hex GUID.

### Scoped File Map

- **CI & Harness Scope:** `UnityProject/`, `.github/workflows/`, `.gitignore`
- **Package Core Scope:** `UnityPackage/Editor/` (`AutoBuilder.cs`, `BuildMenu.cs`, `BuildContext.cs` [new])
- **Android & Safeguards Scope:** `UnityPackage/Editor/Signing/AndroidSigning.cs`, `UnityPackage/Editor/Platforms/AndroidPlatform.cs`, `UnityPackage/Editor/Platforms/AndroidDebugSymbolsUtility.cs` [new], `UnityPackage/Editor/Safeguards/BuildSettingsRestorePoint.cs`
- **Planning & Settings Scope:** `UnityPackage/Editor/Planning/` (`BuildPlanner.cs`, `BuildInputs.cs`, `BuildNamePattern.cs` [new]), `UnityPackage/Editor/Settings/` (`AutoBuildPipelineSettings.cs`, `AutoBuildPipelineSettingsProvider.cs`, `AndroidDebugSymbols.cs` [new])
- **Tests Scope:** `UnityPackage/Tests/Editor/`
- **Docs Scope:** `CHANGELOG.md`, `README.md`, `Documentation/` (`USAGE.md`, `SIGNING.md`, `TESTING.md`, `CI.md`)

### Priority Scopes

#### A. Test Project Harness & CI Upgrades — Issue #7 (Independent | Est: 40m)

- [x] **A1. Create minimal `UnityProject/` test harness and `.gitignore` entries** (Independent | Est: 15m)
  - **Context:** Enables local batch-mode Unity testing across all 4 installed Unity versions and eliminates GameCI's broken `packageMode: true` (`codecoverage 1.1.1`) for Unity 6.6.
  - **Scope & Entry:** `UnityProject/Packages/manifest.json` (new), `.gitignore`
  - **Action:** Create `UnityProject/Packages/manifest.json` depending on `com.unity.test-framework` and `"com.reromanlee.autobuildpipeline": "file:../../UnityPackage"` with `"testables": ["com.reromanlee.autobuildpipeline"]`, and ignore generated Unity folders (`Assets/`, `Library/`, `ProjectSettings/`, etc.) under `UnityProject/` in `.gitignore`.
  - **Verify:** Run Unity Edit Mode tests in batch mode on `6000.6.7f1` against `UnityProject` and confirm 100% pass with 0 compiler warnings.
  - **Note:** Passed on `6000.6.7f1` (143 passed, 0 failed, 3 explicit skipped, 0 warnings). Kept `UnityProject/ProjectSettings/` gitignored so switching Unity versions never dirties git status; CI writes `ProjectVersion.txt` from `matrix.unityVersion`.

- [x] **A2. Upgrade GitHub Actions CI workflows (`continuous-integration.yml`, `tests.yml`, `cleanup.yml`)** (Depends on: A1 | Est: 25m)
  - **Context:** Fixes Issue #7 ("3 of 9 passing" neutral GameCI checks + missing Unity 6.6) and applies `RepositoryWorkflow` CI rules (Invariant 7).
  - **Scope & Entry:** `.github/workflows/continuous-integration.yml` (new), `.github/workflows/tests.yml`, `.github/workflows/cleanup.yml` (new)
  - **Action:** Convert `tests.yml` to `on: workflow_call` using `UnityProject`, 4-version matrix (`2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1`, `6000.6.7f1`), `Library` caching, `GAME_CI_COVERAGE_ENABLED: false`, no `githubToken`/`checkName`, NUnit XML verifier + `$GITHUB_STEP_SUMMARY`, C# warning check, `timeout-minutes` and `retention-days`. Wrap it with `continuous-integration.yml` (tree-SHA gate + `paths-ignore`) and add collision-safe `cleanup.yml`. Commit A1+A2 logically.
  - **Verify:** Validate YAML syntax and pinned action SHAs; verify NUnit XML parser script locally against both a passing and simulated failing XML.

#### B. Android Keystore & Debug Symbols — Issues #9 and #2 (Depends on: A1 | Est: 45m)

- [x] **B1. Make Android custom keystore optional for `.apk` and `.aab` releases (Issue #9)** (Depends on: A1 | Est: 20m)
  - **Context:** Projects without a custom keystore currently fail all Android releases (Invariant 2).
  - **Scope & Entry:** `UnityPackage/Editor/Signing/AndroidSigning.cs`, `UnityPackage/Editor/Platforms/AndroidPlatform.cs`, `UnityPackage/Editor/AutoBuilder.cs`, `UnityPackage/Tests/Editor/Signing/`
  - **Action:** When `!PlayerSettings.Android.useCustomKeystore` during an Android release, log a warning (calling out Google Play rejection for debug-signed `.aab`) and continue with Unity's debug keystore; append an unsigned notice in `AutoBuilder.ConfirmRelease` when interactive Android release has no custom keystore. Keep failing if `useCustomKeystore` is true or `-androidKeystoreName` override is provided with missing keystore/alias/passwords. Add unit tests and commit.
  - **Verify:** Edit Mode tests pass on Unity `6000.6.7f1` covering debug-signed `.apk`/`.aab` release allowance and broken custom keystore failure.

- [x] **B2. Add configurable Android Release Debug Symbols with Unity 2021-6 support (Issue #2)** (Depends on: B1 | Est: 25m)
  - **Context:** Produces native debug symbols on Android releases (`Off`, `Public`, `Debugging`; default `Public`) without requiring the Android module to compile and with crash-safe restore (Invariant 3).
  - **Scope & Entry:** `UnityPackage/Editor/Settings/AndroidDebugSymbols.cs` (new), `UnityPackage/Editor/Settings/AutoBuildPipelineSettings.cs`, `UnityPackage/Editor/Settings/AutoBuildPipelineSettingsProvider.cs`, `UnityPackage/Editor/Platforms/AndroidDebugSymbolsUtility.cs` (new), `UnityPackage/Editor/Platforms/AndroidPlatform.cs`, `UnityPackage/Editor/Safeguards/BuildSettingsRestorePoint.cs`, `UnityPackage/Tests/Editor/Safeguards/BuildSettingsRestorePointTests.cs`
  - **Action:** Add `AndroidDebugSymbols` enum and setting (default `Public`), implement cross-version reader/writer (`EditorUserBuildSettings.androidCreateSymbols` on `< UNITY_6000_0_OR_NEWER`, reflection on `UnityEditor.Android.UserBuildSettings+DebugSymbols.level` on Unity 6), apply in `AndroidPlatform.TryPrepare` for releases only, capture/restore in `BuildSettingsRestorePoint`, and draw dropdown in Settings Provider. Add unit tests and commit.
  - **Verify:** Edit Mode tests pass on both Unity `2021.3.45f2` and `6000.6.7f1` with 0 warnings, confirming apply and restore round-trips.

#### C. Build Context Exposure & Switch Platform Menu — Issues #5 and #8 (Depends on: A1 | Est: 35m)

- [x] **C1. Expose `AutoBuilder.CurrentBuild` and `AutoBuilder.CurrentRequest` (Issue #5)** (Depends on: A1 | Est: 20m)
  - **Context:** Allows Unity build callbacks (`IPreprocessBuildWithReport`, `IPostprocessBuildWithReport`, etc.) to inspect whether AutoBuildPipeline started the build and read resolved version/build number/paths (Invariant 4).
  - **Scope & Entry:** `UnityPackage/Editor/BuildContext.cs` (new), `UnityPackage/Editor/AutoBuilder.cs`, `UnityPackage/Tests/Editor/Builds/BuildSmokeTests.cs`
  - **Action:** Create immutable `public sealed class BuildContext`, add `AutoBuilder.CurrentBuild` and `AutoBuilder.CurrentRequest` properties, assign before platform preparation / `RunBuildPipeline` inside `try`, and clear to `null` in `finally`. Add unit/smoke callback tests verifying visibility inside `IPreprocessBuildWithReport`/`IPostprocessBuildWithReport` and guaranteed cleanup on exception. Commit.
  - **Verify:** Edit Mode tests (and callback smoke test) pass on Unity `6000.6.7f1`.

- [x] **C2. Add `Build ▸ Switch Platform` submenu with confirmation (Issue #8)** (Depends on: A1 | Est: 15m)
  - **Context:** Lets developers switch between supported platforms directly from the `Build` menu with confirmation before triggering reimport (Invariant 5).
  - **Scope & Entry:** `UnityPackage/Editor/BuildMenu.cs`
  - **Action:** Add `Build/Switch Platform/` items for `Android`, `WebGL`, `Windows`, `macOS`, `Linux` with validation handlers setting `Menu.SetChecked`, disabling when already active, busy, or unsupported (`BuildPipeline.IsBuildTargetSupported`), and confirming via `EditorUtility.DisplayDialog` before `EditorUserBuildSettings.SwitchActiveBuildTargetAsync`. Commit.
  - **Verify:** Edit Mode tests compile and pass on Unity `2021.3.45f2` and `6000.6.7f1` with 0 warnings.

#### D. Configurable Build Folder & File Names — Issue #4 (Depends on: B2 | Est: 50m)

- [x] **D1. Implement `BuildNamePattern` token formatter, sanitizer and collision validator** (Depends on: A1 | Est: 25m)
  - **Context:** Replaces hardcoded folder/file naming with validated, cross-OS-safe patterns while preserving 1.0.0 defaults and preventing preview/release or release/release collisions (Invariant 6).
  - **Scope & Entry:** `UnityPackage/Editor/Planning/BuildNamePattern.cs` (new), `UnityPackage/Tests/Editor/Planning/BuildNamePatternTests.cs` (new)
  - **Action:** Implement pattern token parsing (`{name}`, `{product}`, `{identifier}`, `{version}`, `{code}`, `{platform}`, `{label}`, `{date}`, `{time}`), empty-token separator collapsing, unknown-token errors, path-separator/traversal rejection, `FileNames.Sanitize` per output, and collision validation (release requires `{version}`, preview requires `{label}`, preview resolved folder cannot match release folder).
  - **Verify:** Unit tests cover all tokens, empty `{code}` trimming, Windows reserved names, unknown tokens, invalid path separators, and collision rejections.

- [x] **D2. Wire pattern settings into `AutoBuildPipelineSettings`, `BuildPlanner` and Settings UI** (Depends on: D1 | Est: 25m)
  - **Context:** Exposes the 3 patterns in Project Settings with live preview and applies them during planning without breaking existing 1.0.0 behavior.
  - **Scope & Entry:** `UnityPackage/Editor/Settings/AutoBuildPipelineSettings.cs`, `UnityPackage/Editor/Settings/AutoBuildPipelineSettingsProvider.cs`, `UnityPackage/Editor/Planning/BuildInputs.cs`, `UnityPackage/Editor/Planning/BuildPlanner.cs`, `UnityPackage/Editor/Platforms/BuildPlatform.cs`, `UnityPackage/Tests/Editor/Planning/BuildPlannerTests.cs`
  - **Action:** Add `ReleaseFolderPattern`, `PreviewFolderPattern`, `PlayerNamePattern` to settings and `BuildInputs`; update `BuildPlanner.ResolveLocation` and platform player path helpers; add pattern fields and token hint in `AutoBuildPipelineSettingsProvider`; add planner tests. Commit D1+D2.
  - **Verify:** Full Edit Mode test suite passes on Unity `6000.6.7f1` including all existing 1.0.0 default layout assertions and new custom pattern tests.

#### E. Documentation, Changelog & Full Verification (Depends on: A, B, C, D | Est: 30m)

- [x] **E1. Update `CHANGELOG.md`, `README.md` and `Documentation/`** (Depends on: A2, B2, C2, D2 | Est: 15m)
  - **Context:** Keeps documentation and unreleased changelog accurate and aligned with `RepositoryWorkflow`.
  - **Scope & Entry:** `CHANGELOG.md`, `README.md`, `Documentation/USAGE.md`, `Documentation/SIGNING.md`, `Documentation/TESTING.md`, `Documentation/CI.md`
  - **Action:** Add `# AutoBuildPipeline (Unreleased)` to `CHANGELOG.md`; update `README.md` features/limitations/usage; update `USAGE.md` (name patterns, symbols, `CurrentBuild`/`CurrentRequest`, Switch Platform), `SIGNING.md` (optional keystore behavior), `TESTING.md` (repo GitHub Actions workflows table/triggers/details), and link from `CI.md`. Commit.
  - **Verify:** Review all docs for clean formatting, accurate paths/symbols, and no broken links.

- [x] **E2. Run full cross-version Edit Mode tests and smoke builds** (Depends on: E1 | Est: 15m)
  - **Context:** Verifies zero warnings and passing tests across all 4 supported Unity editors locally before reporting ready for PR.
  - **Scope & Entry:** `UnityProject/` across `2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1`, `6000.6.7f1`
  - **Action:** Run Edit Mode tests on all 4 Unity versions and check editor logs for any `warning CS` in `UnityPackage`; run explicit `Build` smoke tests on `2021.3.45f2` and `6000.6.7f1`; verify `git status` and commit log compliance with `RepositoryWorkflow`.
  - **Verify:** 0 failed tests, 0 compiler warnings in `UnityPackage`, clean working tree on `feature/open-issues`, all commits <= 25 chars.
  - **Note:** Passed 177/177 Edit Mode tests (0 warnings) on `2021.3.45f2`, `2022.3.62f3`, `6000.0.85f1`, `6000.6.7f1`, plus 3/3 Windows smoke builds (0 warnings) on `2021.3.45f2` and `6000.6.7f1`.
