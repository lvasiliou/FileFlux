namespace FileFlux
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            var titleBar = new TitleBar
            {
                Title = "FileFlux",
                Icon = "filefluxicon.ico"
            };
        }
    }
}
