using FontManager.Enums;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FontManager.Models
{
    public class FontModel : INotifyPropertyChanged
    {
        public string FamilyName { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public List<string> Subsets { get; set; } = new();
        public FontSourceType SourceType { get; set; }

        public bool IsFavorite
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        }

        public InstallStatus Status
        {
            get;
            set
            {
                if (field != value)
                {
                    field = value;
                    OnPropertyChanged();
                }
            }
        } = InstallStatus.NotInstalled;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}