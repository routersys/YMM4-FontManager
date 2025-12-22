using FontManager.Models;
using FontManager.Services;
using FontManager.Services.Interfaces;
using FontManager.Settings;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace FontManager.ViewModels
{
    public class FontManagerViewModel : INotifyPropertyChanged
    {
        private readonly IFontService _fontService = new GoogleFontsService();
        private readonly IFontInstaller _fontInstaller = new WinApiFontInstaller();
        private readonly FavoriteService _favoriteService = new();

        private List<FontItemViewModel> _allFonts = new();

        public ObservableCollection<FontRowViewModel> FontRows { get; } = new();
        public ObservableCollection<string> AvailableTags { get; } = new();

        private int _columnsCount = 2;
        public int ColumnsCount
        {
            get => _columnsCount;
            set
            {
                if (_columnsCount != value)
                {
                    _columnsCount = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        private string _selectedTag = "All";
        public string SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (_selectedTag != value)
                {
                    _selectedTag = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        private bool _showOnlyFavorites;
        public bool ShowOnlyFavorites
        {
            get => _showOnlyFavorites;
            set
            {
                if (_showOnlyFavorites != value)
                {
                    _showOnlyFavorites = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        private bool _showInstalledOnly;
        public bool ShowInstalledOnly
        {
            get => _showInstalledOnly;
            set
            {
                if (_showInstalledOnly != value)
                {
                    _showInstalledOnly = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        public ICommand LoadCommand { get; }

        public FontManagerViewModel()
        {
            LoadCommand = new RelayCommand<object>(async _ => await LoadFonts(true));
            _ = LoadFonts(false);
        }

        private async Task LoadFonts(bool forceRefresh)
        {
            var settings = FontManagerSettings.Default;
            var apiKey = settings.GoogleFontsApiKey;

            IEnumerable<FontModel> fonts = await _fontService.GetGoogleFontsAsync(apiKey);

            await BuildViewModels(fonts);
        }

        private async Task BuildViewModels(IEnumerable<FontModel> fonts)
        {
            _allFonts = await Task.Run(() =>
            {
                var installedFonts = _fontInstaller.GetInstalledFontNames().ToHashSet(StringComparer.OrdinalIgnoreCase);
                return fonts.Select(font =>
                {
                    bool isInstalled = installedFonts.Any(x => x.Contains(font.FamilyName, StringComparison.OrdinalIgnoreCase));
                    return new FontItemViewModel(font, _fontInstaller, _favoriteService, isInstalled);
                }).ToList();
            });

            var tags = _allFonts.SelectMany(f => f.Model.Tags).Distinct().OrderBy(t => t).ToList();
            tags.Insert(0, "All");

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableTags.Clear();
                foreach (var t in tags) AvailableTags.Add(t);
                _selectedTag = "All";
                OnPropertyChanged(nameof(SelectedTag));
            });

            UpdateRows();
        }

        private void UpdateRows()
        {
            if (_allFonts.Count == 0) return;

            Task.Run(() =>
            {
                var query = _allFonts.AsEnumerable();

                if (ShowInstalledOnly)
                {
                    query = query.Where(x => x.Model.Status == Enums.InstallStatus.Installed);
                }

                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    query = query.Where(x => x.Model.FamilyName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                if (SelectedTag != "All" && !string.IsNullOrEmpty(SelectedTag))
                {
                    query = query.Where(x => x.Model.Tags.Contains(SelectedTag));
                }

                if (ShowOnlyFavorites)
                {
                    query = query.Where(x => x.IsFavorite);
                }

                var filteredList = query.ToList();

                var rows = filteredList.Chunk(ColumnsCount > 0 ? ColumnsCount : 2)
                                   .Select(chunk => new FontRowViewModel(chunk))
                                   .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FontRows.Clear();
                    foreach (var row in rows) FontRows.Add(row);
                });
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}