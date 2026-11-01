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

        [SerializeField]
        private string _outputFolder = DefaultOutputFolder;

        [SerializeField]
        private string _previewLabel = PreviewVersion.DefaultLabel;

        [SerializeField]
        private bool _isReleaseConfirmationEnabled = true;

        [SerializeField]
        private bool _isBuildRevealEnabled = true;

        [SerializeField]
        private AndroidReleaseFormat _androidReleaseFormat = AndroidReleaseFormat.AppBundle;

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

        internal void SetOutputFolder(string outputFolder)
        {
            _outputFolder = outputFolder;
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
    }
}
