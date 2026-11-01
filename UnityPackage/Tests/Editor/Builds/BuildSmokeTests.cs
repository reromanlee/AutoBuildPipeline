using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    /// <summary>
    /// Builds real Standalone players of an empty scene for the editor's own operating system, through the whole
    /// pipeline: planning, safeguards, Unity's build, restore and build-info.json. Explicit, because each build takes
    /// a minute; run it with <c>-testCategory Build</c> or select it in the Test Runner window.
    /// </summary>
    [TestFixture]
    [Category(BuildCategory)]
    [Explicit("Builds real players, which takes minutes. Run it with -testCategory Build or select it in the Test Runner.")]
    public sealed class BuildSmokeTests
    {
        public const string BuildCategory = "Build";

        private const string SceneFolder = "Assets/AutoBuildPipelineSmokeTest";
        private const string ScenePath = SceneFolder + "/SmokeTest.unity";
        private const string StartVersion = "1.2.3";

        private bool _isUntitledSceneReplaced;
        private EditorBuildSettingsScene[] _previousScenes;
        private string _previousVersion;
        private string _outputFolder;
        private BuildPlatform _platform;

        [OneTimeSetUp]
        public void CreateProjectState()
        {
            BuildTarget hostTarget = GetHostStandaloneTarget();
            if (EditorUserBuildSettings.activeBuildTarget != hostTarget)
            {
                Assert.Ignore($"Switch the active platform to {hostTarget} to run the smoke builds.");
            }

            Assert.That(BuildPlatforms.TryGet(hostTarget, out _platform), Is.True);

            // An additive scene leaves whatever is open in the editor untouched. Unity refuses to add one next to an
            // untitled scene, which is what batch mode starts with; an unchanged untitled scene is simply replaced.
            Scene activeScene = SceneManager.GetActiveScene();
            _isUntitledSceneReplaced = SceneManager.sceneCount == 1 && string.IsNullOrEmpty(activeScene.path);
            if (_isUntitledSceneReplaced && activeScene.isDirty)
            {
                Assert.Ignore("Save or discard the untitled scene to run the smoke builds.");
            }

            Directory.CreateDirectory(SceneFolder);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, _isUntitledSceneReplaced ? NewSceneMode.Single : NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, ScenePath);
            if (!_isUntitledSceneReplaced)
            {
                EditorSceneManager.CloseScene(scene, true);
            }

            AssetDatabase.Refresh();

            _previousScenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            _previousVersion = PlayerSettings.bundleVersion;
            _outputFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineSmokeTests", Guid.NewGuid().ToString("N"));
        }

        [OneTimeTearDown]
        public void RestoreProjectState()
        {
            if (_previousScenes != null)
            {
                EditorBuildSettings.scenes = _previousScenes;
                PlayerSettings.bundleVersion = _previousVersion;
                if (_isUntitledSceneReplaced)
                {
                    // The smoke test scene is still open, so an untitled scene takes its place again before it is deleted.
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                }

                AssetDatabase.DeleteAsset(SceneFolder);
                AssetDatabase.SaveAssets();
            }

            if (_outputFolder != null && Directory.Exists(_outputFolder))
            {
                Directory.Delete(_outputFolder, true);
            }
        }

        [SetUp]
        public void ResetVersion()
        {
            PlayerSettings.bundleVersion = StartVersion;
        }

        [Test]
        public void PreviewKeepsProjectVersion()
        {
            BuildReport report = AutoBuilder.Build(BuildRequest.Preview, CreateOverrides());

            Assert.That(report.summary.result, Is.EqualTo(BuildResult.Succeeded));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo(StartVersion));
            AssertBuildExists(report, "-dev", expectedVersionPrefix: "1.2.4-dev+");
        }

        [Test]
        public void PatchReleaseSavesNewVersion()
        {
            BuildReport report = AutoBuilder.Build(BuildRequest.PatchRelease, CreateOverrides());

            Assert.That(report.summary.result, Is.EqualTo(BuildResult.Succeeded));
            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("1.2.4"));
            AssertBuildExists(report, "-1.2.4", expectedVersionPrefix: "1.2.4");
        }

        [Test]
        public void RebuildReplacesPreviousBuild()
        {
            // First preview, plus a stray file that only the first build folder has.
            BuildReport firstReport = AutoBuilder.Build(BuildRequest.Preview, CreateOverrides());
            string buildFolder = Path.GetDirectoryName(firstReport.summary.outputPath);
            string strayFile = Path.Combine(buildFolder, "stray.txt");
            File.WriteAllText(strayFile, "left over from the first build");

            // Second preview replaces the whole folder.
            BuildReport secondReport = AutoBuilder.Build(BuildRequest.Preview, CreateOverrides());

            Assert.That(secondReport.summary.result, Is.EqualTo(BuildResult.Succeeded));
            Assert.That(File.Exists(strayFile), Is.False);
            Assert.That(Directory.Exists(buildFolder + BuildFolderBackup.BackupSuffix), Is.False);
        }

        private BuildOverrides CreateOverrides()
        {
            return new BuildOverrides
            {
                OutputFolder = _outputFolder,
                IsUnattended = true
            };
        }

        private void AssertBuildExists(BuildReport report, string folderSuffix, string expectedVersionPrefix)
        {
            string playerPath = report.summary.outputPath;
            string buildFolder = Path.GetDirectoryName(playerPath);
            Assert.That(Path.GetDirectoryName(buildFolder), Is.EqualTo(Path.Combine(_outputFolder, _platform.FolderName)));
            // macOS release folders also carry the build number, e.g. My Game-1.2.4-1.
            Assert.That(Path.GetFileName(buildFolder), Does.Contain(folderSuffix));
            Assert.That(File.Exists(playerPath) || Directory.Exists(playerPath), Is.True, playerPath);

            string buildInfoPath = Path.Combine(buildFolder, BuildInfoFile.FileName);
            Assert.That(File.Exists(buildInfoPath), Is.True, buildInfoPath);
            BuildInfoData buildInfo = JsonUtility.FromJson<BuildInfoData>(File.ReadAllText(buildInfoPath));
            Assert.That(buildInfo.Version, Does.StartWith(expectedVersionPrefix));
            Assert.That(buildInfo.Platform, Is.EqualTo(_platform.DisplayName));
        }

        private static BuildTarget GetHostStandaloneTarget()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXEditor:
                {
                    return BuildTarget.StandaloneOSX;
                }
                case RuntimePlatform.LinuxEditor:
                {
                    return BuildTarget.StandaloneLinux64;
                }
                default:
                {
                    return BuildTarget.StandaloneWindows64;
                }
            }
        }
    }
}
