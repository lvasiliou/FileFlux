using FileFlux.Services;
using FileFlux.ViewModel;

using System.Diagnostics;
using FileFlux.Utilities;
using FileFlux.Localization;

#if WINDOWS10_0_17763_0_OR_GREATER
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml.Media;
using Windows.UI.ViewManagement;
#endif

namespace FileFlux
{
    public partial class App : Application
    {
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider)
        {
            this._serviceProvider = serviceProvider;
            InitializeComponent();

#if WINDOWS
            var uiSettings = new UISettings();
            uiSettings.ColorValuesChanged += (sender, e) =>
            {
                this.SetAccentColor(uiSettings);
            };

            this.SetAccentColor(uiSettings);
#endif            

        }

#if WINDOWS
        private void SetAccentColor(UISettings uiSettings)
        {

            var winColor = uiSettings.GetColorValue(UIColorType.Accent);
            Color accentColor = Color.Parse(winColor.ToString());

            if (Resources.ContainsKey("PrimaryColor"))
            {
                this.Resources["PrimaryColor"] = accentColor;
            }

        }
#endif

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window? wnd = null;
            var downloadsViewModel = _serviceProvider.GetService<DownloadsViewModel>();
            if (downloadsViewModel != null)
            {
#if WINDOWS10_0_17763_0_OR_GREATER
                var mainWindow = new MainWindow(new DownloadsPage(downloadsViewModel));
                wnd = mainWindow;

                var nativeWindow = mainWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
                if (nativeWindow != null)
                {
                    nativeWindow.SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
                }
#else
                wnd = new Window(new NavigationPage(new DownloadsPage(downloadsViewModel)));
#endif
            }

            if (wnd != null)
            {
                wnd.Destroying += (s, e) =>
                {
                    var downloadManager = _serviceProvider.GetService<DownloadManager>();
                    downloadManager?.Dispose();
                };
            }
            else
            {
                throw new InvalidOperationException(App_Resources.WindowFailureException);
            }

            return wnd;
        }

        protected override void OnAppLinkRequestReceived(Uri uri)
        {
            Debugger.Break();
            var queryParameters = System.Web.HttpUtility.ParseQueryString(uri.Query);
            string? parameterValue = queryParameters?.Get(Constants.LinkParameter);
            base.OnAppLinkRequestReceived(uri);

        }
    }
}