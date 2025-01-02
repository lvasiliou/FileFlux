using FileFlux.Services;
using FileFlux.ViewModel;
using FileFlux.Utilities;

#if WINDOWS10_0_17763_0_OR_GREATER
using Microsoft.UI.Xaml.Media;
#endif
namespace FileFlux;

public partial class MainWindow : Window
{
    public MainWindow(Page page)
    {
        InitializeComponent();
#if WINDOWS10_0_17763_0_OR_GREATER
        var wnd = this.Handler?.PlatformView as Microsoft.UI.Xaml.Window;

        if (wnd != null)
        {
            wnd.SystemBackdrop = new MicaBackdrop { Kind = Microsoft.UI.Composition.SystemBackdrops.MicaKind.BaseAlt };
        }

#endif
        this.Page = page;
        this.openSettingsButton.Clicked += OpenSettingsButtonClicked;
        this.clearDownloadsButton.Clicked += ClearDownloadsButtonClicked;
        this.newDownloadButton.Clicked += NewDownloadButtonClicked;
    }

    private void NewDownloadButtonClicked(object? sender, EventArgs e)
    {
        Utility.OpenNewDownloadWindow();
    }

    private void ClearDownloadsButtonClicked(object? sender, EventArgs e)
    {
        var downloadManager = Application.Current.Handler.MauiContext.Services.GetService<DownloadManager>();

        if (downloadManager != null)
        {
            downloadManager.ClearDownloads();
        }
    }

    private void OpenSettingsButtonClicked(object? sender, EventArgs e)
    {
        var settingsService = Application.Current.Handler.MauiContext.Services.GetService<SettingsService>();

        var settingsViewModel = new SettingsViewModel(settingsService);

        this.Page.Navigation.PushModalAsync(new SettingsPage(settingsViewModel));
    }
}
