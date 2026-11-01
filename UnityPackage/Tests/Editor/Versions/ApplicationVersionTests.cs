using NUnit.Framework;
using System;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class ApplicationVersionTests
    {
        [TestCase("1.6.3", 1, 6, 3)]
        [TestCase("1.6", 1, 6, 0)]
        [TestCase("2", 2, 0, 0)]
        [TestCase(" 1.6.3 ", 1, 6, 3)]
        [TestCase("v1.6.3", 1, 6, 3)]
        [TestCase("V0.0.1", 0, 0, 1)]
        [TestCase("10.20.30", 10, 20, 30)]
        public void ParseValidVersion(string text, int major, int minor, int patch)
        {
            Assert.That(ApplicationVersion.TryParse(text, out ApplicationVersion version), Is.True);
            Assert.That(version, Is.EqualTo(new ApplicationVersion(major, minor, patch)));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("1.6.3.4")]
        [TestCase("1..3")]
        [TestCase("1.6.")]
        [TestCase("1.6.3-dev")]
        [TestCase("1.6.3+251025")]
        [TestCase("-1.6.3")]
        [TestCase("1.-6.3")]
        [TestCase("one.two.three")]
        [TestCase("99999999999.0.0")]
        public void RejectInvalidVersion(string text)
        {
            Assert.That(ApplicationVersion.TryParse(text, out _), Is.False);
            Assert.Throws<FormatException>(() => ApplicationVersion.Parse(text));
        }

        [TestCase(VersionIncrement.None, "1.6.3")]
        [TestCase(VersionIncrement.Patch, "1.6.4")]
        [TestCase(VersionIncrement.Minor, "1.7.0")]
        [TestCase(VersionIncrement.Major, "2.0.0")]
        public void IncrementResetsLowerParts(VersionIncrement increment, string expected)
        {
            ApplicationVersion version = new(1, 6, 3);

            Assert.That(version.Increment(increment).ToString(), Is.EqualTo(expected));
        }

        [Test]
        public void IncrementBeyondLargestPartOverflows()
        {
            ApplicationVersion version = new(1, 6, int.MaxValue);

            Assert.Throws<OverflowException>(() => version.Increment(VersionIncrement.Patch));
        }

        [Test]
        public void NegativePartIsRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ApplicationVersion(1, -1, 0));
        }

        [Test]
        public void EqualVersionsMatch()
        {
            ApplicationVersion first = new(1, 6, 3);
            ApplicationVersion second = ApplicationVersion.Parse("1.6.3");

            Assert.That(first == second, Is.True);
            Assert.That(first != new ApplicationVersion(1, 6, 4), Is.True);
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }
    }
}
