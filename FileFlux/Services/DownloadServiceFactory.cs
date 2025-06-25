using FileFlux.Contracts;
using FileFlux.Localization;
using FileFlux.Utilities;

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
                Constants.HttpScheme or Constants.HttpsScheme => this._serviceProvider.GetRequiredService<HttpDownloadService>(),
                _ => throw new NotSupportedException(App_Resources.UnsupportedUriError)
            };

        }
    }
}
