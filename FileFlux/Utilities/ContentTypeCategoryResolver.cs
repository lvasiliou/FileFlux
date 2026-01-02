namespace FileFlux.Utilities;

public static class ContentTypeCategoryResolver
{
    public static string Resolve(string contentType)
    {
        if (string.IsNullOrEmpty(contentType))
        {
            return "Unknown";
        }

        var ct = contentType.ToLowerInvariant();

        switch (ct)
        {
            case var _ when ct.StartsWith("video/"):
                return "Video";
            case "application/x-iso9660-image":
                return "ISO";
            case var _ when ct.StartsWith("audio/"):
                return "Audio";
            case var _ when ct.StartsWith("image/"):
                return "Image";
            case var _ when ct.StartsWith("text/"):
                return "Document";
            case var _ when ct.Contains("pdf"):
                return "Document";
            case var _ when ct.Contains("zip") || ct.Contains("tar") || ct.Contains("gzip"):
                return "Archive";
            case var _ when ct.Contains("application/json") || ct.Contains("xml"):
                return "Data";
            default:
                return "Unknown";
        }
    }
}
