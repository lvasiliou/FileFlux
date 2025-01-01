using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FileFlux.Model;
public class FileDownload : INotifyPropertyChanged
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private Guid _id;
    private Uri _url;
    private string _fileName;
    private string _savePath;
    private double _percentCompleted;
    private FileDownloadStatuses _status;
    private byte[]? _content;
    private string? _contentType;
    private string? _errorMessage;
    private long _totalSize;
    private DateTime _lastModified;
    private DateTime _created;

    public Guid Id
    {
        get => _id;
        set => SetProperty(ref _id, value);
    }

    public Uri Url
    {
        get => _url;
        set => SetProperty(ref _url, value);
    }

    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    public string SavePath
    {
        get => _savePath;
        set => SetProperty(ref _savePath, value);
    }

    public double PercentCompleted
    {
        get => _percentCompleted;
        set => SetProperty(ref _percentCompleted, value);
    }

    public FileDownloadStatuses Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public byte[]? Content
    {
        get => _content;
        set => SetProperty(ref _content, value);
    }

    public string? ContentType
    {
        get => _contentType;
        set => SetProperty(ref _contentType, value);
    }

    public long TotalSize
    {
        get => _totalSize;
        set => SetProperty(ref _totalSize, value);
    }
    public DateTime LastModified
    {
        get => _lastModified;
        set => SetProperty(ref _lastModified, value);
    }

    public DateTime Created
    {
        get => _created;
        set => SetProperty(ref _created, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public CancellationTokenSource CancellationTokenSource
    {
        get => _cancellationTokenSource;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (!Equals(field, value))
        {
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}