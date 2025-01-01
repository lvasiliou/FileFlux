using CommunityToolkit.Maui.Storage;

using FileFlux.Services;
using FileFlux.ViewModel;

namespace FileFlux;

public partial class SettingsPage : ContentPage
{
    private SettingsViewModel _settingsViewModel;
    public SettingsPage(SettingsViewModel settingsViewModel)
    {
        InitializeComponent();
        this._settingsViewModel = settingsViewModel;
        this.BindingContext = this._settingsViewModel;
    }

    private void BrowseSaveLocationCLicked(object sender, EventArgs e)
    {
        this.PickFolder(new CancellationToken());
    }

    private async void PickFolder(CancellationToken cancellationToken)
    {

        await FolderPicker.Default.PickAsync(this._settingsViewModel.SaveLocation, cancellationToken).ContinueWith(t =>
        {
            if (t.Result != null)
            {
                if (string.Compare(t.Result?.Folder?.Path, this._settingsViewModel.SaveLocation, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    this._settingsViewModel.SaveLocation = t.Result?.Folder?.Path ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

            }
        });

    }

    private void SaveSettingsClicked(object sender, EventArgs e)
    {
        this.Navigation.PopModalAsync();
    }
}