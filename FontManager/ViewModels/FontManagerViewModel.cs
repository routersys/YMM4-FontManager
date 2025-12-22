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
        private List<FontItemViewModel> _allFonts = new();

        public ObservableCollection<FontRowViewModel> FontRows { get; } = new();

        public int ColumnsCount
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        } = 2;

        public string SearchText
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        } = string.Empty;

        public bool ShowOnlyFavorites
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                    UpdateRows();
                }
            }
        }

        public ICommand LoadCommand { get; }

        public FontManagerViewModel()
        {
            LoadCommand = new RelayCommand<object>(async _ => await LoadFonts());
        }

        private async Task LoadFonts()
        {
            var apiKey = FontManagerSettings.Default.GoogleFontsApiKey;
            if (string.IsNullOrEmpty(apiKey)) return;

            var googleFonts = await _fontService.GetGoogleFontsAsync(apiKey);

            _allFonts = await Task.Run(() =>
            {
                var installedFonts = _fontInstaller.GetInstalledFontNames().ToHashSet(StringComparer.OrdinalIgnoreCase);

                return googleFonts.Select(font =>
                {
                    bool isInstalled = installedFonts.Any(x => x.Contains(font.FamilyName, StringComparison.OrdinalIgnoreCase));
                    return new FontItemViewModel(font, _fontInstaller, isInstalled);
                }).ToList();
            });

            UpdateRows();
        }

        private void UpdateRows()
        {
            if (_allFonts.Count == 0) return;

            Task.Run(() =>
            {
                var filtered = _allFonts.Where(vm =>
                {
                    bool matchesSearch = string.IsNullOrWhiteSpace(SearchText) ||
                                       vm.Model.FamilyName.Contains(SearchText, StringComparison.OrdinalIgnoreCase);
                    bool matchesFavorite = !ShowOnlyFavorites || vm.IsFavorite;
                    return matchesSearch && matchesFavorite;
                });

                var rows = filtered.Chunk(ColumnsCount)
                                   .Select(chunk => new FontRowViewModel(chunk))
                                   .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    FontRows.Clear();
                    foreach (var row in rows)
                    {
                        FontRows.Add(row);
                    }
                });
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}