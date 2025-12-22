using FontManager.Enums;
using FontManager.Models;
using FontManager.Services.Interfaces;
using System.Net.Http;
using System.Net.Http.Json;

namespace FontManager.Services
{
    public class GoogleFontsService : IFontService
    {
        private const string ApiUrlBase = "https://www.googleapis.com/webfonts/v1/webfonts?key=";
        private readonly HttpClient _httpClient = new();

        public async Task<IEnumerable<FontModel>> GetGoogleFontsAsync(string apiKey)
        {
            if (string.IsNullOrWhiteSpace(apiKey)) return Enumerable.Empty<FontModel>();

            try
            {
                var response = await _httpClient.GetFromJsonAsync<GoogleFontsApiResponse>($"{ApiUrlBase}{apiKey}&sort=popularity");
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
            catch
            {
                return Enumerable.Empty<FontModel>();
            }
        }
    }
}