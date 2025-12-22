namespace FontManager.Services.Interfaces
{
    public interface IFontInstaller
    {
        Task<bool> InstallFontAsync(string filePath);
        bool IsFontInstalled(string familyName);
        IEnumerable<string> GetInstalledFontNames();
    }
}