using FontManager.Enums;
using FontManager.Models;
using FontManager.Services;
using FontManager.Services.Interfaces;
using FontManager.Settings;
using System.ComponentModel;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Windows;
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
        private static readonly SemaphoreSlim _downloadSemaphore = new(4, 4);
        private byte[]? _fontRamBuffer;

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

            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            _cacheDir = Path.Combine(assemblyPath, "Cache", "Fonts");
            _cacheDir = Path.GetFullPath(_cacheDir);

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
                _ = InitializePreviewAsync();
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

        private async Task InitializePreviewAsync()
        {
            string fileName = _model.FamilyName.Replace(" ", "_") + ".ttf";
            string targetDir;

            if (FontManagerSettings.Default.LoadToRam)
            {
                targetDir = Path.GetTempPath();
            }
            else
            {
                targetDir = _cacheDir;
            }

            targetDir = Path.GetFullPath(targetDir);
            string localPath = Path.Combine(targetDir, fileName);

            if (File.Exists(localPath))
            {
                if (FontManagerSettings.Default.LoadToRam)
                {
                    try { _fontRamBuffer = await File.ReadAllBytesAsync(localPath); } catch { }
                }
                UpdatePreview(localPath);
                return;
            }

            await Task.Run(async () =>
            {
                await _downloadSemaphore.WaitAsync();
                try
                {
                    if (File.Exists(localPath)) return;

                    using var client = new HttpClient();
                    var data = await client.GetByteArrayAsync(_model.DownloadUrl);

                    if (FontManagerSettings.Default.LoadToRam)
                    {
                        _fontRamBuffer = data;
                    }

                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    await File.WriteAllBytesAsync(localPath, data);
                }
                catch { }
                finally
                {
                    _downloadSemaphore.Release();
                }
            });

            if (File.Exists(localPath))
            {
                UpdatePreview(localPath);
            }
        }

        private void UpdatePreview(string path)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    var dir = Path.GetDirectoryName(path) ?? string.Empty;
                    var fileName = Path.GetFileName(path);

                    if (!dir.EndsWith(Path.DirectorySeparatorChar.ToString()))
                    {
                        dir += Path.DirectorySeparatorChar;
                    }

                    var baseUri = new Uri(dir);
                    PreviewFontFamily = new FontFamily(baseUri, $"./{fileName}#{_model.FamilyName}");
                }
                catch
                {
                    PreviewFontFamily = new FontFamily("Segoe UI");
                }
            });
        }

        private async void InstallFont()
        {
            if (_model.Status == InstallStatus.Installed || _model.Status == InstallStatus.Downloading) return;

            _model.Status = InstallStatus.Downloading;
            OnPropertyChanged(nameof(DisplayStatus));

            try
            {
                if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                string localPath = Path.Combine(_cacheDir, $"{_model.FamilyName.Replace(" ", "_")}.ttf");

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
                _ = InitializePreviewAsync();
            }
            OnPropertyChanged(nameof(DisplayStatus));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}