using FontManager.Enums;
using FontManager.Models;
using FontManager.Services.Interfaces;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Windows.Input;
using System.Windows.Media;

namespace FontManager.ViewModels
{
    public class FontItemViewModel : INotifyPropertyChanged
    {
        private readonly FontModel _model;
        private readonly IFontInstaller _installer;
        private readonly string _cacheDir;

        public FontModel Model => _model;
        public ICommand InstallCommand { get; }
        public ICommand ToggleFavoriteCommand { get; }

        public FontItemViewModel(FontModel model, IFontInstaller installer, bool isInstalled)
        {
            _model = model;
            _installer = installer;
            _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontManager", "Cache");

            InstallCommand = new RelayCommand<object>(_ => InstallFont());
            ToggleFavoriteCommand = new RelayCommand<object>(_ => IsFavorite = !IsFavorite);

            if (isInstalled)
            {
                _model.Status = InstallStatus.Installed;
            }
        }

        public bool IsFavorite
        {
            get => _model.IsFavorite;
            set
            {
                if (_model.IsFavorite != value)
                {
                    _model.IsFavorite = value;
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
        }

        private FontFamily? _previewFontFamily;
        public FontFamily PreviewFontFamily
        {
            get
            {
                if (_previewFontFamily == null)
                {
                    InitializePreview();
                }
                return _previewFontFamily ?? new FontFamily("Segoe UI");
            }
            private set
            {
                _previewFontFamily = value;
                OnPropertyChanged(nameof(PreviewFontFamily));
            }
        }

        public string DisplayStatus => _model.Status switch
        {
            InstallStatus.NotInstalled => "インストール",
            InstallStatus.Downloading => "DL中...",
            InstallStatus.Installed => "完了",
            InstallStatus.Error => "エラー",
            _ => ""
        };

        private void InitializePreview()
        {
            if (_model.Status == InstallStatus.Installed)
            {
                _previewFontFamily = new FontFamily(_model.FamilyName);
                return;
            }

            string localPath = Path.Combine(_cacheDir, $"{_model.FamilyName}.ttf");
            if (File.Exists(localPath))
            {
                try
                {
                    _previewFontFamily = new FontFamily(new Uri($"file:///{_cacheDir}/"), $"./#{_model.FamilyName}");
                }
                catch
                {
                    _previewFontFamily = new FontFamily("Segoe UI");
                }
            }
            else
            {
                _previewFontFamily = new FontFamily("Segoe UI");
            }
        }

        private async void InstallFont()
        {
            if (_model.Status == InstallStatus.Installed || _model.Status == InstallStatus.Downloading) return;

            _model.Status = InstallStatus.Downloading;
            OnPropertyChanged(nameof(DisplayStatus));

            try
            {
                Directory.CreateDirectory(_cacheDir);
                string localPath = Path.Combine(_cacheDir, $"{_model.FamilyName}.ttf");

                if (!File.Exists(localPath))
                {
                    using var client = new HttpClient();
                    var data = await client.GetByteArrayAsync(_model.DownloadUrl);
                    await File.WriteAllBytesAsync(localPath, data);
                }

                bool result = await _installer.InstallFontAsync(localPath);

                if (result)
                {
                    _model.Status = InstallStatus.Installed;
                    PreviewFontFamily = new FontFamily(_model.FamilyName);
                }
                else
                {
                    _model.Status = InstallStatus.Error;
                }
            }
            catch
            {
                _model.Status = InstallStatus.Error;
            }

            OnPropertyChanged(nameof(DisplayStatus));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}