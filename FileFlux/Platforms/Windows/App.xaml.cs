
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Microsoft.Windows.AppLifecycle;

using System.Diagnostics;

using Windows.ApplicationModel.Activation;

namespace FileFlux.WinUI
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : MauiWinUIApplication
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            Debugger.Break();
            switch (args.UWPLaunchActivatedEventArgs.Kind)
            {
                case ActivationKind.Protocol:
                    var uri = args.UWPLaunchActivatedEventArgs;
                    //var queryParameters = System.Web.HttpUtility.ParseQueryString(uri.Query);
                    //string? parameterValue = queryParameters?.Get("url");
                    break;
                case ActivationKind.Launch:
                    var instance = AppInstance.FindOrRegisterForKey("FileFlux");
                    if (!instance.IsCurrent)
                    {
                        var currentInstance = AppInstance.GetCurrent();
                        _ = currentInstance.RedirectActivationToAsync(currentInstance.GetActivatedEventArgs());

                        Process.GetCurrentProcess().Kill();
                    }
                    break;
            }

            base.OnLaunched(args);
        }

    }

}
