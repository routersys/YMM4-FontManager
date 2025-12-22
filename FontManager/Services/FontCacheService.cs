using FontManager.Models;
using System.IO;
using System.Text.Json;

namespace FontManager.Services
{
    public class FontCacheService
    {
        private readonly string _cacheFilePath;

        public FontCacheService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontManager");
            Directory.CreateDirectory(folder);
            _cacheFilePath = Path.Combine(folder, "fonts_cache.json");
        }

        public async Task SaveCacheAsync(GoogleFontsApiResponse data)
        {
            try
            {
                using var stream = File.Create(_cacheFilePath);
                await JsonSerializer.SerializeAsync(stream, data);
            }
            catch { }
        }

        public async Task<GoogleFontsApiResponse?> LoadCacheAsync()
        {
            if (!File.Exists(_cacheFilePath)) return null;
            try
            {
                using var stream = File.OpenRead(_cacheFilePath);
                return await JsonSerializer.DeserializeAsync<GoogleFontsApiResponse>(stream);
            }
            catch
            {
                return null;
            }
        }
    }
}