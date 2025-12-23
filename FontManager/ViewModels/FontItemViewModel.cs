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
        private bool _canUninstall;
        private bool _isPreviewLoaded;
        private bool _isLoadingPreview;
        private bool _isEditing;

        public FontModel Model => _model;
        public ICommand InstallCommand { get; }
        public ICommand UninstallCommand { get; }
        public ICommand StartEditCommand { get; }

        private string _previewText = Translate.Preview_Text;
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

        public bool IsEditing
        {
            get => _isEditing;
            set
            {
                if (_isEditing != value)
                {
                    _isEditing = value;
                    OnPropertyChanged(nameof(IsEditing));
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
            UninstallCommand = new RelayCommand<object>(_ => UninstallFont(), _ => _canUninstall);
            StartEditCommand = new RelayCommand<object>(_ => IsEditing = true);

            if (isInstalled)
            {
                _model.Status = InstallStatus.Installed;
            }

            UpdateUninstallState();
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
            get
            {
                if (!_isPreviewLoaded && !_isLoadingPreview)
                {
                    _isLoadingPreview = true;
                    _ = Task.Run(InitializePreviewAsync);
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
            InstallStatus.NotInstalled => Translate.Status_Install,
            InstallStatus.Downloading => Translate.Status_Downloading,
            InstallStatus.Installed => Translate.Status_Installed,
            InstallStatus.Error => Translate.Status_Error,
            _ => ""
        };

        private string GetLocalFontPath()
        {
            string fileName = _model.FamilyName.Replace(" ", "_") + ".ttf";
            string targetDir;

            if (FontManagerSettings.Default.LoadToRam)
            {
                targetDir = Path.Combine(Path.GetTempPath(), "FontManagerCache");
            }
            else
            {
                targetDir = _cacheDir;
            }

            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            return Path.Combine(targetDir, fileName);
        }

        private async Task InitializePreviewAsync()
        {
            try
            {
                string localPath = GetLocalFontPath();
                string permanentPath = Path.Combine(_cacheDir, _model.FamilyName.Replace(" ", "_") + ".ttf");

                if (File.Exists(localPath))
                {
                    if (FontManagerSettings.Default.LoadToRam && _fontRamBuffer == null)
                    {
                        try { _fontRamBuffer = await File.ReadAllBytesAsync(localPath); } catch { }
                    }
                    UpdatePreview(localPath);
                    return;
                }

                if (FontManagerSettings.Default.LoadToRam && File.Exists(permanentPath))
                {
                    try
                    {
                        var cachedBytes = await File.ReadAllBytesAsync(permanentPath);
                        _fontRamBuffer = cachedBytes;
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        await File.WriteAllBytesAsync(localPath, cachedBytes);
                        UpdatePreview(localPath);
                        return;
                    }
                    catch { }
                }

                await _downloadSemaphore.WaitAsync();
                try
                {
                    if (File.Exists(localPath))
                    {
                        UpdatePreview(localPath);
                        return;
                    }

                    if (FontManagerSettings.Default.LoadToRam && File.Exists(permanentPath))
                    {
                        var cachedBytes = await File.ReadAllBytesAsync(permanentPath);
                        _fontRamBuffer = cachedBytes;
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        await File.WriteAllBytesAsync(localPath, cachedBytes);
                        UpdatePreview(localPath);
                        return;
                    }

                    using var client = new HttpClient();
                    var downloadBytes = await client.GetByteArrayAsync(_model.DownloadUrl);

                    if (FontManagerSettings.Default.LoadToRam)
                    {
                        _fontRamBuffer = downloadBytes;
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        await File.WriteAllBytesAsync(localPath, downloadBytes);

                        try
                        {
                            if (!Directory.Exists(_cacheDir)) Directory.CreateDirectory(_cacheDir);
                            await File.WriteAllBytesAsync(permanentPath, downloadBytes);
                        }
                        catch { }
                    }
                    else
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath)!);
                        await File.WriteAllBytesAsync(localPath, downloadBytes);
                    }
                }
                finally
                {
                    _downloadSemaphore.Release();
                }

                if (File.Exists(localPath))
                {
                    UpdatePreview(localPath);
                }
            }
            catch
            {

            }
            finally
            {
                _isLoadingPreview = false;
                _isPreviewLoaded = true;
            }
        }

        private void UpdatePreview(string path)
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
                var ff = new FontFamily(baseUri, $"./{fileName}#{_model.FamilyName}");

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.InvokeAsync(() => PreviewFontFamily = ff);
                }
            }
            catch
            {

            }
        }

        private void UpdateUninstallState()
        {
            if (_model.Status == InstallStatus.Installed)
            {
                _canUninstall = _installer.IsFontUninstallable(_model.FamilyName);
            }
            else
            {
                _canUninstall = false;
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private async void InstallFont()
        {
            if (_model.Status == InstallStatus.Installed || _model.Status == InstallStatus.Downloading) return;

            _model.Status = InstallStatus.Downloading;
            OnPropertyChanged(nameof(DisplayStatus));

            try
            {
                string localPath = GetLocalFontPath();
                string permanentPath = Path.Combine(_cacheDir, _model.FamilyName.Replace(" ", "_") + ".ttf");

                if (FontManagerSettings.Default.LoadToRam && _fontRamBuffer != null && !File.Exists(localPath))
                {
                    await File.WriteAllBytesAsync(localPath, _fontRamBuffer);
                }

                await _downloadSemaphore.WaitAsync();
                try
                {
                    if (!File.Exists(localPath))
                    {
                        if (FontManagerSettings.Default.LoadToRam && File.Exists(permanentPath))
                        {
                            var cachedBytes = await File.ReadAllBytesAsync(permanentPath);
                            _fontRamBuffer = cachedBytes;
                            await File.WriteAllBytesAsync(localPath, cachedBytes);
                        }
                        else
                        {
                            using var client = new HttpClient();
                            var downloadBytes = await client.GetByteArrayAsync(_model.DownloadUrl);

                            if (FontManagerSettings.Default.LoadToRam)
                            {
                                _fontRamBuffer = downloadBytes;
                                try { await File.WriteAllBytesAsync(permanentPath, downloadBytes); } catch { }
                            }

                            await File.WriteAllBytesAsync(localPath, downloadBytes);
                        }
                    }
                }
                finally
                {
                    _downloadSemaphore.Release();
                }

                bool result = await _installer.InstallFontAsync(localPath);

                if (result)
                {
                    _model.Status = InstallStatus.Installed;
                    UpdateUninstallState();
                    UpdatePreview(localPath);
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

        private async void UninstallFont()
        {
            if (_model.Status != InstallStatus.Installed) return;

            try
            {
                bool result = await _installer.UninstallFontAsync(_model.FamilyName);
                if (result)
                {
                    _model.Status = InstallStatus.NotInstalled;
                    UpdateUninstallState();
                    string localPath = GetLocalFontPath();
                    if (File.Exists(localPath))
                    {
                        UpdatePreview(localPath);
                    }
                    else
                    {
                        _ = Task.Run(InitializePreviewAsync);
                    }
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