using NUnit.Framework;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildReportsTests
    {
        private const string Commit = "3f2a9c1e5b7d9f0a1c3e5b7d9f0a1c3e5b7d9f0a";

        private static readonly string BuildFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineTests", "Builds", "Android", "com.company.app-1.6.4-8");

        [Test]
        public void BuildInfoSitsNextToPlayerFile()
        {
            string playerLocation = Path.Combine(BuildFolder, "com.company.app-1.6.4-8.aab");

            Assert.That(BuildInfoFile.GetPath(playerLocation), Is.EqualTo(Path.Combine(Path.GetFullPath(BuildFolder), BuildInfoFile.FileName)));
        }

        [Test]
        public void BuildInfoSitsNextToSiteFolder()
        {
            string siteFolder = Path.Combine(BuildFolder, "My Game") + Path.DirectorySeparatorChar;

            Assert.That(BuildInfoFile.GetPath(siteFolder), Is.EqualTo(Path.Combine(Path.GetFullPath(BuildFolder), BuildInfoFile.FileName)));
        }

        [Test]
        public void BuildInfoDescribesRelease()
        {
            DateTime createdUtc = new(2026, 11, 1, 14, 32, 5, DateTimeKind.Utc);

            BuildInfoData data = BuildInfoFile.Create(CreateReleasePlan(), createdUtc, 1024L);
            BuildInfoData restored = JsonUtility.FromJson<BuildInfoData>(JsonUtility.ToJson(data));

            Assert.That(restored.Product, Is.EqualTo("My Game"));
            Assert.That(restored.Identifier, Is.EqualTo("com.company.app"));
            Assert.That(restored.Platform, Is.EqualTo("Android"));
            Assert.That(restored.Version, Is.EqualTo("1.6.4"));
            Assert.That(restored.BuildNumber, Is.EqualTo(8));
            Assert.That(restored.IsRelease, Is.True);
            Assert.That(restored.IsDevelopment, Is.False);
            Assert.That(restored.Commit, Is.EqualTo(Commit));
            Assert.That(restored.UnityVersion, Is.EqualTo(Application.unityVersion));
            Assert.That(restored.CreatedUtc, Is.EqualTo("2026-11-01T14:32:05Z"));
            Assert.That(restored.Player, Is.EqualTo("com.company.app-1.6.4-8.aab"));
            Assert.That(restored.SizeInBytes, Is.EqualTo(1024L));
        }

        [Test]
        public void GitHubOutputsHaveOneLineEach()
        {
            BuildPlan plan = CreateReleasePlan();

            string outputs = GitHubActionsOutput.Format(plan, "build-info.json");

            string expected = "version=1.6.4\n" +
                              "build-number=8\n" +
                              "platform=Android\n" +
                              $"output-path={plan.PlayerLocation}\n" +
                              "build-info-path=build-info.json\n";
            Assert.That(outputs, Is.EqualTo(expected));
        }

        [Test]
        public void GitHubOutputsKeepValuesOnOneLine()
        {
            BuildPlan plan = CreateReleasePlan();
            plan.HasBuildNumber = false;
            plan.PlayerLocation = "first\nsecond";

            string outputs = GitHubActionsOutput.Format(plan, null);

            Assert.That(outputs, Does.Contain("build-number=\n"));
            Assert.That(outputs, Does.Contain("output-path=first second\n"));
            Assert.That(outputs, Does.EndWith("build-info-path=\n"));
        }

        [TestCase(Commit, true)]
        [TestCase("3f2a9c1", false)]
        [TestCase("3F2A9C1E5B7D9F0A1C3E5B7D9F0A1C3E5B7D9F0A", false)]
        [TestCase("zz2a9c1e5b7d9f0a1c3e5b7d9f0a1c3e5b7d9f0a", false)]
        [TestCase(null, false)]
        public void RecognizeCommitHash(string text, bool isCommitHash)
        {
            Assert.That(GitRepository.IsCommitHash(text), Is.EqualTo(isCommitHash));
        }

        private static BuildPlan CreateReleasePlan()
        {
            Assert.That(BuildPlatforms.TryGet(BuildTarget.Android, out BuildPlatform platform), Is.True);
            return new BuildPlan
            {
                Platform = platform,
                Request = BuildRequest.PatchRelease,
                CurrentVersion = "1.6.3",
                Version = new ApplicationVersion(1, 6, 4),
                VersionName = "1.6.4",
                CurrentBuildNumber = "7",
                HasBuildNumber = true,
                BuildNumber = 8,
                IsAppBundle = true,
                BuildFolder = BuildFolder,
                PlayerLocation = Path.Combine(BuildFolder, "com.company.app-1.6.4-8.aab"),
                Commit = Commit,
                ProductName = "My Game",
                ApplicationIdentifier = "com.company.app"
            };
        }
    }
}
