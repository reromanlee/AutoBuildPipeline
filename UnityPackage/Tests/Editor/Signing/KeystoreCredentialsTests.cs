using NUnit.Framework;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class KeystoreCredentialsTests
    {
        [TestCase("store", "alias")]
        [TestCase("with \"quotes\" and \\slashes\\", "line\nbreak")]
        [TestCase("dots.and=equals", "пароль密码🔑")]
        public void SerializedPasswordsRoundTrip(string keystorePassword, string aliasPassword)
        {
            KeystoreCredentials credentials = new(keystorePassword, aliasPassword);

            Assert.That(KeystoreCredentials.TryDeserialize(credentials.Serialize(), out KeystoreCredentials restored), Is.True);
            Assert.That(restored.KeystorePassword, Is.EqualTo(keystorePassword));
            Assert.That(restored.AliasPassword, Is.EqualTo(aliasPassword));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("no-separator")]
        [TestCase(".c2Vjb25k")]
        [TestCase("Zmlyc3Q=.")]
        [TestCase("not base64!.c2Vjb25k")]
        public void MalformedValueIsRejected(string text)
        {
            Assert.That(KeystoreCredentials.TryDeserialize(text, out KeystoreCredentials credentials), Is.False);
            Assert.That(credentials.IsComplete, Is.False);
        }

        [Test]
        public void AliasPasswordFallsBackToKeystorePassword()
        {
            KeystoreCredentials credentials = KeystoreCredentials.FromPasswords("store", null);

            Assert.That(credentials.AliasPassword, Is.EqualTo("store"));
            Assert.That(credentials.IsComplete, Is.True);
        }

        [Test]
        public void MissingKeystorePasswordIsIncomplete()
        {
            Assert.That(KeystoreCredentials.FromPasswords(null, "alias").IsComplete, Is.False);
        }
    }
}
