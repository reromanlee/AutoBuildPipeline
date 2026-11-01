using NUnit.Framework;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class CommandLineBuildTests
    {
        [TestCase("patch", VersionIncrement.Patch, true)]
        [TestCase("Minor", VersionIncrement.Minor, true)]
        [TestCase("MAJOR", VersionIncrement.Major, true)]
        [TestCase(" exact ", VersionIncrement.None, true)]
        [TestCase("preview", VersionIncrement.Patch, false)]
        public void ParseBuildType(string buildType, VersionIncrement increment, bool isRelease)
        {
            BuildRequest request = CommandLineBuild.ParseBuildType(buildType);

            Assert.That(request.VersionIncrement, Is.EqualTo(increment));
            Assert.That(request.IsRelease, Is.EqualTo(isRelease));
            Assert.That(request.IsRunAfterBuild, Is.False);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("release")]
        public void RejectUnknownBuildType(string buildType)
        {
            Assert.Throws<BuildFailedException>(() => CommandLineBuild.ParseBuildType(buildType));
        }

        [Test]
        public void ReadPackageArguments()
        {
            CommandLineArguments arguments = new(new[]
            {
                "Unity.exe", "-batchmode", "-executeMethod", "reromanlee.AutoBuildPipeline.Editor.CommandLineBuild.Run",
                "-buildType", "patch", "-BUILDOUTPUT", "../Artifacts", "-buildVersion", "1.7.0", "-buildNumber", "42"
            });

            BuildOverrides overrides = CommandLineBuild.CreateOverrides(arguments);

            Assert.That(overrides.OutputFolder, Is.EqualTo("../Artifacts"));
            Assert.That(overrides.Version, Is.EqualTo("1.7.0"));
            Assert.That(overrides.BuildNumber, Is.EqualTo("42"));
            Assert.That(overrides.PlayerLocation, Is.Null);
            Assert.That(overrides.IsUnattended, Is.True);
        }

        [Test]
        public void ReadGameCIArguments()
        {
            CommandLineArguments arguments = new(new[]
            {
                "-customBuildPath", "/github/workspace/build/Android/Android.aab",
                "-androidVersionCode", "1006004",
                "-androidKeystoreName", "user.keystore",
                "-androidKeyaliasName", "release",
                "-androidKeystorePass", "store-secret",
                "-androidKeyaliasPass", "alias-secret"
            });

            BuildOverrides overrides = CommandLineBuild.CreateOverrides(arguments);

            Assert.That(overrides.PlayerLocation, Is.EqualTo("/github/workspace/build/Android/Android.aab"));
            Assert.That(overrides.BuildNumber, Is.EqualTo("1006004"));
            Assert.That(overrides.KeystorePath, Is.EqualTo("user.keystore"));
            Assert.That(overrides.AliasName, Is.EqualTo("release"));
            Assert.That(overrides.KeystorePassword, Is.EqualTo("store-secret"));
            Assert.That(overrides.AliasPassword, Is.EqualTo("alias-secret"));
        }

        [Test]
        public void ReadNewerGameCIArgumentNames()
        {
            CommandLineArguments arguments = new(new[]
            {
                "-androidKeyAlias", "release",
                "-androidKeystorePassword", "store-secret",
                "-androidKeyAliasPassword", "alias-secret"
            });

            BuildOverrides overrides = CommandLineBuild.CreateOverrides(arguments);

            Assert.That(overrides.AliasName, Is.EqualTo("release"));
            Assert.That(overrides.KeystorePassword, Is.EqualTo("store-secret"));
            Assert.That(overrides.AliasPassword, Is.EqualTo("alias-secret"));
        }

        [Test]
        public void PackageArgumentWinsOverGameCIAlias()
        {
            CommandLineArguments arguments = new(new[] { "-androidVersionCode", "1006004", "-buildNumber", "42" });

            BuildOverrides overrides = CommandLineBuild.CreateOverrides(arguments);

            Assert.That(overrides.BuildNumber, Is.EqualTo("42"));
        }

        [Test]
        public void GameCIVersionCodeOfZeroIsNotApplied()
        {
            CommandLineArguments arguments = new(new[] { "-buildVersion", "none", "-androidVersionCode", "0" });

            BuildOverrides overrides = CommandLineBuild.CreateOverrides(arguments);

            Assert.That(overrides.BuildNumber, Is.Null);
        }

        [Test]
        public void PackageBuildNumberOfZeroIsKept()
        {
            CommandLineArguments arguments = new(new[] { "-buildNumber", "0" });

            Assert.That(CommandLineBuild.CreateOverrides(arguments).BuildNumber, Is.EqualTo("0"));
        }

        [Test]
        public void ArgumentFollowedByAnotherNameHasNoValue()
        {
            CommandLineArguments arguments = new(new[] { "-buildOutput", "-buildType", "patch", "-buildVersion" });

            Assert.That(arguments.GetValue("-buildOutput"), Is.Null);
            Assert.That(arguments.GetValue("-buildVersion"), Is.Null);
            Assert.That(arguments.GetValue("-buildType"), Is.EqualTo("patch"));
        }
    }
}
