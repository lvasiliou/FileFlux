
using FileFlux.Model;

namespace FileFlux.Services
{
    public class SettingsService
    {
        private const string overwriteBehaviourKey = "OverwriteKey";
        private const string saveLocationKey = "SaveLocationKey";

        public bool GetOverwriteBehaviour() => GetSetting(overwriteBehaviourKey, false);
        public bool SaveOverwriteBehaviour(bool value) => SaveSetting(overwriteBehaviourKey, value);

        public string GetSaveLocation() => GetSetting(saveLocationKey, Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        public bool SaveSaveLocation(string value) => SaveSetting(saveLocationKey, value);

        public T GetSetting<T>(string key, T defaultValue)
        {

            object value = defaultValue switch
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

            T? returnT = (T)Convert.ChangeType(value, typeof(T));
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

        internal void PauseDownload(FileDownload download)
        {
            throw new NotImplementedException();
        }
    }
}