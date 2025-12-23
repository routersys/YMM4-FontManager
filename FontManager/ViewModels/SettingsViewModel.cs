using FontManager.Settings;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace FontManager.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private const string ReleasesUrl = "https://api.github.com/repos/routersys/YMM4-FontManager/releases";
        private string _currentVersion = string.Empty;
        private string _updateStatus = string.Empty;
        private bool _isUpdateAvailable;

        public FontManagerSettings Settings => FontManagerSettings.Default;

        public string CurrentVersion
        {
            get => _currentVersion;
            set
            {
                if (_currentVersion != value)
                {
                    _currentVersion = value;
                    OnPropertyChanged();
                }
            }
        }

        public string UpdateStatus
        {
            get => _updateStatus;
            set
            {
                if (_updateStatus != value)
                {
                    _updateStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsUpdateAvailable
        {
            get => _isUpdateAvailable;
            set
            {
                if (_isUpdateAvailable != value)
                {
                    _isUpdateAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        public SettingsViewModel()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            CurrentVersion = version != null ? $"{version.Major}.{version.Minor}.{version.Build}" : "0.0.0";
            _ = CheckVersionAsync();
        }

        private async Task CheckVersionAsync()
        {
            UpdateStatus = Translate.UI_Settings_Version_Checking;
            try
            {
                using var client = new HttpClient();
                client.DefaultRequestHeaders.UserAgent.ParseAdd("YMM4-FontManager");
                var releases = await client.GetFromJsonAsync<List<GithubReleaseDto>>(ReleasesUrl);

                if (releases != null && releases.Count > 0)
                {
                    var latestTagName = releases[0].TagName ?? string.Empty;
                    var versionString = latestTagName.TrimStart('v');

                    if (Version.TryParse(versionString, out var latestVersion) && Version.TryParse(CurrentVersion, out var currentVersion))
                    {
                        if (latestVersion > currentVersion)
                        {
                            UpdateStatus = Translate.UI_Settings_Version_UpdateAvailable;
                            IsUpdateAvailable = true;
                        }
                        else
                        {
                            UpdateStatus = Translate.UI_Settings_Version_NoUpdate;
                            IsUpdateAvailable = false;
                        }
                    }
                }
            }
            catch
            {
                UpdateStatus = Translate.UI_Settings_Version_NoUpdate;
                IsUpdateAvailable = false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private class GithubReleaseDto
        {
            [JsonPropertyName("tag_name")]
            public string? TagName { get; set; }
        }
    }
}