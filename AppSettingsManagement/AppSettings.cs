using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace AppSettingsManagement
{
    public record AppSettings
    {
        
        public AppSettings()
        {
        }

        public string Token { get; init; }
        public IEnumerable<string> Groups { get; init; }
    }

    public class AppSettingsManager
    {
        public AppSettingsManager(string appName)
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string dirPath = Path.Combine(appDataPath, appName);
            Directory.CreateDirectory(dirPath);
            _appSettingsPath = Path.Combine(dirPath, "settings.json");
            if (File.Exists(_appSettingsPath))
                return;

            Reset();
        }

        private readonly string _appSettingsPath;

        public AppSettings Load()
        {
            string json = File.ReadAllText(_appSettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json);
        }

        public void Save(AppSettings newAppSettings)
        {
            string json = JsonSerializer.Serialize(newAppSettings);
            File.WriteAllText(_appSettingsPath, json);
        }

        public void Reset() => Save(new AppSettings { Token = string.Empty, Groups = Enumerable.Empty<string>() });
    }
}