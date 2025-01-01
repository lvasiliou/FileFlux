using FileFlux.Services;

namespace FileFlux.ViewModel
{
    public class SettingsViewModel
    {
        private readonly SettingsService _settingsService;

        public SettingsViewModel(SettingsService settingsService) => _settingsService = settingsService;
        public bool OverwriteBehaviour
        {
            get => _settingsService.GetOverwriteBehaviour();
            set => _settingsService.SaveOverwriteBehaviour(value);
        }

        public string SaveLocation
        {
            get => _settingsService.GetSaveLocation();
            set => _settingsService.SaveSaveLocation(value);
        }



    }
}
