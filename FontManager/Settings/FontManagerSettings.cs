using YukkuriMovieMaker.Plugin;

namespace FontManager.Settings
{
    public class FontManagerSettings : SettingsBase<FontManagerSettings>
    {
        public override string Name => "FontManager";
        public override SettingsCategory Category => SettingsCategory.None;
        public override bool HasSettingView => true;
        public override object? SettingView => new FontManager.Views.SettingsView();

        public string GoogleFontsApiKey
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
        } = string.Empty;

        public bool UseApiDirectly
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
        } = false;

        public override void Initialize() { }
    }
}