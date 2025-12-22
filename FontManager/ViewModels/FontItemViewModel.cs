using FontManager.Enums;
using FontManager.Models;
using FontManager.Services;
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
        private readonly FavoriteService _favoriteService;
        private readonly string _cacheDir;

        public FontModel Model => _model;
        public ICommand InstallCommand { get; }
        public ICommand UninstallCommand { get; }

        private string _previewText = "あいうえお ABC";
        public string PreviewText
        {
            get => _previewText;
            set
            {
                if (_previewText != value)
                {
                    _previewText = value;
                    OnPropertyChanged(nameof(PreviewText));
                }
            }
        }

        public FontItemViewModel(FontModel model, IFontInstaller installer, FavoriteService favoriteService, bool isInstalled)
        {
            _model = model;
            _installer = installer;
            _favoriteService = favoriteService;
            _cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontManager", "Cache");

            _model.IsFavorite = _favoriteService.IsFavorite(_model.FamilyName);

            InstallCommand = new RelayCommand<object>(_ => InstallFont());
            UninstallCommand = new RelayCommand<object>(_ => UninstallFont());

            if (isInstalled)
            {
                _model.Status = InstallStatus.Installed;
                PreviewFontFamily = new FontFamily(_model.FamilyName);
            }
            else
            {
                InitializePreview();
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
                    _favoriteService.SetFavorite(_model.FamilyName, value);
                    OnPropertyChanged(nameof(IsFavorite));
                }
            }
        }

        private FontFamily? _previewFontFamily;
        public FontFamily PreviewFontFamily
        {
            get => _previewFontFamily ?? new FontFamily("Segoe UI");
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
            InstallStatus.Installed => "済み",
            InstallStatus.Error => "エラー",
            _ => ""
        };

        private void InitializePreview()
        {
            string localPath = Path.Combine(_cacheDir, $"{_model.FamilyName}.ttf");
            if (File.Exists(localPath))
            {
                try { PreviewFontFamily = new FontFamily(new Uri($"file:///{_cacheDir}/"), $"./#{_model.FamilyName}"); }
                catch { PreviewFontFamily = new FontFamily("Segoe UI"); }
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
                else _model.Status = InstallStatus.Error;
            }
            catch { _model.Status = InstallStatus.Error; }

            OnPropertyChanged(nameof(DisplayStatus));
        }

        private async void UninstallFont()
        {
            if (_model.Status != InstallStatus.Installed) return;

            bool result = await _installer.UninstallFontAsync(_model.FamilyName);
            if (result)
            {
                _model.Status = InstallStatus.NotInstalled;
                InitializePreview();
            }
            OnPropertyChanged(nameof(DisplayStatus));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}