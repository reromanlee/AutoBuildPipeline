using NUnit.Framework;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.TestTools;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class AndroidSigningTests
    {
        private bool _useCustomKeystore;
        private string _keystoreName;
        private string _keyaliasName;

        [SetUp]
        public void RememberSettings()
        {
            _useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            _keystoreName = PlayerSettings.Android.keystoreName;
            _keyaliasName = PlayerSettings.Android.keyaliasName;
        }

        [TearDown]
        public void RestoreSettings()
        {
            PlayerSettings.Android.useCustomKeystore = _useCustomKeystore;
            PlayerSettings.Android.keystoreName = _keystoreName;
            PlayerSettings.Android.keyaliasName = _keyaliasName;
        }

        [Test]
        public void ApkReleaseWithoutCustomKeystoreSucceedsAndWarns()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            LogAssert.Expect(LogType.Warning, new Regex("Android APK release without a custom keystore"));

            bool prepared = AndroidSigning.TryPrepare(isRelease: true, isAppBundle: false, new BuildOverrides { IsUnattended = true });

            Assert.That(prepared, Is.True);
        }

        [Test]
        public void AppBundleReleaseWithoutCustomKeystoreSucceedsAndWarnsAboutGooglePlay()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            LogAssert.Expect(LogType.Warning, new Regex("Google Play rejects debug-signed bundles"));

            bool prepared = AndroidSigning.TryPrepare(isRelease: true, isAppBundle: true, new BuildOverrides { IsUnattended = true });

            Assert.That(prepared, Is.True);
        }

        [Test]
        public void PreviewWithoutCustomKeystoreSucceedsWithoutWarning()
        {
            PlayerSettings.Android.useCustomKeystore = false;

            bool prepared = AndroidSigning.TryPrepare(isRelease: false, isAppBundle: false, new BuildOverrides { IsUnattended = true });

            Assert.That(prepared, Is.True);
        }

        [Test]
        public void EnabledCustomKeystoreWithoutPathFails()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = string.Empty;

            Assert.Throws<BuildFailedException>(() =>
                AndroidSigning.TryPrepare(isRelease: true, isAppBundle: false, new BuildOverrides { IsUnattended = true }));
        }

        [Test]
        public void KeystoreOverrideWithMissingFileFails()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            BuildOverrides overrides = new()
            {
                KeystorePath = "nonexistent-release.keystore",
                IsUnattended = true
            };

            Assert.Throws<BuildFailedException>(() =>
                AndroidSigning.TryPrepare(isRelease: true, isAppBundle: true, overrides));
        }
    }
}
