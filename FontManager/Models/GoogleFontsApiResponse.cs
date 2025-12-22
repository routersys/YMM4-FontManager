using System.Text.Json.Serialization;

namespace FontManager.Models
{
    public class GoogleFontsApiResponse
    {
        [JsonPropertyName("items")]
        public List<GoogleFontItemDto> Items { get; set; } = new();
    }

    public class GoogleFontItemDto
    {
        [JsonPropertyName("family")]
        public string Family { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("subsets")]
        public List<string> Subsets { get; set; } = new();

        [JsonPropertyName("files")]
        public Dictionary<string, string> Files { get; set; } = new();

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;
    }
}