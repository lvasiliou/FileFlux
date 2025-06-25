namespace FileFlux.Model
{
    public class DownloadPart
    {
        public int Index { get; set; }
        public long Start { get; set; }
        public long End { get; set; }
        public string PartFilePath { get; set; } = string.Empty;
        
        public long DownloadedBytes { get; set; }

        public bool Completed { get; set; } = false;
    }
}