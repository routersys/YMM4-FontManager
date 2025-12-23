using FontManager.Views;
using FontManager.ViewModels;
using System.Reflection;
using YukkuriMovieMaker.Plugin;

namespace FontManager
{
    [PluginDetails(
        AuthorName = "routersys",
        ContentId = ""
    )]
    public class FontManagerPlugin : IToolPlugin
    {
        public string Name => Translate.Plugin_Name;
        public PluginDetailsAttribute Details => GetType().GetCustomAttribute<PluginDetailsAttribute>() ?? new();
        public Type ViewModelType => typeof(FontManagerViewModel);
        public Type ViewType => typeof(FontManagerView);
        public FontManagerPlugin() => Settings.FontManagerSettings.Default.Initialize();
    }
}