using FileFlux.Services;
using FileFlux.ViewModel;

#if WINDOWS10_0_17763_0_OR_GREATER
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
#endif

namespace FileFlux
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;
        public App(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            this._serviceProvider = serviceProvider;

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window wnd = null;
            var downloadsViewModel = _serviceProvider.GetService<DownloadsViewModel>();
            if (downloadsViewModel != null)
            {
#if WINDOWS10_0_17763_0_OR_GREATER
                var mainWindow = new MainWindow(new DownloadsPage(downloadsViewModel));
                wnd = mainWindow;

                var nativeWindow = mainWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    nativeWindow.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
                }
#else                
                wnd = new Window(new NavigationPage(new DownloadsPage(downloadsViewModel)));
#endif
            }

            wnd.Destroying += (s, e) =>
            {
                var downloadManager = _serviceProvider.GetService<DownloadManager>();
                downloadManager?.Dispose();
            };

            return wnd;
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            base.OnAppLinkRequestReceived(uri);
            var queryParameters = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string? parameterValue = queryParameters?.Get("url");

        }
    }
}