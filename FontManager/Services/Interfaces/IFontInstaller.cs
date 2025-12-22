namespace FontManager.Services.Interfaces
{
    public interface IFontInstaller
    {
        Task<bool> InstallFontAsync(string filePath);
        Task<bool> UninstallFontAsync(string familyName);
        bool IsFontInstalled(string familyName);
        IEnumerable<string> GetInstalledFontNames();
    }
}