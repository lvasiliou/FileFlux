using FileFlux.Contracts;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                "http" or "https" => this._serviceProvider.GetRequiredService<HttpOriginGuard>(),
                _ => throw new NotSupportedException("The specified URI scheme is not supported.")
            };

        }
    }
}
