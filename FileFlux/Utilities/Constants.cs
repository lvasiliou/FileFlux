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
        public const string MaxConcurrentDownloadsSettingKey = "MaxConcurrentDownloads";
        #endregion

        public const string UnitBytes = "B";
        public const string UnitKiloBytes = "KB";
        public const string UnitMegaBytes = "MB";
        public const string UnitGigaBytes = "GB";
        public const string UnitTeraBytes = "TB";
        public const string DownloadsDirectoryWindowsGuid = "374DE290-123F-4565-9164-39C4925E467B";
        public const string HttpScheme = "http";
        public const string HttpsScheme = "https";
        public const string UserAgentHeader = "User-Agent";

        public const string LinkParameter = "url";

        #region Exceptions
        public const string SavePathEmptyException = "Save Path cannot be null or whitespace.";
        public const string DirectoryNotDeterminedException = "Directory name cannot be determined.";        
        #endregion

    }
}
