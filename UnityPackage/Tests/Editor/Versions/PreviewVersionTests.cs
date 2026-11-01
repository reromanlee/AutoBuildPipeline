using NUnit.Framework;
using System;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class PreviewVersionTests
    {
        private static readonly DateTime Timestamp = new(2026, 11, 1, 14, 32, 59);

        [Test]
        public void FormatWithShortCommit()
        {
            string version = PreviewVersion.Format(new ApplicationVersion(1, 6, 4), "dev", Timestamp, "3f2a9c1e5b7d9f0a1c3e5b7d9f0a1c3e5b7d9f0a");

            Assert.That(version, Is.EqualTo("1.6.4-dev+261101.1432.3f2a9c1"));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("NOT-A-HASH")]
        public void FormatWithoutCommit(string commit)
        {
            string version = PreviewVersion.Format(new ApplicationVersion(1, 6, 4), "beta.2", Timestamp, commit);

            Assert.That(version, Is.EqualTo("1.6.4-beta.2+261101.1432"));
        }

        [TestCase("dev")]
        [TestCase("beta.2")]
        [TestCase("rc-1")]
        [TestCase("0")]
        [TestCase("alpha.10.x")]
        public void AcceptValidLabel(string label)
        {
            Assert.That(PreviewVersion.IsValidLabel(label), Is.True);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("dev build")]
        [TestCase("dev_build")]
        [TestCase("beta.")]
        [TestCase(".beta")]
        [TestCase("beta..2")]
        [TestCase("beta.02")]
        [TestCase("dev+1")]
        [TestCase("тест")]
        public void RejectInvalidLabel(string label)
        {
            Assert.That(PreviewVersion.IsValidLabel(label), Is.False);
        }
    }
}
