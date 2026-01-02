using FileFlux.Model;
using FileFlux.Pages;
using FileFlux.Services;
using FileFlux.ViewModel;

#if WINDOWS
using System.Runtime.InteropServices;
#endif

namespace FileFlux.Utilities
{
    internal class Utility
    {
        public static void OpenNewDownloadWindow(DownloadManager downloadManager)
        {
            string url = string.Empty;
            var window = App.Current?.Windows[0] as Window;
            var newDownloadm = new NewDownloadViewModel(downloadManager);
            if (window != null)
            {
                window.Page?.Navigation.PushModalAsync(new NewDownloadPage(newDownloadm)).ContinueWith(x =>
                {
                    Download? fileDownload = newDownloadm.FileDownload;

                    if (fileDownload != null)
                    {
                        MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            _ = downloadManager.ResumeDownloadAsync(fileDownload);
                        });
                    }

                });
            }
        }

        public static void OpenSettingsWindow(SettingsViewModel settingsViewModel)
        {
            var window = App.Current?.Windows[0] as Window;
            if (window != null)
            {
                window.Page?.Navigation.PushModalAsync(new SettingsPage(settingsViewModel));
            }
        }

#if WINDOWS
        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint flags, IntPtr token, out IntPtr path);
#endif

        public static string GetDownloadsDirectory()
        {
            string saveLocation = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

#if WINDOWS
            var downloadsGuid = new Guid(Constants.DownloadsDirectoryWindowsGuid);
            SHGetKnownFolderPath(downloadsGuid, 0, IntPtr.Zero, out var outPath);
            var path = Marshal.PtrToStringUni(outPath);
            Marshal.FreeCoTaskMem(outPath);
            if (!string.IsNullOrWhiteSpace(path))
            {
                saveLocation = path;
            }

#elif ANDROID
            saveLocation = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
#endif

                return saveLocation;
            }
        }
    }
