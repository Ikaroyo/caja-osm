using System;
using System.IO;
using System.Text.Json;

namespace PdfExtractor.Models
{
    public class AppConfig
    {
        public string SaveLocation { get; set; } = "";
        public string CajaSeleccionada { get; set; } = "CAJA 1";
        public string WebPageUrl { get; set; } = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo"; // Nueva propiedad
        
        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "PdfExtractor", "config.json");

        public static AppConfig Load()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Loading config from: {ConfigFilePath}");
                
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    System.Diagnostics.Debug.WriteLine($"Config file content: {json}");
                    
                    var result = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
                    System.Diagnostics.Debug.WriteLine($"Config loaded successfully, SaveLocation: '{result.SaveLocation}'");
                    return result;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Config file does not exist, returning new AppConfig");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading config: {ex}");
            }
            
            return new AppConfig();
        }

        public void Save()
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigFilePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
            }
        }
    }
}
