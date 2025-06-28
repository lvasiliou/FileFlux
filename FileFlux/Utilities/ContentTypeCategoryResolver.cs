namespace FileFlux.Utilities;

public static class ContentTypeCategoryResolver
{
    public static string Resolve(string contentType)
    {
        switch (contentType?.ToLowerInvariant())
        {
            case var ct when ct.StartsWith("video/"):
                return "Video";
            case "application/x-iso9660-image":
                return "ISO";
            case var ct when ct.StartsWith("audio/"):
                return "Audio";
            case var ct when ct.StartsWith("image/"):
                return "Image";
            case var ct when ct.StartsWith("text/"):
                return "Document";
            case var ct when ct.Contains("pdf"):
                return "Document";
            case var ct when ct.Contains("zip") || ct.Contains("tar") || ct.Contains("gzip"):
                return "Archive";
            case var ct when ct.Contains("application/json") || ct.Contains("xml"):
                return "Data";
            default:
            case null:
            case "":
                return "Unknown";
        }
    }
}
