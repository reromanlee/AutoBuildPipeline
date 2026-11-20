using NUnit.Framework;
using UnityEditor;

namespace reromanlee.AutoBuildPipeline.Editor.Tests
{
    public sealed class BuildSettingsRestorePointTests
    {
        private string _bundleVersion;
        private int _versionCode;
        private bool _isDevelopment;
        private bool _isAppBundle;
        private string _keystoreName;
        private bool _hasDebugSymbols;
        private AndroidDebugSymbols _debugSymbols;

        [SetUp]
        public void RememberSettings()
        {
            _bundleVersion = PlayerSettings.bundleVersion;
            _versionCode = PlayerSettings.Android.bundleVersionCode;
            _isDevelopment = EditorUserBuildSettings.development;
            _isAppBundle = EditorUserBuildSettings.buildAppBundle;
            _keystoreName = PlayerSettings.Android.keystoreName;
            _hasDebugSymbols = AndroidDebugSymbolsUtility.TryRead(out _debugSymbols);
        }

        [TearDown]
        public void RestoreSettings()
        {
            PlayerSettings.bundleVersion = _bundleVersion;
            PlayerSettings.Android.bundleVersionCode = _versionCode;
            EditorUserBuildSettings.development = _isDevelopment;
            EditorUserBuildSettings.buildAppBundle = _isAppBundle;
            PlayerSettings.Android.keystoreName = _keystoreName;
            if (_hasDebugSymbols)
            {
                AndroidDebugSymbolsUtility.TryWrite(_debugSymbols);
            }
        }

        [Test]
        public void RestoreRevertsEverything()
        {
            // Project starts at 1.6.3 (7), not a development build, packaged as an APK.
            ApplyProjectState();
            BuildSettingsRestorePoint restorePoint = BuildSettingsRestorePoint.Create(GetAndroid());

            // A preview changes every setting temporarily.
            ApplyBuildState();
            restorePoint.Restore(isVersionKept: false);

            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("1.6.3"));
            Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(7));
            Assert.That(EditorUserBuildSettings.development, Is.False);
            Assert.That(EditorUserBuildSettings.buildAppBundle, Is.False);
            Assert.That(PlayerSettings.Android.keystoreName, Is.EqualTo("../app.keystore"));
            if (_hasDebugSymbols)
            {
                Assert.That(AndroidDebugSymbolsUtility.TryRead(out AndroidDebugSymbols restoredSymbols), Is.True);
                Assert.That(restoredSymbols, Is.EqualTo(AndroidDebugSymbols.Off));
            }
        }

        [Test]
        public void RestoreKeepsReleasedVersion()
        {
            ApplyProjectState();
            BuildSettingsRestorePoint restorePoint = BuildSettingsRestorePoint.Create(GetAndroid());

            // A release changes the version to 1.6.4 (8) and keeps it.
            ApplyBuildState();
            restorePoint.Restore(isVersionKept: true);

            Assert.That(PlayerSettings.bundleVersion, Is.EqualTo("1.6.4"));
            Assert.That(PlayerSettings.Android.bundleVersionCode, Is.EqualTo(8));
            Assert.That(EditorUserBuildSettings.buildAppBundle, Is.False);
            Assert.That(PlayerSettings.Android.keystoreName, Is.EqualTo("../app.keystore"));
            if (_hasDebugSymbols)
            {
                Assert.That(AndroidDebugSymbolsUtility.TryRead(out AndroidDebugSymbols restoredSymbols), Is.True);
                Assert.That(restoredSymbols, Is.EqualTo(AndroidDebugSymbols.Off));
            }
        }

        [TestCase(AndroidDebugSymbols.Off)]
        [TestCase(AndroidDebugSymbols.Public)]
        [TestCase(AndroidDebugSymbols.Debugging)]
        public void AndroidDebugSymbolsRoundTrip(AndroidDebugSymbols level)
        {
            if (!_hasDebugSymbols)
            {
                Assert.Ignore("Android Build Support is not installed in this editor.");
            }

            Assert.That(AndroidDebugSymbolsUtility.TryWrite(level), Is.True);
            Assert.That(AndroidDebugSymbolsUtility.TryRead(out AndroidDebugSymbols actual), Is.True);
            Assert.That(actual, Is.EqualTo(level));
        }

        private static void ApplyProjectState()
        {
            PlayerSettings.bundleVersion = "1.6.3";
            PlayerSettings.Android.bundleVersionCode = 7;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.Android.keystoreName = "../app.keystore";
            AndroidDebugSymbolsUtility.TryWrite(AndroidDebugSymbols.Off);
        }

        private static void ApplyBuildState()
        {
            PlayerSettings.bundleVersion = "1.6.4";
            PlayerSettings.Android.bundleVersionCode = 8;
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.buildAppBundle = true;
            PlayerSettings.Android.keystoreName = "C:/Keys/app.keystore";
            AndroidDebugSymbolsUtility.TryWrite(AndroidDebugSymbols.Public);
        }

        private static BuildPlatform GetAndroid()
        {
            Assert.That(BuildPlatforms.TryGet(BuildTarget.Android, out BuildPlatform platform), Is.True);
            return platform;
        }
    }
}
