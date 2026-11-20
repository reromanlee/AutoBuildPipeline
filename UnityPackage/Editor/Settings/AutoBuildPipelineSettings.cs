using UnityEditor;
using UnityEngine;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Project-wide build settings, edited in <c>Project Settings ▸ Auto Build Pipeline</c> and stored as text in
    /// <c>ProjectSettings/AutoBuildPipelineSettings.asset</c>, so every machine and CI builds the same way.
    /// Until the page is first changed, the file does not exist and the defaults apply.
    /// </summary>
    [FilePath("ProjectSettings/AutoBuildPipelineSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class AutoBuildPipelineSettings : ScriptableSingleton<AutoBuildPipelineSettings>
    {
        /// <summary>
        /// Default root folder of all builds, relative to the project folder.
        /// </summary>
        public const string DefaultOutputFolder = "Builds";

        /// <summary>
        /// Default pattern for release build folders.
        /// </summary>
        public const string DefaultReleaseFolderPattern = BuildNamePattern.DefaultReleaseFolderPattern;

        /// <summary>
        /// Default pattern for preview build folders.
        /// </summary>
        public const string DefaultPreviewFolderPattern = BuildNamePattern.DefaultPreviewFolderPattern;

        [SerializeField]
        private string _outputFolder = DefaultOutputFolder;

        [SerializeField]
        private string _releaseFolderPattern = DefaultReleaseFolderPattern;

        [SerializeField]
        private string _previewFolderPattern = DefaultPreviewFolderPattern;

        [SerializeField]
        private string _playerNamePattern = string.Empty;

        [SerializeField]
        private string _previewLabel = PreviewVersion.DefaultLabel;

        [SerializeField]
        private bool _isReleaseConfirmationEnabled = true;

        [SerializeField]
        private bool _isBuildRevealEnabled = true;

        [SerializeField]
        private AndroidReleaseFormat _androidReleaseFormat = AndroidReleaseFormat.AppBundle;

        [SerializeField]
        private AndroidDebugSymbols _androidReleaseSymbols = AndroidDebugSymbols.Public;

        /// <summary>
        /// Root folder of all builds; each platform gets a subfolder, e.g. <c>Builds/Android</c>.
        /// Relative paths start at the project folder. Returns <see cref="DefaultOutputFolder"/> when blank.
        /// </summary>
        public string OutputFolder
        {
            get
            {
                return string.IsNullOrWhiteSpace(_outputFolder) ? DefaultOutputFolder : _outputFolder.Trim();
            }
        }

        /// <summary>
        /// Folder name pattern for release builds under <c>&lt;OutputFolder&gt;/&lt;Platform&gt;/</c>.
        /// Must include <c>{version}</c>. Returns <see cref="DefaultReleaseFolderPattern"/> when blank.
        /// </summary>
        public string ReleaseFolderPattern
        {
            get
            {
                return string.IsNullOrWhiteSpace(_releaseFolderPattern) ? DefaultReleaseFolderPattern : _releaseFolderPattern.Trim();
            }
        }

        /// <summary>
        /// Folder name pattern for preview builds under <c>&lt;OutputFolder&gt;/&lt;Platform&gt;/</c>.
        /// Must include <c>{label}</c>. Returns <see cref="DefaultPreviewFolderPattern"/> when blank.
        /// </summary>
        public string PreviewFolderPattern
        {
            get
            {
                return string.IsNullOrWhiteSpace(_previewFolderPattern) ? DefaultPreviewFolderPattern : _previewFolderPattern.Trim();
            }
        }

        /// <summary>
        /// Optional player file (or WebGL folder) name pattern without extension.
        /// Empty uses each platform's default (the build folder name on Android, the product name elsewhere).
        /// </summary>
        public string PlayerNamePattern
        {
            get
            {
                return string.IsNullOrWhiteSpace(_playerNamePattern) ? string.Empty : _playerNamePattern.Trim();
            }
        }

        /// <summary>
        /// Pre-release label of preview versions, e.g. <c>dev</c> in <c>1.6.1-dev+251025.1432</c>.
        /// Returns <c>dev</c> when blank.
        /// </summary>
        public string PreviewLabel
        {
            get
            {
                return string.IsNullOrWhiteSpace(_previewLabel) ? PreviewVersion.DefaultLabel : _previewLabel.Trim();
            }
        }

        /// <summary>
        /// Whether releases started from the editor ask for confirmation, showing the version change.
        /// Command line builds never ask.
        /// </summary>
        public bool IsReleaseConfirmationEnabled
        {
            get
            {
                return _isReleaseConfirmationEnabled;
            }
        }

        /// <summary>
        /// Whether the editor shows a build in Explorer or Finder after a build that is not launched.
        /// </summary>
        public bool IsBuildRevealEnabled
        {
            get
            {
                return _isBuildRevealEnabled;
            }
        }

        /// <summary>
        /// Package format of Android release builds.
        /// </summary>
        public AndroidReleaseFormat AndroidReleaseFormat
        {
            get
            {
                return _androidReleaseFormat;
            }
        }

        /// <summary>
        /// Native debug symbols level applied during Android release builds and restored afterwards.
        /// </summary>
        public AndroidDebugSymbols AndroidReleaseSymbols
        {
            get
            {
                return _androidReleaseSymbols;
            }
        }

        internal void SetOutputFolder(string outputFolder)
        {
            _outputFolder = outputFolder;
            Save(true);
        }

        internal void SetReleaseFolderPattern(string pattern)
        {
            _releaseFolderPattern = pattern;
            Save(true);
        }

        internal void SetPreviewFolderPattern(string pattern)
        {
            _previewFolderPattern = pattern;
            Save(true);
        }

        internal void SetPlayerNamePattern(string pattern)
        {
            _playerNamePattern = pattern;
            Save(true);
        }

        internal void SetPreviewLabel(string previewLabel)
        {
            _previewLabel = previewLabel;
            Save(true);
        }

        internal void SetReleaseConfirmationEnabled(bool isEnabled)
        {
            _isReleaseConfirmationEnabled = isEnabled;
            Save(true);
        }

        internal void SetBuildRevealEnabled(bool isEnabled)
        {
            _isBuildRevealEnabled = isEnabled;
            Save(true);
        }

        internal void SetAndroidReleaseFormat(AndroidReleaseFormat format)
        {
            _androidReleaseFormat = format;
            Save(true);
        }

        internal void SetAndroidReleaseSymbols(AndroidDebugSymbols symbols)
        {
            _androidReleaseSymbols = symbols;
            Save(true);
        }
    }
}
