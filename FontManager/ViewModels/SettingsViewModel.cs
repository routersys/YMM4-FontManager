using FontManager.Settings;
using System.ComponentModel;

namespace FontManager.ViewModels
{
    public class SettingsViewModel : INotifyPropertyChanged
    {
        public FontManagerSettings Settings => FontManagerSettings.Default;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}