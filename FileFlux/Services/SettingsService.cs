
using FileFlux.Model;
using FileFlux.Utilities;

namespace FileFlux.Services
{
    public class SettingsService
    {

        public bool GetOverwriteBehaviour() => GetSetting(Constants.OverwriteBehaviourSettingKey, false);
        public bool SaveOverwriteBehaviour(bool value) => SaveSetting(Constants.OverwriteBehaviourSettingKey, value);

        public string GetSaveLocation() => Utility.GetDownloadsDirectory();
        public bool SaveSaveLocation(string value) => SaveSetting(Constants.SaveLocationSettingKey, value);

        public int GetMaxConcurrentDownloads() => GetSetting("MaxConcurrentDownloads", 8);
        public bool SaveMaxConcurrentDownloads(int value) => SaveSetting("MaxConcurrentDownloads", value);

        public T GetSetting<T>(string key, T defaultValue)
        {
            object? value = defaultValue switch
            {
                string => Preferences.Get(key, defaultValue as string),
                bool => Preferences.Get(key, Convert.ToBoolean(defaultValue)),
                int => Preferences.Get(key, Convert.ToInt32(defaultValue)),
                double => Preferences.Get(key, Convert.ToDouble(defaultValue)),
                float => Preferences.Get(key, Convert.ToSingle(defaultValue)),
                long => Preferences.Get(key, Convert.ToInt64(defaultValue)),
                DateTime => Preferences.Get(key, Convert.ToDateTime(defaultValue)),
                DateTimeOffset => Preferences.Get(key, (DateTimeOffset)Convert.ChangeType(defaultValue, typeof(DateTimeOffset))),
                _ => throw new NotSupportedException($"Type {typeof(T)} is not supported")
            };

            if (value is null)
            {
                return defaultValue;
            }

            T returnT = (T)Convert.ChangeType(value, typeof(T));
            return returnT;
        }

        public bool SaveSetting(string key, dynamic value)
        {
            try
            {
                Preferences.Set(key, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void PauseDownload(Download download)
        {
            throw new NotImplementedException();
        }
    }
}