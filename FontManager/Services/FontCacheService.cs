using FontManager.Models;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace FontManager.Services
{
    public class FontCacheService
    {
        private static readonly string CacheDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Cache");
        private readonly string _cacheFilePath;
        private static readonly SemaphoreSlim _lock = new(1, 1);

        public FontCacheService()
        {
            Directory.CreateDirectory(CacheDir);
            _cacheFilePath = Path.Combine(CacheDir, "fonts_cache.json");
        }

        public async Task SaveCacheAsync(GoogleFontsApiResponse data)
        {
            await _lock.WaitAsync();
            try
            {
                using var stream = new FileStream(_cacheFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                await JsonSerializer.SerializeAsync(stream, data);
            }
            catch { }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<GoogleFontsApiResponse?> LoadCacheAsync()
        {
            if (!File.Exists(_cacheFilePath)) return null;
            await _lock.WaitAsync();
            try
            {
                using var stream = new FileStream(_cacheFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return await JsonSerializer.DeserializeAsync<GoogleFontsApiResponse>(stream);
            }
            catch
            {
                return null;
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}