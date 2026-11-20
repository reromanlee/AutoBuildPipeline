using NUnit.Framework;
using System;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildNamePatternTests
    {
        private static readonly DateTime FixedTime = new(2026, 11, 20, 14, 32, 0);

        [Test]
        public void DefaultReleasePatternIncludesCodeWhenPresent()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "com.company.app", code: "13", label: null);

            string folder = BuildNamePattern.FormatReleaseFolder(BuildNamePattern.DefaultReleaseFolderPattern, tokens);

            Assert.That(folder, Is.EqualTo("com.company.app-1.7.4-13"));
        }

        [Test]
        public void DefaultReleasePatternTrimsTrailingSeparatorWhenCodeIsEmpty()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: null, label: null);

            string folder = BuildNamePattern.FormatReleaseFolder(BuildNamePattern.DefaultReleaseFolderPattern, tokens);

            Assert.That(folder, Is.EqualTo("My Game-1.7.4"));
        }

        [Test]
        public void LeadingEmptyTokenTrimsFollowingSeparator()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: null, label: null);

            string folder = BuildNamePattern.FormatReleaseFolder("{code}-{name}-{version}", tokens);

            Assert.That(folder, Is.EqualTo("My Game-1.7.4"));
        }

        [Test]
        public void MiddleEmptyTokenCollapsesAdjacentSeparator()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: null, label: null);

            string folder = BuildNamePattern.FormatReleaseFolder("{name}_{code}_{version}", tokens);

            Assert.That(folder, Is.EqualTo("My Game_1.7.4"));
        }

        [Test]
        public void AllTokensResolveAndSanitize()
        {
            BuildNamePattern.Tokens tokens = new(
                name: "My Game Deluxe",
                product: "My Game: Deluxe?",
                identifier: "com.company.app",
                version: "1.7.4",
                code: "13",
                platform: "Android",
                label: "beta.1",
                timestamp: FixedTime);

            string release = BuildNamePattern.FormatReleaseFolder("{platform}-{product}-{identifier}-{version}-{code}-{date}-{time}", tokens);
            string preview = BuildNamePattern.FormatPreviewFolder("{name}-{label}-{date}", tokens);
            string player = BuildNamePattern.FormatPlayerName("{product}_{version}_{code}", tokens);

            Assert.That(release, Is.EqualTo("Android-My Game Deluxe-com.company.app-1.7.4-13-2026-11-20-1432"));
            Assert.That(preview, Is.EqualTo("My Game Deluxe-beta.1-2026-11-20"));
            Assert.That(player, Is.EqualTo("My Game Deluxe_1.7.4_13"));
        }

        [Test]
        public void WindowsReservedNameIsSanitized()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "CON", product: "CON", code: null, label: null);

            string player = BuildNamePattern.FormatPlayerName("{product}", tokens);
            string dotted = BuildNamePattern.FormatReleaseFolder("NUL.{version}", tokens);

            Assert.That(player, Is.EqualTo("CON_"));
            Assert.That(dotted, Is.EqualTo("NUL.1.7.4_"));
        }

        [Test]
        public void BlankPlayerPatternReturnsNullForPlatformDefault()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: null, label: null);

            Assert.That(BuildNamePattern.FormatPlayerName("", tokens), Is.Null);
            Assert.That(BuildNamePattern.FormatPlayerName("   ", tokens), Is.Null);
        }

        [Test]
        public void ReleasePatternWithoutVersionFails()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: "13", label: null);

            Assert.Throws<BuildFailedException>(() => BuildNamePattern.FormatReleaseFolder("{name}-{code}", tokens));
        }

        [Test]
        public void PreviewPatternWithoutLabelFails()
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: null, label: "dev");

            Assert.Throws<BuildFailedException>(() => BuildNamePattern.FormatPreviewFolder("{name}-{version}", tokens));
        }

        [TestCase("{name}-{unknown}-{version}")]
        [TestCase("{name}-{version")]
        [TestCase("{name}-version}")]
        [TestCase("{name}-{}-{version}")]
        [TestCase("{platform}/{name}-{version}")]
        [TestCase("{platform}\\{name}-{version}")]
        [TestCase("..-{version}")]
        public void InvalidPatternsAreRejected(string pattern)
        {
            BuildNamePattern.Tokens tokens = CreateTokens(name: "My Game", code: "13", label: "dev");

            Assert.Throws<BuildFailedException>(() => BuildNamePattern.FormatReleaseFolder(pattern, tokens));
        }

        private static BuildNamePattern.Tokens CreateTokens(string name, string code, string label, string product = "My Game")
        {
            return new BuildNamePattern.Tokens(
                name: name,
                product: product,
                identifier: "com.company.app",
                version: "1.7.4",
                code: code,
                platform: "Windows",
                label: label,
                timestamp: FixedTime);
        }
    }
}
