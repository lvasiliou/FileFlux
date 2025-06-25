namespace FileFlux.Utilities;

using FileFlux.Model;

using System.Diagnostics.CodeAnalysis;

internal class DownloadEqualityComparer : IEqualityComparer<Download>
{
    public bool Equals(Download? x, Download? y)
    {
        return x?.Id.Equals(y?.Id) ?? false;
    }

    public int GetHashCode([DisallowNull] Download obj)
    {
        return obj.Id.GetHashCode();
    }
}
