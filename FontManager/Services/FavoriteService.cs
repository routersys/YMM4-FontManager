using System.IO;
using System.Text.Json;

namespace FontManager.Services
{
    public class FavoriteService
    {
        private readonly string _filePath;
        private HashSet<string> _favorites = new();

        public FavoriteService()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FontManager");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "favorites.json");
            Load();
        }

        public bool IsFavorite(string familyName) => _favorites.Contains(familyName);

        public void SetFavorite(string familyName, bool isFavorite)
        {
            if (isFavorite) _favorites.Add(familyName);
            else _favorites.Remove(familyName);
            Save();
        }

        private void Load()
        {
            if (!File.Exists(_filePath)) return;
            try
            {
                var json = File.ReadAllText(_filePath);
                var list = JsonSerializer.Deserialize<List<string>>(json);
                if (list != null) _favorites = new HashSet<string>(list);
            }
            catch { }
        }

        private void Save()
        {
            try
            {
                var json = JsonSerializer.Serialize(_favorites.ToList());
                File.WriteAllText(_filePath, json);
            }
            catch { }
        }
    }
}