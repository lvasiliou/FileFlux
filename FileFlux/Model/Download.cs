using FileFlux.Utilities;

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FileFlux.Model;
public class Download : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    private Guid _id = Guid.NewGuid();

    public Guid Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    private Uri? _uri;
    public Uri? Uri
    {
        get => _uri;
        set { _uri = value; OnPropertyChanged(); }
    }

    private string _fileName = string.Empty;
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set { _filePath = value; OnPropertyChanged(); }
    }


    private long _totalBytes;
    public long TotalBytes
    {
        get => _totalBytes;
        set { _totalBytes = value; OnPropertyChanged(); }
    }

    private long _bytesDownloaded;
    public long BytesDownloaded
    {
        get => _bytesDownloaded;
        set { _bytesDownloaded = value; OnPropertyChanged(); }
    }

    private int _optimalBufferSize = 8192; // Default buffer size
    public int OptimalBufferSize
    {
        get => _optimalBufferSize;
        set { _optimalBufferSize = value; OnPropertyChanged(); }
    }

    private string? _contentType;
    public string? ContentType
    {
        get => _contentType;
        set { _contentType = value; OnPropertyChanged(); OnPropertyChanged(nameof(ContentCategory)); }
    }
    private double _perrcentCompleted;
    public double PercentCompleted
    {
        get => _perrcentCompleted;
        set { _perrcentCompleted = value; OnPropertyChanged(); }
    }

    private bool _supportsResume;
    public bool SupportsResume
    {
        get => _supportsResume;
        set { _supportsResume = value; OnPropertyChanged(); }
    }

    private DateTimeOffset? _lastModified;
    public DateTimeOffset? LastModified
    {
        get => _lastModified;
        set { _lastModified = value; OnPropertyChanged(); }
    }

    private string? _etag;
    public string? ETag
    {
        get => _etag;
        set { _etag = value; OnPropertyChanged(); }
    }

    private FileDownloadStatuses _status = FileDownloadStatuses.Pending;
    public FileDownloadStatuses Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    private DateTimeOffset _dateAdded = DateTimeOffset.UtcNow;
    public DateTimeOffset DateAdded
    {
        get => _dateAdded;
        set { _dateAdded = value; OnPropertyChanged(); }
    }

    private DateTimeOffset? _dateCrreated;
    public DateTimeOffset? DateCreated
    {
        get => _dateCrreated;
        set { _dateCrreated = value; OnPropertyChanged(); }
    }

    private DateTimeOffset? _dateCompleted;
    public DateTimeOffset? DateCompleted
    {
        get => _dateCompleted;
        set { _dateCompleted = value; OnPropertyChanged(); }
    }

    private int? _optimalChunks;
    public int? OptimalChunks
    {
        get => _optimalChunks;
        set { _optimalChunks = value; OnPropertyChanged(); }
    }

    private string? _errorMessage;

    [JsonIgnore]
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    [JsonIgnore]
    public CancellationTokenSource CancellationTokenSource { get; set; } = new();

    public List<DownloadPart> Parts { get; set; } = new();

    // Human-readable content category (e.g., Video, Audio, Document)
    public string ContentCategory =>
        ContentTypeCategoryResolver.Resolve(ContentType ?? string.Empty);
}