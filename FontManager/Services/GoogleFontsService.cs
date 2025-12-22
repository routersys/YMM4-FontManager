using FontManager.Enums;
using FontManager.Models;
using FontManager.Services.Interfaces;
using FontManager.Settings;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;

namespace FontManager.Services
{
    public class GoogleFontsService : IFontService
    {
        private const string ApiUrlBase = "https://www.googleapis.com/webfonts/v1/webfonts?key=";
        private const string GitHubUrl = "https://raw.githubusercontent.com/routersys/YMM4-FontManager/main/font_list/google_fonts_data/all_fonts.json";
        private readonly HttpClient _httpClient = new();
        private readonly string _cachePath;
        private static readonly SemaphoreSlim _fileLock = new(1, 1);

        public GoogleFontsService()
        {
            var cacheDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Cache");
            _cachePath = Path.Combine(cacheDir, "fonts_cache.json");
        }

        public async Task<IEnumerable<FontModel>> GetGoogleFontsAsync(string apiKey)
        {
            GoogleFontsApiResponse? response = null;
            var settings = FontManagerSettings.Default;

            try
            {
                if (settings.UseApiDirectly && !string.IsNullOrWhiteSpace(apiKey))
                {
                    response = await _httpClient.GetFromJsonAsync<GoogleFontsApiResponse>($"{ApiUrlBase}{apiKey}&sort=popularity");
                }
                else
                {
                    response = await _httpClient.GetFromJsonAsync<GoogleFontsApiResponse>(GitHubUrl);
                }

                if (response != null)
                {
                    await _fileLock.WaitAsync();
                    try
                    {
                        var dir = Path.GetDirectoryName(_cachePath);
                        if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                        using var stream = new FileStream(_cachePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                        await JsonSerializer.SerializeAsync(stream, response);
                    }
                    finally
                    {
                        _fileLock.Release();
                    }
                }
            }
            catch
            {
                await _fileLock.WaitAsync();
                try
                {
                    if (File.Exists(_cachePath))
                    {
                        using var stream = new FileStream(_cachePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        response = await JsonSerializer.DeserializeAsync<GoogleFontsApiResponse>(stream);
                    }
                }
                finally
                {
                    _fileLock.Release();
                }
            }

            if (response?.Items is null) return Enumerable.Empty<FontModel>();

            return response.Items.Select(item => new FontModel
            {
                FamilyName = item.Family,
                Author = "Google Fonts",
                License = "OFL",
                Description = item.Category,
                DownloadUrl = item.Files.GetValueOrDefault("regular") ?? item.Files.Values.FirstOrDefault() ?? "",
                Tags = new List<string> { item.Category }.Concat(item.Subsets).ToList(),
                Subsets = item.Subsets,
                SourceType = FontSourceType.GoogleFonts
            });
        }
    }
}