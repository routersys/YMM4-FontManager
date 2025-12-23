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

        public ObservableCollection<TagGroupViewModel> TagGroups { get; } = new();
        public ObservableCollection<string> VisibleTags { get; } = new();

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

        private string _selectedTag = Translate.Tag_All;
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

        private TagGroupViewModel? _selectedTagGroup;
        public TagGroupViewModel? SelectedTagGroup
        {
            get => _selectedTagGroup;
            set
            {
                if (_selectedTagGroup != value)
                {
                    _selectedTagGroup = value;
                    OnPropertyChanged();
                    UpdateVisibleTags();
                }
            }
        }

        private string _tagSearchText = string.Empty;
        public string TagSearchText
        {
            get => _tagSearchText;
            set
            {
                if (_tagSearchText != value)
                {
                    _tagSearchText = value;
                    OnPropertyChanged();
                    UpdateVisibleTags();
                }
            }
        }

        private bool _isTagPopupOpen;
        public bool IsTagPopupOpen
        {
            get => _isTagPopupOpen;
            set
            {
                if (_isTagPopupOpen != value)
                {
                    _isTagPopupOpen = value;
                    OnPropertyChanged();
                    if (value)
                    {
                        TagSearchText = string.Empty;
                    }
                }
            }
        }

        private bool _isSideMenuTagPopupOpen;
        public bool IsSideMenuTagPopupOpen
        {
            get => _isSideMenuTagPopupOpen;
            set
            {
                if (_isSideMenuTagPopupOpen != value)
                {
                    _isSideMenuTagPopupOpen = value;
                    OnPropertyChanged();
                    if (value)
                    {
                        TagSearchText = string.Empty;
                    }
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
        public ICommand SelectTagCommand { get; }

        public FontManagerViewModel()
        {
            LoadCommand = new RelayCommand<object>(async _ => await LoadFonts(true));
            SelectTagCommand = new RelayCommand<string>(tag =>
            {
                SelectedTag = tag ?? Translate.Tag_All;
                IsTagPopupOpen = false;
                IsSideMenuTagPopupOpen = false;
            });
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

            var allTags = _allFonts.SelectMany(f => f.Model.Tags).Distinct().OrderBy(t => t).ToList();
            var categories = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "serif", "sans-serif", "display", "handwriting", "monospace" };
            var styleTags = allTags.Where(t => categories.Contains(t)).ToList();
            var subsetTags = allTags.Where(t => !categories.Contains(t)).ToList();

            Application.Current.Dispatcher.Invoke(() =>
            {
                AvailableTags.Clear();
                AvailableTags.Add(Translate.Tag_All);
                foreach (var t in allTags) AvailableTags.Add(t);

                TagGroups.Clear();
                TagGroups.Add(new TagGroupViewModel { Name = Translate.Tag_All, Tags = allTags });
                TagGroups.Add(new TagGroupViewModel { Name = Translate.TagGroup_Categories, Tags = styleTags });
                TagGroups.Add(new TagGroupViewModel { Name = Translate.TagGroup_Subsets, Tags = subsetTags });

                SelectedTagGroup = TagGroups.First();
                _selectedTag = Translate.Tag_All;
                OnPropertyChanged(nameof(SelectedTag));
            });

            UpdateRows();
        }

        private void UpdateVisibleTags()
        {
            if (SelectedTagGroup == null) return;

            var query = SelectedTagGroup.Tags.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(TagSearchText))
            {
                query = query.Where(t => t.Contains(TagSearchText, StringComparison.OrdinalIgnoreCase));
            }

            var list = query.ToList();

            if (SelectedTagGroup.Name == Translate.Tag_All && string.IsNullOrEmpty(TagSearchText))
            {
                if (!list.Contains(Translate.Tag_All))
                {
                    list.Insert(0, Translate.Tag_All);
                }
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                VisibleTags.Clear();
                foreach (var t in list) VisibleTags.Add(t);
            });
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

                if (SelectedTag != Translate.Tag_All && !string.IsNullOrEmpty(SelectedTag))
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

    public class TagGroupViewModel
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
    }
}