using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildContextTests
    {
        private EditorBuildSettingsScene[] _previousScenes;
        private string _previousVersion;

        [SetUp]
        public void RememberProjectState()
        {
            _previousScenes = EditorBuildSettings.scenes;
            _previousVersion = PlayerSettings.bundleVersion;
        }

        [TearDown]
        public void RestoreProjectState()
        {
            EditorBuildSettings.scenes = _previousScenes;
            PlayerSettings.bundleVersion = _previousVersion;
        }

        [Test]
        public void CurrentBuildAndRequestAreNullWhenIdle()
        {
            Assert.That(AutoBuilder.CurrentBuild, Is.Null);
            Assert.That(AutoBuilder.CurrentRequest, Is.Null);
        }

        [Test]
        public void BuildContextCopiesResolvedPlanFields()
        {
            Assert.That(BuildPlatforms.TryGet(BuildTarget.Android, out BuildPlatform android), Is.True);
            string projectFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineContextTests");
            BuildInputs inputs = new()
            {
                CurrentVersion = "1.6.3",
                CurrentBuildNumber = "7",
                ProductName = "My Game",
                ApplicationIdentifier = "com.company.app",
                ProjectFolder = projectFolder,
                OutputFolder = AutoBuildPipelineSettings.DefaultOutputFolder,
                PreviewLabel = "dev",
                AndroidReleaseFormat = AndroidReleaseFormat.AppBundle,
                IsDevelopmentEnabled = true,
                Timestamp = new DateTime(2026, 11, 1, 14, 32, 0),
                Commit = "abcdef1234567890"
            };

            BuildPlan plan = BuildPlanner.Create(android, BuildRequest.PatchRelease, new BuildOverrides(), inputs);
            BuildContext context = new(plan, isUnattended: true);

            Assert.That(context.Request.IsRelease, Is.True);
            Assert.That(context.Request.VersionIncrement, Is.EqualTo(VersionIncrement.Patch));
            Assert.That(context.Target, Is.EqualTo(BuildTarget.Android));
            Assert.That(context.PlatformName, Is.EqualTo("Android"));
            Assert.That(context.Version, Is.EqualTo(new ApplicationVersion(1, 6, 4)));
            Assert.That(context.VersionName, Is.EqualTo("1.6.4"));
            Assert.That(context.HasBuildNumber, Is.True);
            Assert.That(context.BuildNumber, Is.EqualTo(8));
            Assert.That(context.IsRelease, Is.True);
            Assert.That(context.IsDevelopment, Is.False);
            Assert.That(context.IsAppBundle, Is.True);
            Assert.That(context.BuildFolder, Is.EqualTo(plan.BuildFolder));
            Assert.That(context.PlayerLocation, Is.EqualTo(plan.PlayerLocation));
            Assert.That(context.Commit, Is.EqualTo("abcdef1234567890"));
            Assert.That(context.IsUnattended, Is.True);
        }

        [Test]
        public void BuildContextResolvesBuildFolderWhenCustomPlayerLocationIsUsed()
        {
            Assert.That(BuildPlatforms.TryGet(BuildTarget.Android, out BuildPlatform android), Is.True);
            string projectFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineContextTests");
            BuildOverrides overrides = new() { PlayerLocation = "custom/out/Game.apk" };
            BuildInputs inputs = new()
            {
                CurrentVersion = "1.0.0",
                CurrentBuildNumber = "1",
                ProductName = "My Game",
                ApplicationIdentifier = "com.company.app",
                ProjectFolder = projectFolder,
                OutputFolder = AutoBuildPipelineSettings.DefaultOutputFolder,
                PreviewLabel = "dev",
                AndroidReleaseFormat = AndroidReleaseFormat.AppBundle,
                IsDevelopmentEnabled = false,
                Timestamp = new DateTime(2026, 11, 1, 14, 32, 0)
            };

            BuildPlan plan = BuildPlanner.Create(android, BuildRequest.ExactRelease, overrides, inputs);
            BuildContext context = new(plan, isUnattended: false);

            Assert.That(plan.BuildFolder, Is.Null);
            Assert.That(context.BuildFolder, Is.EqualTo(Path.GetFullPath(Path.Combine(projectFolder, "custom", "out"))));
        }

        [Test]
        public void CurrentBuildIsClearedWhenBuildThrows()
        {
            if (!BuildPlatforms.IsSupported(EditorUserBuildSettings.activeBuildTarget))
            {
                Assert.Ignore("Active build target is not supported.");
            }

            PlayerSettings.bundleVersion = "1.0.0";
            EditorBuildSettings.scenes = Array.Empty<EditorBuildSettingsScene>();

            BuildOverrides overrides = new()
            {
                OutputFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineContextFailTest"),
                IsUnattended = true
            };

            Assert.Throws<BuildFailedException>(() => AutoBuilder.Build(BuildRequest.Preview, overrides));
            Assert.That(AutoBuilder.CurrentBuild, Is.Null);
            Assert.That(AutoBuilder.CurrentRequest, Is.Null);
        }
    }
}
