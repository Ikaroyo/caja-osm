using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace PdfExtractor.Services
{
    /// <summary>
    /// Servicio independiente para extraer datos de PDFs de control de lotes
    /// </summary>
    public class PdfDataExtractor : IDisposable
    {
        private readonly HttpClient _httpClient;

        public PdfDataExtractor()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMinutes(5);
        }

        public PdfDataExtractor(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Extrae datos de un PDF desde una URL
        /// </summary>
        public async Task<ExtractedPdfData> ExtractFromUrlAsync(string url)
        {
            var pdfData = await DownloadPdfAsync(url);
            var textContent = ExtractTextFromPdf(pdfData);
            return ExtractStructuredData(textContent);
        }

        /// <summary>
        /// Extrae datos de un archivo PDF local
        /// </summary>
        public ExtractedPdfData ExtractFromFile(string filePath)
        {
            var pdfData = File.ReadAllBytes(filePath);
            var textContent = ExtractTextFromPdf(pdfData);
            return ExtractStructuredData(textContent);
        }

        /// <summary>
        /// Extrae datos de un array de bytes de PDF
        /// </summary>
        public ExtractedPdfData ExtractFromBytes(byte[] pdfData)
        {
            var textContent = ExtractTextFromPdf(pdfData);
            return ExtractStructuredData(textContent);
        }

        /// <summary>
        /// Extrae datos de texto ya extraído del PDF
        /// </summary>
        public ExtractedPdfData ExtractFromText(string pdfText)
        {
            return ExtractStructuredData(pdfText);
        }

        /// <summary>
        /// Descarga un PDF desde una URL
        /// </summary>
        public async Task<byte[]> DownloadPdfAsync(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new PdfExtractionException($"Error al descargar el PDF: {ex.Message}", ex);
            }
            catch (TaskCanceledException ex)
            {
                throw new PdfExtractionException("Timeout al descargar el PDF. Verifique la conexión y la URL.", ex);
            }
        }

        /// <summary>
        /// Extrae texto plano de un PDF
        /// </summary>
        public string ExtractTextFromPdf(byte[] pdfData)
        {
            try
            {
                using var memoryStream = new MemoryStream(pdfData);
                using var reader = new PdfReader(memoryStream);
                using var pdfDoc = new PdfDocument(reader);
                
                var text = new StringBuilder();
                
                for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                {
                    text.AppendLine($"=== PÁGINA {page} ===");
                    string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                    text.AppendLine(pageText);
                    text.AppendLine();
                }
                
                return text.ToString();
            }
            catch (Exception ex)
            {
                throw new PdfExtractionException($"Error al extraer texto del PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extrae datos estructurados del texto del PDF
        /// </summary>
        public ExtractedPdfData ExtractStructuredData(string pdfText)
        {
            if (string.IsNullOrEmpty(pdfText))
            {
                throw new ArgumentException("El contenido del PDF está vacío.");
            }

            var data = new ExtractedPdfData
            {
                RawText = pdfText
            };

            try
            {
                // Extraer información básica
                ExtractBasicInfo(pdfText, data);
                
                // Extraer formas de pago
                ExtractPaymentMethods(pdfText, data);
                
                // Extraer totales
                ExtractTotals(pdfText, data);
                
                return data;
            }
            catch (Exception ex)
            {
                throw new PdfExtractionException($"Error al analizar el PDF: {ex.Message}", ex);
            }
        }

        private void ExtractBasicInfo(string pdfText, ExtractedPdfData data)
        {
            // Extraer Lote
            var loteMatch = Regex.Match(pdfText, @"Lote\s+C\s+(\d+)", RegexOptions.IgnoreCase);
            if (loteMatch.Success)
            {
                data.Lote = loteMatch.Groups[1].Value;
            }

            // Extraer Fecha
            var fechaMatch = Regex.Match(pdfText, @"Fecha\s+(\d{2}-[A-Z]{3}-\d{2})", RegexOptions.IgnoreCase);
            if (fechaMatch.Success)
            {
                data.Fecha = ParseFecha(fechaMatch.Groups[1].Value);
            }

            // Extraer Usuario
            var usuarioMatch = Regex.Match(pdfText, @"Usuario\s+([A-Z]+)", RegexOptions.IgnoreCase);
            if (usuarioMatch.Success)
            {
                data.Usuario = usuarioMatch.Groups[1].Value;
            }

            // Extraer Cajero
            var cajeroMatch = Regex.Match(pdfText, @"Cajero\s+([A-Z]+)", RegexOptions.IgnoreCase);
            if (cajeroMatch.Success)
            {
                data.Cajero = cajeroMatch.Groups[1].Value;
            }
        }

        private void ExtractPaymentMethods(string pdfText, ExtractedPdfData data)
        {
            // Buscar sección de "Formas de Pago"
            var paymentSection = Regex.Match(pdfText, 
                @"Formas de Pago\s+Importe\s+Moneda(.*?)(?=Total Recibos|$)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (paymentSection.Success)
            {
                string paymentText = paymentSection.Groups[1].Value;

                // Extraer EFECTIVO
                var efectivoMatch = Regex.Match(paymentText, @"EFECTIVO\s+([\d,\.]+)\s+PES", RegexOptions.IgnoreCase);
                if (efectivoMatch.Success)
                {
                    data.Efectivo = ParseDecimal(efectivoMatch.Groups[1].Value);
                }

                // Extraer TARJETACR (Crédito)
                var creditoMatch = Regex.Match(paymentText, @"TARJETACR\s+([\d,\.]+)\s+PES", RegexOptions.IgnoreCase);
                if (creditoMatch.Success)
                {
                    data.TarjetaCredito = ParseDecimal(creditoMatch.Groups[1].Value);
                }

                // Extraer TARJETADE (Débito)
                var debitoMatch = Regex.Match(paymentText, @"TARJETADE\s+([\d,\.]+)\s+PES", RegexOptions.IgnoreCase);
                if (debitoMatch.Success)
                {
                    data.TarjetaDebito = ParseDecimal(debitoMatch.Groups[1].Value);
                }

                // Extraer CHEQUE
                var chequeMatch = Regex.Match(paymentText, @"CHEQUE\s+([\d,\.]+)\s+PES", RegexOptions.IgnoreCase);
                if (chequeMatch.Success)
                {
                    data.Cheque = ParseDecimal(chequeMatch.Groups[1].Value);
                }
            }
        }

        private void ExtractTotals(string pdfText, ExtractedPdfData data)
        {
            // Extraer Total OSM
            var osmMatch = Regex.Match(pdfText, 
                @"Total Recibos en el lote de Obras Sanitarias.*?Total Importe:\s*([\d,\.]+)", 
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (osmMatch.Success)
            {
                data.TotalOSM = ParseDecimal(osmMatch.Groups[1].Value);
            }

            // Extraer Total MUNI - buscar diferentes patrones
            var muniPatterns = new[]
            {
                @"Total Recibos Cobrados:\s*\d+\s*Total Importe:\s*([\d,\.]+)",
                @"(\d+)\s+([\d,\.]+)\s+Total Recibos Cobrados:",
                @"Por.*?Municipalidad.*?Total Importe:\s*([\d,\.]+)"
            };

            foreach (var pattern in muniPatterns)
            {
                var muniMatch = Regex.Match(pdfText, pattern, RegexOptions.IgnoreCase);
                if (muniMatch.Success)
                {
                    var groupIndex = muniMatch.Groups.Count > 2 ? 2 : 1;
                    data.TotalMunicipalidad = ParseDecimal(muniMatch.Groups[groupIndex].Value);
                    break;
                }
            }

            // Calcular total general
            data.TotalGeneral = data.TotalOSM + data.TotalMunicipalidad;
        }

        private DateTime ParseFecha(string fechaStr)
        {
            try
            {
                // Formato: 05-SEP-25
                var parts = fechaStr.Split('-');
                if (parts.Length != 3) return DateTime.MinValue;

                int dia = int.Parse(parts[0]);
                int año = 2000 + int.Parse(parts[2]); // Asumiendo años 20xx
                
                string mesStr = parts[1].ToUpper();
                int mes = mesStr switch
                {
                    "ENE" => 1, "FEB" => 2, "MAR" => 3, "ABR" => 4,
                    "MAY" => 5, "JUN" => 6, "JUL" => 7, "AGO" => 8,
                    "SEP" => 9, "OCT" => 10, "NOV" => 11, "DIC" => 12,
                    _ => 1
                };

                return new DateTime(año, mes, dia);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        private decimal ParseDecimal(string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return 0;

                // Limpiar el valor
                string cleanValue = value.Trim().Replace(" ", "");
                
                // Si contiene coma como separador decimal
                if (cleanValue.Contains(","))
                {
                    // Reemplazar puntos (separadores de miles) por nada
                    cleanValue = cleanValue.Replace(".", "");
                    // Reemplazar coma por punto decimal
                    cleanValue = cleanValue.Replace(",", ".");
                }

                return decimal.Parse(cleanValue, CultureInfo.InvariantCulture);
            }
            catch
            {
                return 0;
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    /// <summary>
    /// Datos extraídos del PDF
    /// </summary>
    public class ExtractedPdfData
    {
        public string RawText { get; set; } = "";
        public string Lote { get; set; } = "";
        public DateTime Fecha { get; set; } = DateTime.MinValue;
        public string Usuario { get; set; } = "";
        public string Cajero { get; set; } = "";
        
        // Formas de pago
        public decimal Efectivo { get; set; } = 0;
        public decimal TarjetaCredito { get; set; } = 0;
        public decimal TarjetaDebito { get; set; } = 0;
        public decimal Cheque { get; set; } = 0;
        
        // Totales
        public decimal TotalOSM { get; set; } = 0;
        public decimal TotalMunicipalidad { get; set; } = 0;
        public decimal TotalGeneral { get; set; } = 0;
        
        // Propiedades calculadas
        public decimal TotalTarjetas => TarjetaCredito + TarjetaDebito;
        public string FechaString => Fecha != DateTime.MinValue ? Fecha.ToString("dd/MM/yyyy") : "";
        public string DiaSemana => Fecha != DateTime.MinValue ? 
            new CultureInfo("es-ES").DateTimeFormat.GetDayName(Fecha.DayOfWeek).ToUpper() : "";
    }

    /// <summary>
    /// Excepción personalizada para errores de extracción de PDF
    /// </summary>
    public class PdfExtractionException : Exception
    {
        public PdfExtractionException(string message) : base(message) { }
        public PdfExtractionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
