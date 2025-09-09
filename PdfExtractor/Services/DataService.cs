using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using PdfExtractor.Models;

namespace PdfExtractor.Services
{
    public static class DataService
    {
        private const string DataFileName = "lotes_data.json";

        public static string GetDataFilePath(string saveLocation)
        {
            return Path.Combine(saveLocation, DataFileName);
        }

        public static List<LoteData> LoadData(string saveLocation)
        {
            try
            {
                string filePath = GetDataFilePath(saveLocation);
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    var items = JsonSerializer.Deserialize<List<LoteDataJson>>(json) ?? new List<LoteDataJson>();
                    return items.Select(ConvertFromJson).ToList();
                }
            }
            catch { }
            
            return new List<LoteData>();
        }

        public static void SaveData(List<LoteData> data, string saveLocation)
        {
            try
            {
                Directory.CreateDirectory(saveLocation);
                string filePath = GetDataFilePath(saveLocation);
                
                var jsonData = data.Select(ConvertToJson).ToList();
                string json = JsonSerializer.Serialize(jsonData, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                
                File.WriteAllText(filePath, json);
            }
            catch { }
        }

        public static void AddLote(LoteData newLote, string saveLocation)
        {
            var existingData = LoadData(saveLocation);
            
            // Buscar si ya existe un lote con el mismo número
            var existingLote = existingData.FirstOrDefault(l => l.Lote == newLote.Lote);
            if (existingLote != null)
            {
                // Actualizar lote existente
                existingData.Remove(existingLote);
            }
            
            existingData.Add(newLote);
            SaveData(existingData, saveLocation);
        }

        private static LoteDataJson ConvertToJson(LoteData data)
        {
            return new LoteDataJson
            {
                DIA = data.Dia,
                FECHA = data.FechaString,
                LOTE = data.Lote,
                CAJA = data.Caja,
                USUARIO = data.Usuario, // Nueva propiedad
                DEPOSITADO = data.Depositado, // Nueva propiedad
                OBSERVACIONES = data.Observaciones, // Campo Observaciones
                OSM = FormatCurrency(data.OSM),
                MUNI = FormatCurrency(data.MUNI),
                Credito = FormatCurrency(data.Credito),
                Debito = FormatCurrency(data.Debito),
                Cheque = FormatCurrency(data.Cheque),
                Efectivo = FormatCurrency(data.Efectivo),
                CreditoDebito = FormatCurrency(data.Credito + data.Debito)
            };
        }

        private static LoteData ConvertFromJson(LoteDataJson json)
        {
            return new LoteData
            {
                Dia = json.DIA,
                Fecha = DateTime.TryParse(json.FECHA, out var fecha) ? fecha : DateTime.MinValue,
                Lote = json.LOTE,
                Caja = json.CAJA ?? "CAJA 1",
                Usuario = json.USUARIO ?? "", // Nueva propiedad
                Depositado = json.DEPOSITADO, // Nueva propiedad
                Observaciones = json.OBSERVACIONES ?? "", // Campo Observaciones
                OSM = ParseCurrency(json.OSM),
                MUNI = ParseCurrency(json.MUNI),
                Credito = ParseCurrency(json.Credito),
                Debito = ParseCurrency(json.Debito),
                Cheque = ParseCurrency(json.Cheque)
            };
        }

        private static string FormatCurrency(decimal value)
        {
            if (value == 0) return "";
            // Usar formato argentino: punto para miles, coma para decimales
            return value.ToString("C2", new CultureInfo("es-AR")).Replace("$", "$ ");
        }

        private static decimal ParseCurrency(string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            
            // Remover símbolo de moneda y espacios
            string cleanValue = value.Replace("$", "").Replace(" ", "").Trim();
            
            // Manejar formato argentino: 1.234.567,89
            if (cleanValue.Contains(".") && cleanValue.Contains(","))
            {
                // Remover puntos (separadores de miles) y reemplazar coma por punto
                cleanValue = cleanValue.Replace(".", "").Replace(",", ".");
            }
            else if (cleanValue.Contains(",") && !cleanValue.Contains("."))
            {
                // Solo coma decimal
                cleanValue = cleanValue.Replace(",", ".");
            }
            
            return decimal.TryParse(cleanValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
        }

        // Método para actualizar estado de depósito en lote
        public static void UpdateDepositado(List<string> lotes, bool depositado, string saveLocation)
        {
            var data = LoadData(saveLocation);
            foreach (var loteData in data.Where(l => lotes.Contains(l.Lote)))
            {
                loteData.Depositado = depositado;
            }
            SaveData(data, saveLocation);
        }
    }

    public class LoteDataJson
    {
        public string DIA { get; set; } = "";
        public string FECHA { get; set; } = "";
        public string LOTE { get; set; } = "";
        public string CAJA { get; set; } = "";
        public string USUARIO { get; set; } = ""; // Nueva propiedad
        public bool DEPOSITADO { get; set; } = false; // Nueva propiedad
        public string OBSERVACIONES { get; set; } = ""; // Campo Observaciones
        public string OSM { get; set; } = "";
        public string MUNI { get; set; } = "";
        public string Credito { get; set; } = "";
        public string Debito { get; set; } = "";
        public string Cheque { get; set; } = "";
        public string Efectivo { get; set; } = "";
        public string CreditoDebito { get; set; } = "";
    }
}
