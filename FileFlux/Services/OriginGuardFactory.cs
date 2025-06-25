using FileFlux.Contracts;
using FileFlux.Localization;
using FileFlux.Utilities;

namespace FileFlux.Services
{
    public class OriginGuardFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public OriginGuardFactory(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
        }

        public IOriginGuard GetGuard(Uri uri)
        {
            return uri.Scheme.ToLower() switch
            {
                Constants.HttpScheme or Constants.HttpsScheme => this._serviceProvider.GetRequiredService<HttpOriginGuard>(),
                _ => throw new NotSupportedException(App_Resources.UnsupportedUriError)
            };

        }
    }
}
