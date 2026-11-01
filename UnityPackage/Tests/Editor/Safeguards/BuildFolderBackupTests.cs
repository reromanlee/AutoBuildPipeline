using NUnit.Framework;
using System;
using System.IO;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildFolderBackupTests
    {
        private const string PreviousContent = "previous build";
        private const string NewContent = "new build";

        private string _rootFolder;
        private string _buildFolder;

        [SetUp]
        public void CreateFolders()
        {
            _rootFolder = Path.Combine(Path.GetTempPath(), "AutoBuildPipelineTests", Guid.NewGuid().ToString("N"));
            _buildFolder = Path.Combine(_rootFolder, "My Game-1.6.4");
            Directory.CreateDirectory(_rootFolder);
        }

        [TearDown]
        public void DeleteFolders()
        {
            if (Directory.Exists(_rootFolder))
            {
                Directory.Delete(_rootFolder, true);
            }
        }

        [Test]
        public void SuccessReplacesPreviousBuild()
        {
            // Previous build exists (player.txt = previous).
            WritePlayer(PreviousContent);

            // Backup moves it aside, so the build starts from an empty folder.
            BuildFolderBackup backup = BuildFolderBackup.Create(_buildFolder);
            Assert.That(backup.HasBackup, Is.True);
            Assert.That(Directory.Exists(_buildFolder), Is.False);

            // New build succeeds (player.txt = new, backup deleted).
            WritePlayer(NewContent);
            backup.Complete(true);

            Assert.That(ReadPlayer(), Is.EqualTo(NewContent));
            Assert.That(Directory.Exists(_buildFolder + BuildFolderBackup.BackupSuffix), Is.False);
        }

        [Test]
        public void FailureRestoresPreviousBuild()
        {
            // Previous build exists (player.txt = previous).
            WritePlayer(PreviousContent);
            BuildFolderBackup backup = BuildFolderBackup.Create(_buildFolder);

            // New build fails halfway (player.txt = new, partial).
            WritePlayer(NewContent);
            backup.Complete(false);

            // The partial build is gone and the previous one is back (player.txt = previous).
            Assert.That(ReadPlayer(), Is.EqualTo(PreviousContent));
            Assert.That(Directory.Exists(_buildFolder + BuildFolderBackup.BackupSuffix), Is.False);
        }

        [Test]
        public void FailureOfFirstBuildLeavesNothing()
        {
            BuildFolderBackup backup = BuildFolderBackup.Create(_buildFolder);
            Assert.That(backup.HasBackup, Is.False);

            // First build fails halfway, leaving a partial folder.
            WritePlayer(NewContent);
            backup.Complete(false);

            Assert.That(Directory.Exists(_buildFolder), Is.False);
        }

        [Test]
        public void LeftoverBackupIsReplaced()
        {
            // An interrupted build left an old backup behind.
            Directory.CreateDirectory(_buildFolder + BuildFolderBackup.BackupSuffix);
            WritePlayer(PreviousContent);

            BuildFolderBackup backup = BuildFolderBackup.Create(_buildFolder);
            backup.Complete(false);

            Assert.That(ReadPlayer(), Is.EqualTo(PreviousContent));
        }

        [Test]
        public void DefaultValueGuardsNothing()
        {
            WritePlayer(PreviousContent);
            BuildFolderBackup backup = default;

            backup.Complete(false);

            Assert.That(ReadPlayer(), Is.EqualTo(PreviousContent));
        }

        private void WritePlayer(string content)
        {
            Directory.CreateDirectory(_buildFolder);
            File.WriteAllText(Path.Combine(_buildFolder, "player.txt"), content);
        }

        private string ReadPlayer()
        {
            return File.ReadAllText(Path.Combine(_buildFolder, "player.txt"));
        }
    }
}
