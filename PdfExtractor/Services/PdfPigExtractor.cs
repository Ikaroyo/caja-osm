using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace PdfExtractor.Services
{
    public class PdfPigExtractionResult
    {
        public string? Lote { get; set; }
        public string? Usuario { get; set; }
        public decimal? TarjetaCredito { get; set; }
        public decimal? TarjetaDebito { get; set; }
        public decimal? ChequeDiferido { get; set; }
        public decimal? Muni { get; set; }
        public decimal? Osm { get; set; }
    }

    /// <summary>
    /// Advanced PDF extractor using PdfPig with robust text cleaning and pattern matching.
    /// Implements best practices for OSM document processing including page concatenation,
    /// artifact removal, and intelligent pattern detection with lookahead/lookbehind support.
    /// </summary>
    public class PdfPigExtractor
    {
        /// <summary>
        /// Main method to process a PDF file and extract OSM data
        /// </summary>
        public PdfPigExtractionResult ExtractData(string filePath)
        {
            var result = new PdfPigExtractionResult();
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"PDF file not found: {filePath}");
            }

            // Use PdfPig to open and read all text from the PDF
            using PdfDocument document = PdfDocument.Open(filePath);
            
            // Step 1: Concatenate all pages into a single string
            var allPagesText = new StringBuilder();
            foreach (var page in document.GetPages())
            {
                allPagesText.AppendLine(page.Text);
            }
            
            // Step 2: Clean artifacts and disruptive elements
            string cleanText = CleanPdfText(allPagesText.ToString());

            // Step 3: Extract data using improved patterns on clean text
            result.Lote = ExtractSingleValue(cleanText, @"Lote\s*C?\s*:?\s*(\w+)(?:\s+|$)");
            result.Usuario = ExtractSingleValue(cleanText, @"Usuario\s*:?\s*(\w+)|Cajero\s*:?\s*(\w+)")?.Replace("Cajero", "").Trim();
            
            // Extract payment method totals with improved patterns
            result.TarjetaCredito = ExtractDecimal(cleanText, @"TARJETACR\s*:?\s*([\d.,]+)(?=\s|$|\n)");
            result.TarjetaDebito = ExtractDecimal(cleanText, @"TARJETADE\s*:?\s*([\d.,]+)(?=\s|$|\n)");
            result.ChequeDiferido = ExtractDecimal(cleanText, @"CHEQUEDIF\s*:?\s*([\d.,]+)(?=\s|$|\n)");
            
            // LÓGICA SIMPLIFICADA SEGÚN ESPECIFICACIÓN:
            // 1. Verificar si existe la cadena "Por 774-Municipalidad V.Mercedes"
            bool hasMuniPattern = cleanText.Contains("Por 774-Municipalidad V.Mercedes");
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] Has MUNI pattern: {hasMuniPattern}");
            
            if (hasMuniPattern)
            {
                // CASO NORMAL: Hay MUNI - extraer MUNI y OSM por separado
                System.Diagnostics.Debug.WriteLine("[PdfPigExtractor] Using NORMAL logic (has MUNI)");
                result.Muni = ExtractDecimal(cleanText, @"Por 774-Municipalidad V\.Mercedes[\s\S]*?Total Importe:\s*([\d.,]+)")
                             ?? ExtractDecimal(cleanText, @"MUNI\s*:?\s*([\d.,]+)(?=\s|$|\n)")
                             ?? ExtractDecimal(cleanText, @"Municipalidad[\s\S]*?([\d.,]+)");
                
                // Buscar OSM con patrones específicos cuando hay MUNI - MÁS PATRONES
                result.Osm = ExtractDecimal(cleanText, @"OSM\s*:?\s*([\d.,]+)(?=\s|MUNI|$|\n)")
                            ?? ExtractDecimal(cleanText, @"Obras Sanitarias[\s\S]*?Total Importe:\s*([\d.,]+)")
                            ?? ExtractDecimal(cleanText, @"Importe Lote\s+([\d.,]+)")
                            // PATRÓN ADICIONAL: Si hay tanto MUNI como OSM, buscar el segundo "Total Importe"
                            ?? ExtractSecondTotalImporte(cleanText);
                            
                System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] Normal results: MUNI={result.Muni?.ToString("N2") ?? "null"}, OSM={result.Osm?.ToString("N2") ?? "null"}");
            }
            else
            {
                // CASO ESPECIAL: NO hay "Por 774-Municipalidad V.Mercedes"
                // → El último "Total Importe:" es OSM, no hay MUNI
                System.Diagnostics.Debug.WriteLine("[PdfPigExtractor] Using SPECIAL logic (NO MUNI pattern found)");
                result.Muni = null;
                result.Osm = ExtractLastTotalImporte(cleanText);
                
                System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] Special results: MUNI=null, OSM={result.Osm?.ToString("N2") ?? "null"}");
            }

            // DEBUG: Si OSM y MUNI son null/0, mostrar información adicional
            if ((result.Osm == null || result.Osm == 0) && (result.Muni == null || result.Muni == 0))
            {
                System.Diagnostics.Debug.WriteLine("[PdfPigExtractor] WARNING: Both OSM and MUNI are null/0!");
                System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] File: {Path.GetFileName(filePath)}");
                System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] Had MUNI pattern: {cleanText.Contains("Por 774-Municipalidad V.Mercedes")}");
                
                // Mostrar las primeras líneas del texto limpio para debug
                var firstLines = cleanText.Split('\n').Take(10);
                System.Diagnostics.Debug.WriteLine("[PdfPigExtractor] First 10 lines of clean text:");
                foreach (var line in firstLines)
                {
                    System.Diagnostics.Debug.WriteLine($"  > {line.Trim()}");
                }
                
                // Buscar manualmente "Total Importe"
                var debugMatches = System.Text.RegularExpressions.Regex.Matches(cleanText, @"Total Importe", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                System.Diagnostics.Debug.WriteLine($"[PdfPigExtractor] Found {debugMatches.Count} instances of 'Total Importe' (without values)");
            }

            return result;
        }

        /// <summary>
        /// Clean PDF text by removing headers, footers and disruptive artifacts.
        /// This implements best practices for robust PDF text processing.
        /// </summary>
        private string CleanPdfText(string rawText)
        {
            if (string.IsNullOrEmpty(rawText))
                return "";

            // LIMPIEZA MÁS CONSERVADORA - solo eliminar lo esencial
            var patterns = new[]
            {
                @"Página\s+\d+.*?\n",           // Page numbers
                @"^\s*[-=]{10,}\s*$"           // Long separator lines only
            };

            var cleanText = rawText;
            foreach (var pattern in patterns)
            {
                cleanText = Regex.Replace(cleanText, pattern, "", 
                    RegexOptions.Multiline | RegexOptions.IgnoreCase);
            }

            // Normalización MÁS SUAVE - preservar más estructura
            cleanText = Regex.Replace(cleanText, @"\s{3,}", " ");        // Solo reemplazar 3+ espacios
            cleanText = Regex.Replace(cleanText, @"\n\s*\n\s*\n", "\n\n"); // Solo reemplazar 3+ saltos de línea
            
            return cleanText.Trim();
        }

        /// <summary>
        /// Helper to run regex and return the first captured group as a string.
        /// Supports patterns with OR conditions (multiple capture groups).
        /// </summary>
        private string? ExtractSingleValue(string text, string pattern)
        {
            Match match = Regex.Match(text, pattern);
            if (!match.Success) return null;
            
            // For patterns with an OR condition, result might be in Group 1 or 2
            return match.Groups[1].Value.Trim() != "" ? match.Groups[1].Value.Trim() : 
                   match.Groups.Count > 2 ? match.Groups[2].Value.Trim() : null;
        }

        /// <summary>
        /// Helper to run regex and parse the result to a decimal using Argentine culture.
        /// Handles various number formats commonly found in OSM documents.
        /// </summary>
        private decimal? ExtractDecimal(string text, string pattern)
        {
            var valueStr = ExtractSingleValue(text, pattern);
            if (valueStr == null) return null;

            // Standardize number format and parse using Argentine culture
            var culture = new CultureInfo("es-AR"); // Argentine culture for parsing
            if (decimal.TryParse(valueStr.Replace('.', culture.NumberFormat.NumberDecimalSeparator[0]).Replace(',', culture.NumberFormat.NumberDecimalSeparator[0]), 
                NumberStyles.Any, 
                culture, 
                out decimal parsedValue))
            {
                return parsedValue;
            }

            return null;
        }

        /// <summary>
        /// Extrae el valor del último "Total Importe:" encontrado en el texto.
        /// Usado para casos especiales donde no hay MUNI y el último Total Importe es OSM.
        /// </summary>
        private decimal? ExtractLastTotalImporte(string text)
        {
            // Buscar TODAS las ocurrencias de "Total Importe:" con sus valores
            var matches = Regex.Matches(text, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
            
            System.Diagnostics.Debug.WriteLine($"[ExtractLastTotalImporte] Found {matches.Count} 'Total Importe:' occurrences");
            
            if (matches.Count == 0)
                return null;
            
            // Debug: mostrar todas las ocurrencias
            for (int i = 0; i < matches.Count; i++)
            {
                var value = matches[i].Groups[1].Value;
                System.Diagnostics.Debug.WriteLine($"[ExtractLastTotalImporte] [{i+1}] Total Importe: {value}");
            }
            
            // Tomar la ÚLTIMA ocurrencia (la más importante)
            var lastMatch = matches[matches.Count - 1];
            var valueStr = lastMatch.Groups[1].Value.Trim();
            
            System.Diagnostics.Debug.WriteLine($"[ExtractLastTotalImporte] Using LAST occurrence: {valueStr}");
            
            // Parsear usando cultura argentina
            var culture = new CultureInfo("es-AR");
            if (decimal.TryParse(valueStr.Replace('.', culture.NumberFormat.NumberDecimalSeparator[0]).Replace(',', culture.NumberFormat.NumberDecimalSeparator[0]), 
                NumberStyles.Any, 
                culture, 
                out decimal parsedValue))
            {
                System.Diagnostics.Debug.WriteLine($"[ExtractLastTotalImporte] Parsed successfully: {parsedValue:N2}");
                return parsedValue;
            }

            System.Diagnostics.Debug.WriteLine($"[ExtractLastTotalImporte] Failed to parse: {valueStr}");
            return null;
        }

        /// <summary>
        /// Para casos donde hay MUNI y OSM: extrae el SEGUNDO "Total Importe:" (el primero sería MUNI)
        /// </summary>
        private decimal? ExtractSecondTotalImporte(string text)
        {
            var matches = Regex.Matches(text, @"Total Importe:\s*([\d.,]+)", RegexOptions.IgnoreCase);
            
            System.Diagnostics.Debug.WriteLine($"[ExtractSecondTotalImporte] Found {matches.Count} 'Total Importe:' occurrences");
            
            if (matches.Count >= 2)
            {
                // Tomar la SEGUNDA ocurrencia (primera = MUNI, segunda = OSM)
                var secondMatch = matches[1];
                var valueStr = secondMatch.Groups[1].Value.Trim();
                
                System.Diagnostics.Debug.WriteLine($"[ExtractSecondTotalImporte] Using SECOND occurrence: {valueStr}");
                
                var culture = new CultureInfo("es-AR");
                if (decimal.TryParse(valueStr.Replace('.', culture.NumberFormat.NumberDecimalSeparator[0]).Replace(',', culture.NumberFormat.NumberDecimalSeparator[0]), 
                    NumberStyles.Any, 
                    culture, 
                    out decimal parsedValue))
                {
                    System.Diagnostics.Debug.WriteLine($"[ExtractSecondTotalImporte] Parsed successfully: {parsedValue:N2}");
                    return parsedValue;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Debug method to show PDF content for pattern analysis
        /// </summary>
        public void ShowPdfContent(string pdfPath)
        {
            using var document = PdfDocument.Open(pdfPath);
            Console.WriteLine($"📄 PDF: {Path.GetFileName(pdfPath)} ({document.NumberOfPages} páginas)");
            
            for (int pageNum = 1; pageNum <= Math.Min(2, document.NumberOfPages); pageNum++)
            {
                var page = document.GetPage(pageNum);
                var text = page.Text;
                
                Console.WriteLine($"\n--- PÁGINA {pageNum} ---");
                Console.WriteLine(text.Substring(0, Math.Min(1000, text.Length)));
                if (text.Length > 1000) Console.WriteLine("... (texto truncado)");
            }
        }
    }
}
