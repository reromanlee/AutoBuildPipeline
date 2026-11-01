using System;
using System.IO;
using UnityEditor.Build;

namespace reromanlee.AutoBuildPipeline.Editor
{
    /// <summary>
    /// Moves an existing build folder aside so the new build starts from an empty folder,
    /// and puts the previous folder back when the new build fails or is cancelled.
    /// The default value guards nothing, for builds to an exact location the package does not own.
    /// </summary>
    internal readonly struct BuildFolderBackup
    {
        /// <summary>
        /// Suffix of the folder that holds the previous build while the new one is built.
        /// </summary>
        public const string BackupSuffix = ".previous";

        private readonly string _folderPath;
        private readonly string _backupPath;

        private BuildFolderBackup(string folderPath, string backupPath)
        {
            _folderPath = folderPath;
            _backupPath = backupPath;
        }

        /// <summary>
        /// Whether a previous build was moved aside.
        /// </summary>
        public bool HasBackup
        {
            get
            {
                return _backupPath != null;
            }
        }

        /// <summary>
        /// Moves the folder aside, e.g. <c>app-1.6.1-7</c> to <c>app-1.6.1-7.previous</c>.
        /// When the folder does not exist yet, only remembers it, so a failed build leaves nothing behind.
        /// </summary>
        /// <exception cref="BuildFailedException">A file in the folder is in use, e.g. by a running player.</exception>
        public static BuildFolderBackup Create(string folderPath)
        {
            if (!Directory.Exists(folderPath))
            {
                return new BuildFolderBackup(folderPath, null);
            }

            string backupPath = folderPath + BackupSuffix;
            try
            {
                // A leftover backup can only come from an interrupted build of the same folder.
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                Directory.Move(folderPath, backupPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                throw new BuildFailedException($"Could not replace '{folderPath}'. Close any program that uses it, such as a running player, and try again. {exception.Message}");
            }

            return new BuildFolderBackup(folderPath, backupPath);
        }

        /// <summary>
        /// After a successful build, deletes the previous build. Otherwise deletes what the failed build left behind
        /// and puts the previous build back in place.
        /// </summary>
        public void Complete(bool isSucceeded)
        {
            if (_folderPath == null)
            {
                return;
            }

            try
            {
                if (isSucceeded)
                {
                    if (_backupPath != null)
                    {
                        Directory.Delete(_backupPath, true);
                    }

                    return;
                }

                // The folder was empty or missing before the build, so anything in it is a partial build.
                if (Directory.Exists(_folderPath))
                {
                    Directory.Delete(_folderPath, true);
                }

                if (_backupPath != null)
                {
                    Directory.Move(_backupPath, _folderPath);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                string keptPath = _backupPath ?? _folderPath;
                BuildLog.Warning($"Could not clean up after the build; '{keptPath}' is left as it is: {exception.Message}");
            }
        }
    }
}
