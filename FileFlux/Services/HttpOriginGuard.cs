using FileFlux.Contracts;
using FileFlux.Model;

using System.Net;

namespace FileFlux.Services
{
    public class HttpOriginGuard : IOriginGuard
    {
        private readonly HttpClient _httpClient;

        public HttpOriginGuard(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsResourceValidAsync(Download download)
        {
            if (download == null)
                throw new ArgumentNullException(nameof(download));

            var request = new HttpRequestMessage(HttpMethod.Head, download.Uri);

            if (!string.IsNullOrEmpty(download.ETag))
                request.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue($"\"{download.ETag.Trim('\"')}\""));

            if (download.LastModified > DateTime.MinValue)
                request.Headers.IfUnmodifiedSince = download.LastModified;

            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                // 200 OK means resource is valid and unchanged (despite conservative request headers)
                if (response.StatusCode == HttpStatusCode.OK)
                    return true;

                // 304 Not Modified would indicate the resource hasn't changed (used with If-None-Match)
                if (response.StatusCode == HttpStatusCode.NotModified)
                    return true;

                // 412 Precondition Failed = ETag or LastModified validation failed: resource has changed
                if (response.StatusCode == HttpStatusCode.PreconditionFailed)
                    return false;

                // Fallback: If we get a 2xx but ETag has changed, it's technically modified
                if (response.IsSuccessStatusCode)
                {
                    var currentEtag = response.Headers.ETag?.Tag?.Trim('"');
                    if (!string.IsNullOrEmpty(download.ETag) && currentEtag != null)
                    {
                        return string.Equals(currentEtag, download.ETag.Trim('"'), StringComparison.Ordinal);
                    }
                }

                return false; // Assume invalid if status code doesn’t confirm validity
            }
            catch
            {
                return false; // Fail closed if there's a network or protocol error
            }
        }
    }
}
