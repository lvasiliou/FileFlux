using FileFlux.ViewModel;

using System.Windows.Input;

namespace FileFlux.Pages;

public partial class NewDownloadPage : ContentPage
{
    private readonly NewDownloadViewModel _newDownloadViewModel;

    public NewDownloadPage(NewDownloadViewModel newDownloadViewModel)
    {
        InitializeComponent();
        this._newDownloadViewModel = newDownloadViewModel;
        this.BindingContext = this._newDownloadViewModel;
        this.ErrorLabel.PropertyChanged += ErrorLabelPropertyChanged;
    }

    private void ErrorLabelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(this.ErrorLabel.Text))
        {
            this.ErrorLabel.IsVisible = !string.IsNullOrEmpty(this.ErrorLabel.Text);
        }
    }
}