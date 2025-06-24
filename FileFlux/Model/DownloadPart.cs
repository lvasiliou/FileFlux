namespace FileFlux.Model
{
    public class DownloadPart
    {
        public int PartNumber { get; set; }

        public long StartByte { get; set; }

        public long EndByte { get; set; }

        public long DownloadedBytes { get; set; }

        public string FilePath { get; set; }

        public bool Completed { get; set; }
    }


}
