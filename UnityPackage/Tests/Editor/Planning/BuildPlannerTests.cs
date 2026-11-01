using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildPlannerTests
    {
        private const string Commit = "3f2a9c1e5b7d9f0a1c3e5b7d9f0a1c3e5b7d9f0a";

        private static readonly string ProjectFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineTests", "Project");

        // Android releases.

        [Test]
        public void PatchReleaseIncrementsVersionAndVersionCode()
        {
            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.PatchRelease);

            Assert.That(plan.VersionName, Is.EqualTo("1.6.4"));
            Assert.That(plan.HasBuildNumber, Is.True);
            Assert.That(plan.BuildNumber, Is.EqualTo(8));
            Assert.That(plan.DisplayCurrentVersion, Is.EqualTo("1.6.3 (7)"));
            Assert.That(plan.DisplayVersion, Is.EqualTo("1.6.4 (8)"));
            Assert.That(plan.IsAppBundle, Is.True);
            Assert.That(plan.IsDevelopment, Is.False);
            Assert.That(plan.BuildFolder, Is.EqualTo(ExpectedPath("Builds", "Android", "com.company.app-1.6.4-8")));
            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", "Android", "com.company.app-1.6.4-8", "com.company.app-1.6.4-8.aab")));
        }

        [TestCase("Minor", "1.7.0", 8)]
        [TestCase("Major", "2.0.0", 8)]
        [TestCase("Exact", "1.6.3", 7)]
        public void ReleaseVersionsFollowTheIncrement(string kind, string expectedVersion, int expectedVersionCode)
        {
            BuildRequest request = kind == "Minor" ? BuildRequest.MinorRelease : kind == "Major" ? BuildRequest.MajorRelease : BuildRequest.ExactRelease;

            BuildPlan plan = Plan(BuildTarget.Android, request);

            Assert.That(plan.VersionName, Is.EqualTo(expectedVersion));
            Assert.That(plan.BuildNumber, Is.EqualTo(expectedVersionCode));
        }

        [Test]
        public void ApkReleaseFormatBuildsPackage()
        {
            BuildInputs inputs = CreateInputs();
            inputs.AndroidReleaseFormat = AndroidReleaseFormat.Apk;

            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.PatchRelease, inputs: inputs);

            Assert.That(plan.IsAppBundle, Is.False);
            Assert.That(plan.PlayerLocation, Does.EndWith("com.company.app-1.6.4-8.apk"));
        }

        [Test]
        public void VersionCodeAboveGooglePlayLimitFails()
        {
            BuildInputs inputs = CreateInputs();
            inputs.CurrentBuildNumber = AndroidPlatform.MaximumVersionCode.ToString();

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.Android, BuildRequest.PatchRelease, inputs: inputs));
        }

        [Test]
        public void VersionCodeOfZeroFailsExactRelease()
        {
            BuildInputs inputs = CreateInputs();
            inputs.CurrentBuildNumber = "0";

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.Android, BuildRequest.ExactRelease, inputs: inputs));
        }

        // Previews.

        [Test]
        public void PreviewKeepsVersionCodeAndReusesFolder()
        {
            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.Preview);

            Assert.That(plan.VersionName, Is.EqualTo("1.6.4-dev+261101.1432.3f2a9c1"));
            Assert.That(plan.HasBuildNumber, Is.False);
            Assert.That(plan.DisplayVersion, Is.EqualTo("1.6.4-dev+261101.1432.3f2a9c1"));
            Assert.That(plan.IsAppBundle, Is.False);
            Assert.That(plan.IsDevelopment, Is.True);
            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", "Android", "com.company.app-dev", "com.company.app-dev.apk")));
        }

        [Test]
        public void PreviewWithoutCommitLeavesHashOut()
        {
            BuildInputs inputs = CreateInputs();
            inputs.Commit = null;

            BuildPlan plan = Plan(BuildTarget.StandaloneWindows64, BuildRequest.Preview, inputs: inputs);

            Assert.That(plan.VersionName, Is.EqualTo("1.6.4-dev+261101.1432"));
        }

        [Test]
        public void InvalidPreviewLabelFailsPreviewOnly()
        {
            BuildInputs inputs = CreateInputs();
            inputs.PreviewLabel = "dev build";

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.WebGL, BuildRequest.Preview, inputs: inputs));
            Assert.DoesNotThrow(() => Plan(BuildTarget.WebGL, BuildRequest.PatchRelease, inputs: inputs));
        }

        // Other platforms.

        [Test]
        public void WebGLSiteSitsInsideBuildFolder()
        {
            BuildPlan plan = Plan(BuildTarget.WebGL, BuildRequest.PatchRelease);

            Assert.That(plan.HasBuildNumber, Is.False);
            Assert.That(plan.DisplayVersion, Is.EqualTo("1.6.4"));
            Assert.That(plan.BuildFolder, Is.EqualTo(ExpectedPath("Builds", "WebGL", "My Game-1.6.4")));
            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", "WebGL", "My Game-1.6.4", "My Game")));
        }

        [TestCase(BuildTarget.StandaloneWindows64, "Windows", "My Game.exe")]
        [TestCase(BuildTarget.StandaloneWindows, "Windows-x86", "My Game.exe")]
        [TestCase(BuildTarget.StandaloneLinux64, "Linux", "My Game.x86_64")]
        public void StandalonePlayerIsNamedAfterProduct(BuildTarget target, string platformFolder, string playerName)
        {
            BuildPlan plan = Plan(target, BuildRequest.PatchRelease);

            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", platformFolder, "My Game-1.6.4", playerName)));
        }

        [Test]
        public void MacReleaseIncrementsBuildNumber()
        {
            BuildInputs inputs = CreateInputs();
            inputs.CurrentBuildNumber = "0";

            BuildPlan plan = Plan(BuildTarget.StandaloneOSX, BuildRequest.PatchRelease, inputs: inputs);

            Assert.That(plan.BuildNumber, Is.EqualTo(1));
            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", "macOS", "My Game-1.6.4-1", "My Game.app")));
        }

        [Test]
        public void MacBuildNumberThatIsNotWholeNumberFailsRelease()
        {
            BuildInputs inputs = CreateInputs();
            inputs.CurrentBuildNumber = "1.0";

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.StandaloneOSX, BuildRequest.PatchRelease, inputs: inputs));
            Assert.DoesNotThrow(() => Plan(BuildTarget.StandaloneOSX, BuildRequest.Preview, inputs: inputs));
        }

        [Test]
        public void ProductNameIsSanitized()
        {
            BuildInputs inputs = CreateInputs();
            inputs.ProductName = "My Game: Deluxe?";

            BuildPlan plan = Plan(BuildTarget.StandaloneWindows64, BuildRequest.ExactRelease, inputs: inputs);

            Assert.That(plan.PlayerLocation, Is.EqualTo(ExpectedPath("Builds", "Windows", "My Game Deluxe-1.6.3", "My Game Deluxe.exe")));
        }

        // Project state.

        [TestCase("1.6.3-dev+251025.1432")]
        [TestCase("")]
        [TestCase("latest")]
        public void InvalidPlayerSettingsVersionFails(string currentVersion)
        {
            BuildInputs inputs = CreateInputs();
            inputs.CurrentVersion = currentVersion;

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.Android, BuildRequest.PatchRelease, inputs: inputs));
        }

        [Test]
        public void PreviewIsDevelopmentOnlyWhenChecked()
        {
            BuildInputs inputs = CreateInputs();
            inputs.IsDevelopmentEnabled = false;

            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.Preview, inputs: inputs);

            Assert.That(plan.IsDevelopment, Is.False);
        }

        // Overrides.

        [Test]
        public void VersionOverrideReplacesIncrement()
        {
            BuildOverrides overrides = new() { Version = "v1.9.0", BuildNumber = "42" };

            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.PatchRelease, overrides);

            Assert.That(plan.VersionName, Is.EqualTo("1.9.0"));
            Assert.That(plan.BuildNumber, Is.EqualTo(42));
        }

        [Test]
        public void NoneOverrideIsIgnored()
        {
            BuildOverrides overrides = new() { Version = "none", BuildNumber = "None" };

            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.PatchRelease, overrides);

            Assert.That(plan.VersionName, Is.EqualTo("1.6.4"));
            Assert.That(plan.BuildNumber, Is.EqualTo(8));
        }

        [TestCase("1.9")]
        [TestCase("-5")]
        [TestCase("forty-two")]
        public void InvalidBuildNumberOverrideFails(string buildNumber)
        {
            BuildOverrides overrides = new() { BuildNumber = buildNumber };

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.Android, BuildRequest.PatchRelease, overrides));
        }

        [Test]
        public void InvalidVersionOverrideFails()
        {
            BuildOverrides overrides = new() { Version = "1.9.0-beta" };

            Assert.Throws<BuildFailedException>(() => Plan(BuildTarget.Android, BuildRequest.PatchRelease, overrides));
        }

        [Test]
        public void OutputFolderOverrideReplacesSetting()
        {
            BuildOverrides overrides = new() { OutputFolder = "../Artifacts" };

            BuildPlan plan = Plan(BuildTarget.WebGL, BuildRequest.PatchRelease, overrides);

            string expectedFolder = Path.GetFullPath(Path.Combine(ProjectFolder, "..", "Artifacts", "WebGL", "My Game-1.6.4"));
            Assert.That(plan.BuildFolder, Is.EqualTo(expectedFolder));
        }

        [TestCase("build/Android/Android.apk", false)]
        [TestCase("build/Android/Android.aab", true)]
        [TestCase("build/Android/Android", true)]
        public void PlayerLocationExtensionDecidesAndroidFormat(string playerLocation, bool isAppBundle)
        {
            BuildOverrides overrides = new() { PlayerLocation = playerLocation };

            BuildPlan plan = Plan(BuildTarget.Android, BuildRequest.PatchRelease, overrides);

            Assert.That(plan.IsAppBundle, Is.EqualTo(isAppBundle));
            Assert.That(plan.BuildFolder, Is.Null);
            Assert.That(plan.PlayerLocation, Is.EqualTo(Path.GetFullPath(Path.Combine(ProjectFolder, playerLocation))));
        }

        private static BuildPlan Plan(BuildTarget target, BuildRequest request, BuildOverrides overrides = null, BuildInputs inputs = null)
        {
            Assert.That(BuildPlatforms.TryGet(target, out BuildPlatform platform), Is.True);
            return BuildPlanner.Create(platform, request, overrides ?? new BuildOverrides(), inputs ?? CreateInputs());
        }

        private static BuildInputs CreateInputs()
        {
            return new BuildInputs
            {
                CurrentVersion = "1.6.3",
                CurrentBuildNumber = "7",
                ProductName = "My Game",
                ApplicationIdentifier = "com.company.app",
                ProjectFolder = ProjectFolder,
                OutputFolder = AutoBuildPipelineSettings.DefaultOutputFolder,
                PreviewLabel = "dev",
                AndroidReleaseFormat = AndroidReleaseFormat.AppBundle,
                IsDevelopmentEnabled = true,
                Timestamp = new DateTime(2026, 11, 1, 14, 32, 0),
                Commit = Commit
            };
        }

        private static string ExpectedPath(params string[] segments)
        {
            string path = ProjectFolder;
            foreach (string segment in segments)
            {
                path = Path.Combine(path, segment);
            }

            return Path.GetFullPath(path);
        }
    }
}
