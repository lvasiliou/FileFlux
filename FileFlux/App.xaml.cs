using FileFlux.Services;
using FileFlux.ViewModel;
#if WINDOWS10_0_17763_0_OR_GREATER
using FileFlux.Platforms.Windows;
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
                MainWindow mainWindow = new MainWindow(new DownloadsPage(downloadsViewModel));               

                var nativeWindow = mainWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

                if (nativeWindow != null)
                {
                    nativeWindow.TryMicaOrAcrylic();
                }
                wnd = mainWindow;
#else                
                wnd = new Window(new NavigationPage(new DownloadsPage(downloadsViewModel)));
#endif

            }

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