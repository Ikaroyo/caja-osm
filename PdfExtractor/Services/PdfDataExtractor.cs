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
                    var (muni, osm, dbg) = ExtractAnchoredTotals(pdfData);
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    data.DebugInfo += $"\n[PdfPig Debug] {dbg}";
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
                    var (muni, osm, dbg) = ExtractAnchoredTotals(pdfData);
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    data.DebugInfo += $"\n[PdfPig Debug] {dbg}";
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
                    var (muni, osm, dbg) = ExtractAnchoredTotals(pdfData);
                    data.DebugInfo += $"\n[PdfPig] OSM: {osm:N2}, MUNI: {muni:N2}";
                    data.DebugInfo += $"\n[PdfPig Debug] {dbg}";
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
            debugSb.AppendLine($"Longitud texto PDF: {pdfText.Length} caracteres");
            
            // Debug: Mostrar si existe el patrón de municipalidad
            var municipalidadIndex = pdfText.IndexOf("Por 774-Municipalidad V.Mercedes", StringComparison.OrdinalIgnoreCase);
            debugSb.AppendLine($"Índice de 'Por 774-Municipalidad V.Mercedes': {municipalidadIndex}");
            
            // Debug: Mostrar el texto alrededor del patrón de municipalidad
            if (municipalidadIndex >= 0)
            {
                int contextStart = Math.Max(0, municipalidadIndex - 100);
                int contextEnd = Math.Min(pdfText.Length, municipalidadIndex + 300);
                string context = pdfText.Substring(contextStart, contextEnd - contextStart);
                debugSb.AppendLine($"CONTEXTO MUNI (±200 chars):");
                debugSb.AppendLine($"'{context}'");
                debugSb.AppendLine("--- FIN CONTEXTO ---");
            }
            
            // LÓGICA PRINCIPAL: Verificar si existe "Por 774-Municipalidad V.Mercedes"
            bool hasMuniPattern = municipalidadIndex >= 0;
            debugSb.AppendLine($"¿Contiene patrón MUNI 'Por 774-Municipalidad V.Mercedes'? {(hasMuniPattern ? "SÍ" : "NO")}");

            if (hasMuniPattern)
            {
                // CASO NORMAL: Hay MUNI - extraer OSM y MUNI por separado
                debugSb.AppendLine("MODO: Extracción normal (con MUNI)");
                
                // EXTRAER RECIBOS COBRADOS DE MUNI con patrones mejorados
                var recibosMuniMatch = Regex.Match(pdfText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?Total Recibos Cobrados:\s*(\d+)", RegexOptions.IgnoreCase);
                if (recibosMuniMatch.Success)
                {
                    if (int.TryParse(recibosMuniMatch.Groups[1].Value, out int recibos))
                    {
                        data.TotalRecibosMuni = recibos;
                        debugSb.AppendLine($"Total Recibos MUNI: {recibos}");
                    }
                }
                else
                {
                    // Patrón alternativo sin la V.Mercedes
                    var recibosAltMatch = Regex.Match(pdfText, @"Por \d+-Municipalidad[\s\S]*?Total Recibos Cobrados:\s*(\d+)", RegexOptions.IgnoreCase);
                    if (recibosAltMatch.Success)
                    {
                        if (int.TryParse(recibosAltMatch.Groups[1].Value, out int recibos))
                        {
                            data.TotalRecibosMuni = recibos;
                            debugSb.AppendLine($"Total Recibos MUNI (patrón alternativo): {recibos}");
                        }
                    }
                    else
                    {
                        // Patrón para formato roto: buscar número antes de "Total Recibos Cobrados:"
                        var recibosBrokenMatch = Regex.Match(pdfText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?(\d+)\s+[\d.,]+\s*Total Recibos Cobrados:", RegexOptions.IgnoreCase);
                        if (recibosBrokenMatch.Success)
                        {
                            if (int.TryParse(recibosBrokenMatch.Groups[1].Value, out int recibos))
                            {
                                data.TotalRecibosMuni = recibos;
                                debugSb.AppendLine($"Total Recibos MUNI (formato roto): {recibos}");
                            }
                        }
                        else
                        {
                            debugSb.AppendLine("No se encontraron recibos MUNI");
                        }
                    }
                }
                
                // MUNI: Buscar Total Importe después de "Por 774-Municipalidad" con patrones mejorados
                decimal muni = 0;
                
                // Debug: mostrar todos los "Total Importe:" encontrados
                var allTotalImporte = Regex.Matches(pdfText, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                debugSb.AppendLine($"DEBUG: Encontrados {allTotalImporte.Count} 'Total Importe:' en todo el PDF:");
                for (int i = 0; i < allTotalImporte.Count; i++)
                {
                    var match = allTotalImporte[i];
                    var value = ParseDecimal(match.Groups[1].Value);
                    var index = match.Index;
                    debugSb.AppendLine($"  [{i+1}] Valor: {value:N2} en posición {index}");
                }
                
                // Patrón 1: Formato exacto del usuario "Total Recibos Cobrados: X Total Importe: Y"
                var muniPattern1 = Regex.Match(pdfText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?Total Recibos Cobrados:\s*\d+\s*Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                debugSb.AppendLine($"DEBUG Patrón 1: {(muniPattern1.Success ? "ÉXITO" : "FALLO")}");
                if (muniPattern1.Success)
                {
                    muni = ParseDecimal(muniPattern1.Groups[1].Value);
                    debugSb.AppendLine($"MUNI (Patrón 1 - V.Mercedes + Recibos + Importe): {muni:N2}");
                }
                else
                {
                    // Patrón 2: Sin V.Mercedes pero con recibos
                    var muniPattern2 = Regex.Match(pdfText, @"Por \d+-Municipalidad[\s\S]*?Total Recibos Cobrados:\s*\d+\s*Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                    debugSb.AppendLine($"DEBUG Patrón 2: {(muniPattern2.Success ? "ÉXITO" : "FALLO")}");
                    if (muniPattern2.Success)
                    {
                        muni = ParseDecimal(muniPattern2.Groups[1].Value);
                        debugSb.AppendLine($"MUNI (Patrón 2 - Sin V.Mercedes + Recibos + Importe): {muni:N2}");
                    }
                    else
                    {
                        // Patrón 3: Buscar el primer "Total Importe:" que aparece ANTES de "Obras Sanitarias"
                        var osmIndex = pdfText.IndexOf("Total Recibos en el lote de Obras Sanitarias", StringComparison.OrdinalIgnoreCase);
                        debugSb.AppendLine($"DEBUG: Índice OSM: {osmIndex}");
                        if (osmIndex > 0)
                        {
                            // Buscar "Total Importe:" solo en la parte antes de OSM
                            string textBeforeOSM = pdfText.Substring(0, osmIndex);
                            var muniPattern3 = Regex.Match(textBeforeOSM, @"Por \d+-Municipalidad[\s\S]*?Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                            debugSb.AppendLine($"DEBUG Patrón 3: {(muniPattern3.Success ? "ÉXITO" : "FALLO")}");
                            if (muniPattern3.Success)
                            {
                                muni = ParseDecimal(muniPattern3.Groups[1].Value);
                                debugSb.AppendLine($"MUNI (Patrón 3 - Total Importe antes de OSM): {muni:N2}");
                            }
                        }
                        
                        if (muni == 0)
                        {
                            // Patrón 4: Fallback - cualquier Total Importe después de municipalidad
                            var muniPattern4 = Regex.Match(pdfText, @"Por \d+-Municipalidad[\s\S]*?Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                            debugSb.AppendLine($"DEBUG Patrón 4: {(muniPattern4.Success ? "ÉXITO" : "FALLO")}");
                            if (muniPattern4.Success)
                            {
                                muni = ParseDecimal(muniPattern4.Groups[1].Value);
                                debugSb.AppendLine($"MUNI (Patrón 4 - Fallback): {muni:N2}");
                            }
                            else
                            {
                                debugSb.AppendLine("MUNI no encontrado con ningún patrón");
                                
                                // Patrón especial para formato roto: el valor está ANTES de "Total Recibos Cobrados:"
                                var muniBrokenMatch = Regex.Match(pdfText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?(\d+)\s+([\d.,]+)\s*Total Recibos Cobrados:", RegexOptions.IgnoreCase);
                                if (muniBrokenMatch.Success)
                                {
                                    muni = ParseDecimal(muniBrokenMatch.Groups[2].Value);
                                    debugSb.AppendLine($"MUNI (Patrón formato roto - valor antes de Total Recibos): {muni:N2}");
                                }
                                else
                                {
                                    // Patrón específico para el caso "2     33016,10" + "Total Recibos Cobrados:"
                                    var muniSpecificMatch = Regex.Match(pdfText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?(\d+)\s+([\d.,]+)[\s\r\n]+Total Recibos Cobrados:", RegexOptions.IgnoreCase);
                                    if (muniSpecificMatch.Success)
                                    {
                                        muni = ParseDecimal(muniSpecificMatch.Groups[2].Value);
                                        debugSb.AppendLine($"MUNI (Patrón específico formato roto): {muni:N2}");
                                    }
                                    else
                                    {
                                        // DEBUG: Si hay diferencia significativa entre total esperado y encontrado, esa debe ser MUNI
                                        if (allTotalImporte.Count >= 1)
                                        {
                                            var largestImporte = allTotalImporte.Cast<Match>()
                                                .Select(m => ParseDecimal(m.Groups[1].Value))
                                                .OrderByDescending(v => v)
                                                .First();
                                            
                                            decimal posibleMuni = grandTotal - largestImporte;
                                            if (posibleMuni > 0 && posibleMuni < grandTotal * 0.5m) // MUNI debe ser menor que OSM generalmente
                                            {
                                                muni = posibleMuni;
                                                debugSb.AppendLine($"MUNI (Calculado por diferencia): {muni:N2} = {grandTotal:N2} - {largestImporte:N2}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                
                // OSM: Buscar específicamente después de "Obras Sanitarias"
                decimal osm = 0;
                
                // Patrón 1: OSM después de "Total Recibos en el lote de Obras Sanitarias"
                var osmMatch = Regex.Match(pdfText, @"Total Recibos en el lote de Obras Sanitarias.*?Total Importe:\s*([\d.,]+)", RegexOptions.Singleline);
                if (osmMatch.Success)
                {
                    osm = ParseDecimal(osmMatch.Groups[1].Value);
                    debugSb.AppendLine($"OSM (después de Obras Sanitarias): {osm:N2}");
                }
                else
                {
                    // Patrón 2: Buscar el ÚLTIMO "Total Importe:" que sea diferente del valor de MUNI
                    var totalImporteMatches = Regex.Matches(pdfText, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                    debugSb.AppendLine($"Encontrados {totalImporteMatches.Count} 'Total Importe:'");
                    
                    if (totalImporteMatches.Count >= 2)
                    {
                        // Buscar el que sea diferente del valor de MUNI
                        for (int i = totalImporteMatches.Count - 1; i >= 0; i--)
                        {
                            decimal candidateOsm = ParseDecimal(totalImporteMatches[i].Groups[1].Value);
                            if (Math.Abs(candidateOsm - muni) > 0.1m) // Debe ser diferente de MUNI
                            {
                                osm = candidateOsm;
                                debugSb.AppendLine($"OSM (Total Importe #{i+1}, diferente de MUNI): {osm:N2}");
                                break;
                            }
                        }
                    }
                    else if (totalImporteMatches.Count == 1)
                    {
                        // Solo hay un "Total Importe:" - verificar si es diferente del MUNI
                        decimal singleImporte = ParseDecimal(totalImporteMatches[0].Groups[1].Value);
                        if (muni == 0) // Si no encontramos MUNI, este podría ser OSM
                        {
                            osm = singleImporte;
                            debugSb.AppendLine($"OSM (único Total Importe, MUNI no encontrado): {osm:N2}");
                        }
                        else if (Math.Abs(singleImporte - muni) > 0.1m)
                        {
                            osm = singleImporte;
                            debugSb.AppendLine($"OSM (único Total Importe, diferente de MUNI): {osm:N2}");
                        }
                        else
                        {
                            debugSb.AppendLine($"ADVERTENCIA: Único Total Importe ({singleImporte:N2}) es igual a MUNI, no asignado a OSM");
                        }
                    }
                }

                debugSb.AppendLine($"Resultados MUNI: {muni:N2}, OSM: {osm:N2}");

                // Verificar si los valores son válidos y diferentes
                if (muni > 0 && osm > 0 && Math.Abs(muni - osm) > 0.1m)
                {
                    // Verificar que la suma sea razonable respecto al total
                    decimal suma = muni + osm;
                    if (Math.Abs(suma - grandTotal) < Math.Max(grandTotal * 0.05m, 1000m)) // 5% tolerancia o 1000 pesos
                    {
                        data.TotalMunicipalidad = muni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = suma;
                        debugSb.AppendLine($"ÉXITO: Extracción normal completada (MUNI≠OSM, suma={suma:N2})");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else
                    {
                        debugSb.AppendLine($"ADVERTENCIA: Suma MUNI+OSM ({suma:N2}) muy diferente del Total Lote ({grandTotal:N2})");
                    }
                }
                else if (muni > 0 && osm == 0)
                {
                    // Solo tenemos MUNI, calcular OSM como diferencia
                    osm = grandTotal - muni;
                    if (osm > 0)
                    {
                        data.TotalMunicipalidad = muni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = grandTotal;
                        debugSb.AppendLine($"ÉXITO: MUNI extraído, OSM calculado como diferencia: {osm:N2}");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                }
                else if (muni == 0 && osm > 0)
                {
                    // Solo tenemos OSM, calcular MUNI como diferencia si es razonable
                    decimal posibleMuni = grandTotal - osm;
                    if (posibleMuni > 0 && posibleMuni < grandTotal * 0.5m) // MUNI debe ser menor que OSM generalmente
                    {
                        data.TotalMunicipalidad = posibleMuni;
                        data.TotalOSM = osm;
                        data.TotalGeneral = grandTotal;
                        debugSb.AppendLine($"ÉXITO: OSM extraído, MUNI calculado como diferencia: {posibleMuni:N2}");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else if (Math.Abs(osm - grandTotal) < Math.Max(grandTotal * 0.05m, 1000m))
                    {
                        data.TotalMunicipalidad = 0;
                        data.TotalOSM = osm;
                        data.TotalGeneral = osm;
                        data.TotalRecibosMuni = 0;
                        debugSb.AppendLine($"ÉXITO: Solo OSM encontrado, probablemente no hay MUNI: {osm:N2}");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                }
                else if (Math.Abs(muni - osm) <= 0.1m && muni > 0)
                {
                    debugSb.AppendLine($"ERROR: MUNI y OSM tienen el mismo valor ({muni:N2}), esto es incorrecto");
                    
                    // En lugar de reasignar, calcular MUNI por diferencia
                    decimal posibleMuni = grandTotal - muni;
                    if (posibleMuni > 0 && posibleMuni < grandTotal * 0.5m)
                    {
                        data.TotalMunicipalidad = posibleMuni;
                        data.TotalOSM = muni;
                        data.TotalGeneral = grandTotal;
                        debugSb.AppendLine($"CORRECCIÓN: MUNI calculado por diferencia: {posibleMuni:N2}, OSM: {muni:N2}");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else
                    {
                        // Intentar reasignar: el valor único va a OSM, MUNI = 0
                        data.TotalMunicipalidad = 0;
                        data.TotalOSM = muni; // Usar el valor encontrado para OSM
                        data.TotalGeneral = muni;
                        data.TotalRecibosMuni = 0;
                        debugSb.AppendLine($"CORRECCIÓN: Asignando valor único a OSM, MUNI=0");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                }
            }
            else
            {
                // CASO ESPECIAL: NO hay "Por 774-Municipalidad V.Mercedes"
                // → Solo buscar OSM al final como sugiere el usuario
                debugSb.AppendLine("MODO: Extracción especial (SIN patrón MUNI) - buscar solo OSM");
                
                // Buscar solo OSM después de "Obras Sanitarias"
                var osmMatch = Regex.Match(pdfText, @"Total Recibos en el lote de Obras Sanitarias[\s\S]*?Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                if (osmMatch.Success)
                {
                    decimal osm = ParseDecimal(osmMatch.Groups[1].Value);
                    debugSb.AppendLine($"OSM (después de Obras Sanitarias): {osm:N2}");
                    
                    // Verificar si coincide con el total del lote
                    if (Math.Abs(osm - grandTotal) < Math.Max(grandTotal * 0.05m, 1000m))
                    {
                        data.TotalOSM = osm;
                        data.TotalMunicipalidad = 0; // No hay MUNI en este caso
                        data.TotalGeneral = osm;
                        data.TotalRecibosMuni = 0; // No hay recibos MUNI
                        debugSb.AppendLine("ÉXITO: Extracción especial completada (solo OSM)");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else
                    {
                        debugSb.AppendLine($"ADVERTENCIA: OSM ({osm:N2}) no coincide con Total Lote ({grandTotal:N2}), pero asignando de todas formas");
                        data.TotalOSM = osm;
                        data.TotalMunicipalidad = 0;
                        data.TotalGeneral = osm;
                        data.TotalRecibosMuni = 0;
                        debugSb.AppendLine("ÉXITO: Extracción especial completada con tolerancia");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                }
                else
                {
                    // Fallback: usar el último "Total Importe:" encontrado
                    var totalImporteMatches = Regex.Matches(pdfText, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
                    debugSb.AppendLine($"Fallback - Encontrados {totalImporteMatches.Count} 'Total Importe:'");
                    
                    if (totalImporteMatches.Count > 0)
                    {
                        var lastMatch = totalImporteMatches[totalImporteMatches.Count - 1];
                        decimal osm = ParseDecimal(lastMatch.Groups[1].Value);
                        
                        data.TotalOSM = osm;
                        data.TotalMunicipalidad = 0;
                        data.TotalGeneral = osm;
                        data.TotalRecibosMuni = 0;
                        debugSb.AppendLine($"Fallback - ÚLTIMO Total Importe asignado a OSM: {osm:N2}");
                        data.DebugInfo = debugSb.ToString();
                        return;
                    }
                    else
                    {
                        debugSb.AppendLine("ERROR: No se encontró ningún 'Total Importe:' en el PDF");
                    }
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

        /// <summary>
        /// Método fallback que usa PdfPig para extraer OSM y MUNI cuando iText7 falla
        /// </summary>
        private (decimal muni, decimal osm, string debug) ExtractAnchoredTotals(byte[] pdfData)
        {
            try
            {
                // Crear archivo temporal para PdfPig
                var tempFile = Path.GetTempFileName();
                File.WriteAllBytes(tempFile, pdfData);
                
                try
                {
                    var extractor = new PdfPigExtractor();
                    var result = extractor.ExtractData(tempFile);
                    
                    decimal muni = result.Muni ?? 0;
                    decimal osm = result.Osm ?? 0;
                    
                    string debug = $"PdfPig - MUNI: {muni:N2}, OSM: {osm:N2}";
                    
                    return (muni, osm, debug);
                }
                finally
                {
                    // Limpiar archivo temporal
                    if (File.Exists(tempFile))
                        File.Delete(tempFile);
                }
            }
            catch (Exception ex)
            {
                return (0, 0, $"Error PdfPig: {ex.Message}");
            }
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
        public int TotalRecibosMuni { get; set; }
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
