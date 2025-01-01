using FileFlux.Services;
using FileFlux.ViewModel;

namespace FileFlux;

public partial class MainWindow : Window
{
    public MainWindow(Page page)
    {
        InitializeComponent();
        this.Page = page;
        this.openSettingsButton.Clicked += OpenSettingsButtonClicked;
    }

    private void OpenSettingsButtonClicked(object? sender, EventArgs e)
    {
        var settingsService = Application.Current.Handler.MauiContext.Services.GetService<SettingsService>();
        var settingsViewModel = new SettingsViewModel(settingsService);

        this.Page.Navigation.PushModalAsync(new SettingsPage(settingsViewModel));
    }
}
