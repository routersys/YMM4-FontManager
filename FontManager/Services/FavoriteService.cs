using System.IO;
using System.Reflection;
using System.Text.Json;

namespace FontManager.Services
{
    public class FavoriteService
    {
        private static readonly string DataDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "", "Data");
        private readonly string _filePath;
        private HashSet<string> _favorites = new();

        public FavoriteService()
        {
            Directory.CreateDirectory(DataDir);
            _filePath = Path.Combine(DataDir, "favorites.json");
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
                using var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
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
                using var stream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
            catch { }
        }
    }
}