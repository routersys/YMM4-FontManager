using FontManager.Models;

namespace FontManager.Services.Interfaces
{
    public interface IFontService
    {
        Task<IEnumerable<FontModel>> GetGoogleFontsAsync(string apiKey);
    }
}