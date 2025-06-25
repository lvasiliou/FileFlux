
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using FileFlux.Services;
using FileFlux.Utilities;

using Microsoft.UI.Dispatching;
using Microsoft.Maui.Controls;
using Microsoft.Windows.AppLifecycle;

using Windows.ApplicationModel.Activation;
using FileFlux.Model;


namespace FileFlux.WinUI;

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

    [STAThread]
    public static void Main(string[] args)
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        bool isRedirect = DecideRedirection();
        if (!isRedirect)
        {
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _ = new App();
            });
        }

    }

    private static bool DecideRedirection()
    {
        bool isRedirect = false;
        AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();

        ExtendedActivationKind kind = args.Kind;
        AppInstance keyInstance = AppInstance.FindOrRegisterForKey(Constants.InstanceKey);

        if (keyInstance.IsCurrent)
        {
            keyInstance.Activated += OnActivated;

        }
        else
        {
            isRedirect = true;
            keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
        }

        return isRedirect;
    }

    private static void OnActivated(object? sender, AppActivationArguments args)
    {
        ExtendedActivationKind kind = args.Kind;
        if (args.Kind == ExtendedActivationKind.Protocol && args.Data is ProtocolActivatedEventArgs)
        {
            var protocolArgs = (ProtocolActivatedEventArgs)args.Data;
            var uri = protocolArgs.Uri;
            var downloadManager = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services.GetService<DownloadManager>();

            if (downloadManager != null)
            {
                string rawUrl = uri.ToString().Replace("fileflux:", string.Empty, StringComparison.OrdinalIgnoreCase);

                var url = Uri.UnescapeDataString(rawUrl);
                var unescapedUri = new Uri(url);
#if WINDOWS
                MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _ = downloadManager.NewDownloadAsync(unescapedUri);
                });
#endif
            }
        }
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
