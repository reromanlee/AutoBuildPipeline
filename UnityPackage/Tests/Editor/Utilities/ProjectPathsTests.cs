using NUnit.Framework;
using System.IO;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class ProjectPathsTests
    {
        private static readonly string Root = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineTests");
        private static readonly string ProjectFolder = Path.Combine(Root, "Repository", "UnityProject");

        [Test]
        public void SiblingFolderIsOneLevelUp()
        {
            string target = Path.Combine(Root, "Repository", "UnityBuilds");

            Assert.That(ProjectPaths.ToRelative(ProjectFolder, target, true), Is.EqualTo("../UnityBuilds"));
        }

        [Test]
        public void NestedFolderIsRelative()
        {
            string target = Path.Combine(ProjectFolder, "Builds", "Android");

            Assert.That(ProjectPaths.ToRelative(ProjectFolder, target, true), Is.EqualTo("Builds/Android"));
        }

        [Test]
        public void SameFolderIsDot()
        {
            Assert.That(ProjectPaths.ToRelative(ProjectFolder, ProjectFolder, true), Is.EqualTo("."));
        }

        [Test]
        public void LetterCaseFollowsFileSystem()
        {
            string target = Path.Combine(Root, "repository", "UnityBuilds");

            Assert.That(ProjectPaths.ToRelative(ProjectFolder, target, true), Is.EqualTo("../UnityBuilds"));
            Assert.That(ProjectPaths.ToRelative(ProjectFolder, target, false), Does.StartWith("../../repository"));
        }

        [Test]
        public void RelativePathResolvesFromBaseFolder()
        {
            string expected = Path.GetFullPath(Path.Combine(Root, "Repository", "UnityBuilds"));

            Assert.That(ProjectPaths.ToAbsolute(ProjectFolder, "../UnityBuilds"), Is.EqualTo(expected));
        }

        [Test]
        public void AbsolutePathIsKept()
        {
            string absolute = Path.Combine(Root, "Elsewhere");

            Assert.That(ProjectPaths.ToAbsolute(ProjectFolder, absolute), Is.EqualTo(Path.GetFullPath(absolute)));
        }
    }
}
