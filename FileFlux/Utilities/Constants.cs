namespace FileFlux.Utilities
{
    public static class Constants
    {
        #region settings
        public const string OverwriteBehaviourSettingKey = "OverwriteKey";
        public const string SaveLocationSettingKey = "SaveLocationKey";
        public const string DownloadsPersistenceFileName = "downloads.json";
        public const string AppLocalDataFolder = "FileFlux";
        public const string BytesRangeHeader = "bytes";
        internal const string InstanceKey = "FileFlux";
        #endregion

        public const string UnitBytes = "B";
        public const string UnitKiloBytes = "KB";
        public const string UnitMegaBytes = "MB";
        public const string UnitGigaBytes = "GB";
        public const string UnitTeraBytes = "TB";

        public const string LinkParameter = "url";

        #region Exceptions
        public const string SavePathEmptyException = "Save Path cannot be null or whitespace.";
        public const string DirectoryNotDeterminedException = "Directory name cannot be determined.";        
        #endregion

    }
}
