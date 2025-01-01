using FileFlux.ViewModel;

namespace FileFlux
{
    public partial class DownloadsPage : ContentPage
    {

        private DownloadsViewModel _downloadsViewModel;

        public DownloadsPage(DownloadsViewModel downloadsViewModel)
        {
            InitializeComponent();
            this._downloadsViewModel = downloadsViewModel;
            this.BindingContext = downloadsViewModel;            
        }
    }

}
