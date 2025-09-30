using System;
using System.IO;
using System.Text.Json;

namespace PdfExtractor.Models
{
    public class WindowLayoutConfig
    {
        public double LeftPanelWidth { get; set; } = 650;
        public double WindowWidth { get; set; } = 1400;
        public double WindowHeight { get; set; } = 900;
        public bool IsWindowMaximized { get; set; } = true;

        private const string CONFIG_FILE = "window_layout.json";

        public static WindowLayoutConfig Load()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    string json = File.ReadAllText(CONFIG_FILE);
                    var config = JsonSerializer.Deserialize<WindowLayoutConfig>(json);
                    return config ?? new WindowLayoutConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading window layout config: {ex.Message}");
            }

            return new WindowLayoutConfig();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(CONFIG_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving window layout config: {ex.Message}");
            }
        }

        public bool IsValid()
        {
            return LeftPanelWidth >= 400 && LeftPanelWidth <= 2000 &&
                   WindowWidth >= 800 && WindowWidth <= 4000 &&
                   WindowHeight >= 600 && WindowHeight <= 3000;
        }
    }
}