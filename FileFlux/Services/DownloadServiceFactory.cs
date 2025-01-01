using FileFlux.Contracts;

namespace FileFlux.Services
{
    public class DownloadServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public DownloadServiceFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public IDownloadService GetService(Uri uri)
        {
            return uri.Scheme.ToLower() switch
            {
                "http" or "https" => this._serviceProvider.GetRequiredService<HttpDownloadService>(),
                _ => throw new NotSupportedException("The specified URI scheme is not supported.")
            };

        }
    }
}
