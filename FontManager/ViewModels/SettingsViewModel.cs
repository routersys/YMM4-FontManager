using FontManager.Settings;
using System.ComponentModel;

namespace FontManager.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public FontManagerSettings Settings => FontManagerSettings.Default;

        #pragma warning disable 67
        public event PropertyChangedEventHandler? PropertyChanged;
        #pragma warning restore 67
    }
}