using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace PdfExtractor.Services
{
    // --- CLASE PRINCIPAL ---
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

        public async Task<ExtractedPdfData> ExtractFromUrlAsync(string url)
        {
            var pdfData = await DownloadPdfAsync(url);
            var textContent = ExtractTextFromPdf(pdfData);
            var data = ExtractStructuredData(textContent);
            // Fallback posicional si faltan OSM/MUNI
            if ((data.TotalOSM == 0 || data.TotalMunicipalidad == 0))
            {
                data.DebugInfo = (data.DebugInfo ?? "") + "\n[INFO] Activando PdfPig fallback...";
                try
                {
                    // var (muni, osm, dbg) = PdfPigExtractor.ExtractAnchoredTotals(pdfData);
                    decimal muni = 0, osm = 0; string dbg = "Método deshabilitado temporalmente";
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    if (muni > 0 && osm > 0 && Math.Abs(muni + osm - (data.TotalOSM + data.TotalMunicipalidad)) < 1000)
                    {
                        data.TotalMunicipalidad = muni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = muni + osm;
                        data.DebugInfo += "\n[PdfPig] ÉXITO: Usados valores PdfPig";
                    }
                    else
                    {
                        data.DebugInfo += "\n[PdfPig] FALLO: Valores no válidos o no suman correctamente";
                    }
                }
                catch (Exception ex)
                {
                    data.DebugInfo += $"\n[PdfPig] ERROR: {ex.Message}";
                }
            }
            return data;
        }

        public ExtractedPdfData ExtractFromFile(string filePath)
        {
            var pdfData = File.ReadAllBytes(filePath);
            var textContent = ExtractTextFromPdf(pdfData);
            var data = ExtractStructuredData(textContent);
            if ((data.TotalOSM == 0 || data.TotalMunicipalidad == 0))
            {
                data.DebugInfo = (data.DebugInfo ?? "") + "\n[INFO] Activando PdfPig fallback...";
                try
                {
                    // var (muni, osm, dbg) = PdfPigExtractor.ExtractAnchoredTotals(pdfData);
                    decimal muni = 0, osm = 0; string dbg = "Método deshabilitado temporalmente";
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    if (muni > 0 && osm > 0 && Math.Abs(muni + osm - (data.TotalOSM + data.TotalMunicipalidad)) < 1000)
                    {
                        data.TotalMunicipalidad = muni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = muni + osm;
                        data.DebugInfo += "\n[PdfPig] ÉXITO: Usados valores PdfPig";
                    }
                    else
                    {
                        data.DebugInfo += "\n[PdfPig] FALLO: Valores no válidos o no suman correctamente";
                    }
                }
                catch (Exception ex)
                {
                    data.DebugInfo += $"\n[PdfPig] ERROR: {ex.Message}";
                }
            }
            return data;
        }

        public ExtractedPdfData ExtractFromBytes(byte[] pdfData)
        {
            var textContent = ExtractTextFromPdf(pdfData);
            var data = ExtractStructuredData(textContent);
            if ((data.TotalOSM == 0 || data.TotalMunicipalidad == 0))
            {
                data.DebugInfo = (data.DebugInfo ?? "") + "\n[INFO] Activando PdfPig fallback...";
                try
                {
                    // var (muni, osm, dbg) = PdfPigExtractor.ExtractAnchoredTotals(pdfData);
                    decimal muni = 0, osm = 0; string dbg = "Método deshabilitado temporalmente";
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    if (muni > 0 && osm > 0 && Math.Abs(muni + osm - (data.TotalOSM + data.TotalMunicipalidad)) < 1000)
                    {
                        data.TotalMunicipalidad = muni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = muni + osm;
                        data.DebugInfo += "\n[PdfPig] ÉXITO: Usados valores PdfPig";
                    }
                    else
                    {
                        data.DebugInfo += "\n[PdfPig] FALLO: Valores no válidos o no suman correctamente";
                    }
                }
                catch (Exception ex)
                {
                    data.DebugInfo += $"\n[PdfPig] ERROR: {ex.Message}";
                }
            }
            return data;
        }

        public ExtractedPdfData ExtractFromText(string pdfText)
        {
            return ExtractStructuredData(pdfText);
        }

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
                throw new PdfExtractionException("Timeout al descargar el PDF.", ex);
            }
        }

        public string ExtractTextFromPdf(byte[] pdfData)
        {
            try
            {
                using (var memoryStream = new MemoryStream(pdfData))
                using (var reader = new PdfReader(memoryStream))
                using (var pdfDoc = new PdfDocument(reader))
                {
                    var text = new StringBuilder();
                    for (int page = 1; page <= pdfDoc.GetNumberOfPages(); page++)
                    {
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(page));
                        text.AppendLine(pageText);
                    }
                    return text.ToString();
                }
            }
            catch (Exception ex)
            {
                throw new PdfExtractionException($"Error al extraer texto del PDF: {ex.Message}", ex);
            }
        }

        public ExtractedPdfData ExtractStructuredData(string pdfText)
        {
            if (string.IsNullOrEmpty(pdfText))
            {
                throw new ArgumentException("El contenido del PDF está vacío.");
            }

            var data = new ExtractedPdfData();
            data.RawText = pdfText; // Guardar texto para debug

            try
            {
                ExtractBasicInfo(pdfText, data);
                ExtractPaymentMethods(pdfText, data);
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
            var loteMatch = Regex.Match(pdfText, @"Lote\s+C\s+(\d+)");
            if (loteMatch.Success) data.Lote = loteMatch.Groups[1].Value;

            var fechaMatch = Regex.Match(pdfText, @"Fecha\s+(\d{2}-[A-Z]{3}-\d{2})");
            if (fechaMatch.Success) data.Fecha = ParseFecha(fechaMatch.Groups[1].Value);

            var usuarioMatch = Regex.Match(pdfText, @"Usuario\s+([A-Z\d]+)");
            if (usuarioMatch.Success) data.Usuario = usuarioMatch.Groups[1].Value;

            var cajeroMatch = Regex.Match(pdfText, @"Cajero\s+([A-Z\d]+)");
            if (cajeroMatch.Success) data.Cajero = cajeroMatch.Groups[1].Value;
        }

        private void ExtractPaymentMethods(string pdfText, ExtractedPdfData data)
        {
            var summarySectionMatch = Regex.Match(pdfText,
                @"Formas de Pago\s+Importe\s+Moneda(.*?)(\n\n|Por \d+-Municipalidad|$)",
                RegexOptions.Singleline);

            if (summarySectionMatch.Success)
            {
                string summaryText = summarySectionMatch.Groups[1].Value;
                data.Efectivo = ExtractPaymentValue(summaryText, "EFECTIVO");
                data.TarjetaCredito = ExtractPaymentValue(summaryText, "TARJETACR");
                data.TarjetaDebito = ExtractPaymentValue(summaryText, "TARJETADE");
                data.ChequeDiferido = ExtractPaymentValue(summaryText, "CHEQUEDIF");
            }
        }

        private decimal ExtractPaymentValue(string text, string paymentType)
        {
            var match = Regex.Match(text, $@"{paymentType}\s+([\d.,]+)");
            return match.Success ? ParseDecimal(match.Groups[1].Value) : 0;
        }

        private void ExtractTotals(string pdfText, ExtractedPdfData data)
        {
            var grandTotalMatch = Regex.Match(pdfText, @"Importe Lote\s*([\d.,]+)");
            if (!grandTotalMatch.Success)
            {
                data.DebugInfo = "ERROR: No se encontró el 'Importe Lote' en la cabecera.";
                return;
            }
            decimal grandTotal = ParseDecimal(grandTotalMatch.Groups[1].Value);

            var debugSb = new StringBuilder();
            debugSb.AppendLine($"Total Lote Esperado: {grandTotal:N2}");

            // LÓGICA PRINCIPAL: Verificar si existe "Por 774-Municipalidad V.Mercedes"
            bool hasMuniPattern = pdfText.Contains("Por 774-Municipalidad V.Mercedes");
            debugSb.AppendLine($"¿Contiene patrón MUNI 'Por 774-Municipalidad V.Mercedes'? {(hasMuniPattern ? "SÍ" : "NO")}");

            if (hasMuniPattern)
            {
                // CASO NORMAL: Hay MUNI - extraer OSM y MUNI por separado
                debugSb.AppendLine("MODO: Extracción normal (con MUNI)");
                
                // OSM: "Total Recibos en el lote de Obras Sanitarias.*Total Importe:\s*([\d.,]+)"
                var osmMatch = Regex.Match(pdfText, @"Total Recibos en el lote de Obras Sanitarias.*?Total Importe:\s*([\d.,]+)", RegexOptions.Singleline);
                decimal osm = osmMatch.Success ? ParseDecimal(osmMatch.Groups[1].Value) : 0;
                
                // MUNI: Buscar patrón específico del número aislado después de los recibos de municipalidad
                decimal muni = 0;
                var muniValueMatch = Regex.Match(pdfText, @"Por \d+-Municipalidad.*?(?:\n.*?){0,10}\n\d+\s+([\d.,]+)", RegexOptions.Singleline);
                if (muniValueMatch.Success)
                {
                    muni = ParseDecimal(muniValueMatch.Groups[1].Value);
                }
                else
                {
                    // Patrón alternativo: Total Importe después de section municipalidad
                    var muniTotalMatch = Regex.Match(pdfText, @"Por \d+-Municipalidad.*?Total Recibos Cobrados:\s*.*?Total Importe:\s*([\d.,]+)", RegexOptions.Singleline);
                    if (muniTotalMatch.Success)
                    {
                        muni = ParseDecimal(muniTotalMatch.Groups[1].Value);
                    }
                }

                debugSb.AppendLine($"OSM (patrón específico): {osm:N2}");
                debugSb.AppendLine($"MUNI (patrón específico): {muni:N2}");

                // Verificar si los valores son válidos y suman el total
                if (osm > 0 && muni > 0 && Math.Abs(osm + muni - grandTotal) < 0.1m)
                {
                    data.TotalOSM = osm;
                    data.TotalMunicipalidad = muni;
                    data.TotalGeneral = osm + muni;
                    debugSb.AppendLine("ÉXITO: Extracción normal completada");
                    data.DebugInfo = debugSb.ToString();
                    return;
                }
            }
            else
            {
                // CASO ESPECIAL: NO hay "Por 774-Municipalidad V.Mercedes"
                // → El último "Total Importe:" es OSM, no hay MUNI
                debugSb.AppendLine("MODO: Extracción especial (SIN patrón MUNI)");
                
                // Buscar TODAS las ocurrencias de "Total Importe:" 
                var totalImporteMatches = Regex.Matches(pdfText, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                debugSb.AppendLine($"Encontrados {totalImporteMatches.Count} 'Total Importe:'");
                
                if (totalImporteMatches.Count > 0)
                {
                    // Tomar el ÚLTIMO "Total Importe:" como OSM
                    var lastMatch = totalImporteMatches[totalImporteMatches.Count - 1];
                    decimal osm = ParseDecimal(lastMatch.Groups[1].Value);
                    
                    debugSb.AppendLine($"Último 'Total Importe:' = {osm:N2} (asignado a OSM)");
                    
                    if (Math.Abs(osm - grandTotal) < 0.1m)
                    {
                        data.TotalOSM = osm;
                        data.TotalMunicipalidad = 0; // No hay MUNI en este caso
                        data.TotalGeneral = osm;
                        debugSb.AppendLine("ÉXITO: Extracción especial completada");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else
                    {
                        debugSb.AppendLine($"ADVERTENCIA: OSM ({osm:N2}) no coincide con Total Lote ({grandTotal:N2})");
                    }
                }
                else
                {
                    debugSb.AppendLine("ERROR: No se encontró ningún 'Total Importe:' en el PDF");
                }
            }

            // Si llegamos aquí, algo falló
            debugSb.AppendLine("ERROR: No se pudo extraer OSM ni MUNI con ningún método");
            data.DebugInfo = debugSb.ToString();
        }    // Busca el número más cercano (antes o después) a la palabra clave
    private decimal FindClosestAmountAround(string text, string anchor)
    {
        int idx = text.IndexOf(anchor, StringComparison.OrdinalIgnoreCase);
        if (idx == -1) return 0;
        // Buscar montos en un rango antes y después del ancla (200 caracteres)
        int range = 200;
        int startBefore = Math.Max(0, idx - range);
        int endAfter = Math.Min(text.Length, idx + anchor.Length + range);
        string before = text.Substring(startBefore, idx - startBefore);
        string after = text.Substring(idx + anchor.Length, endAfter - (idx + anchor.Length));
        var regex = new Regex(@"([\d]{1,3}(?:[.,][\d]{3})*[.,]\d{2})");
        var matchesBefore = regex.Matches(before);
        var matchesAfter = regex.Matches(after);
        decimal valBefore = matchesBefore.Count > 0 ? ParseDecimal(matchesBefore[matchesBefore.Count - 1].Value) : 0;
        decimal valAfter = matchesAfter.Count > 0 ? ParseDecimal(matchesAfter[0].Value) : 0;
        // Debug: mostrar contexto
        // Puedes registrar en DebugInfo si lo necesitas
        if (valBefore > 0 && valAfter > 0)
            return Math.Abs(valBefore - valAfter) < 0.1m ? valBefore : (valBefore < valAfter ? valBefore : valAfter);
        if (valBefore > 0) return valBefore;
        if (valAfter > 0) return valAfter;
        return 0;
    }
        

        private DateTime ParseFecha(string fechaStr)
        {
            try
            {
                var parts = fechaStr.Split('-');
                if (parts.Length != 3) return DateTime.MinValue;
                int dia = int.Parse(parts[0]);
                int año = 2000 + int.Parse(parts[2]);
                string mesStr = parts[1].ToUpper();
                int mes = mesStr switch
                {
                    "ENE" => 1, "FEB" => 2, "MAR" => 3, "ABR" => 4,
                    "MAY" => 5, "JUN" => 6, "JUL" => 7, "AGO" => 8,
                    "SEP" => 9, "OCT" => 10, "NOV" => 11, "DIC" => 12,
                    _ => 0
                };
                return mes == 0 ? DateTime.MinValue : new DateTime(año, mes, dia);
            }
            catch { return DateTime.MinValue; }
        }

        private decimal ParseDecimal(string value)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value)) return 0;
                string cleanValue = value.Trim();
                var culture = cleanValue.Contains(',') ? new CultureInfo("es-AR") : CultureInfo.InvariantCulture;
                return decimal.Parse(cleanValue, NumberStyles.Any, culture);
            }
            catch { return 0; }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    // --- CLASE DE DATOS ---
    public class ExtractedPdfData
    {
        public string RawText { get; set; }
        public string Lote { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; }
        public string Cajero { get; set; }
        public decimal Efectivo { get; set; }
        public decimal TarjetaCredito { get; set; }
        public decimal TarjetaDebito { get; set; }
        public decimal ChequeDiferido { get; set; }
        public decimal TotalOSM { get; set; }
        public decimal TotalMunicipalidad { get; set; }
        public decimal TotalGeneral { get; set; }
        public string DebugInfo { get; set; }

        public decimal TotalTarjetas => TarjetaCredito + TarjetaDebito;
        public string FechaString => Fecha != DateTime.MinValue ? Fecha.ToString("dd/MM/yyyy") : "";
        public string DiaSemana => Fecha != DateTime.MinValue ? new CultureInfo("es-ES").DateTimeFormat.GetDayName(Fecha.DayOfWeek).ToUpper() : "";

        public ExtractedPdfData()
        {
            RawText = "";
            Lote = "";
            Usuario = "";
            Cajero = "";
            Fecha = DateTime.MinValue;
            DebugInfo = "Debug info no generada.";
        }
    }

    // --- CLASE DE EXCEPCIÓN ---
    public class PdfExtractionException : Exception
    {
        public PdfExtractionException(string message) : base(message) { }
        public PdfExtractionException(string message, Exception innerException) : base(message, innerException) { }
    }
}
