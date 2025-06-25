namespace FileFlux.Utilities;

public static class ContentTypeCategoryResolver
{
    public static string Resolve(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType)) return "Unknown";

        if (contentType.StartsWith("video/")) return "Video";
        if (contentType.StartsWith("audio/")) return "Audio";
        if (contentType.StartsWith("image/")) return "Image";
        if (contentType.StartsWith("text/")) return "Document";
        if (contentType.Contains("pdf")) return "Document";
        if (contentType.Contains("zip") || contentType.Contains("tar") || contentType.Contains("gzip")) return "Archive";
        if (contentType.Contains("application/json") || contentType.Contains("xml")) return "Data";

        return "Other";
    }
}
