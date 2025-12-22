using FontManager.Enums;
using FontManager.Models;
using FontManager.Services.Interfaces;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace FontManager.Services
{
    public class GoogleFontsService : IFontService
    {
        private const string ApiUrlBase = "https://www.googleapis.com/webfonts/v1/webfonts?key=";
        private readonly HttpClient _httpClient = new();
        private readonly string _cachePath;

        public GoogleFontsService()
        {
            _cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontManager", "fonts_cache.json");
        }

        public async Task<IEnumerable<FontModel>> GetGoogleFontsAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return Enumerable.Empty<FontModel>();
            GoogleFontsApiResponse? response = null;

            if (File.Exists(_cachePath))
            {
                try
                {
                    using var stream = File.OpenRead(_cachePath);
                    response = await JsonSerializer.DeserializeAsync<GoogleFontsApiResponse>(stream);
                }
                catch { }
            }

            if (response == null)
            {
                try
                {
                    response = await _httpClient.GetFromJsonAsync<GoogleFontsApiResponse>($"{ApiUrlBase}{apiKey}&sort=popularity");
                    if (response != null)
                    {
                        using var stream = File.Create(_cachePath);
                        await JsonSerializer.SerializeAsync(stream, response);
                    }
                }
                catch { }
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