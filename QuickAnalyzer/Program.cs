using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace QuickAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            string pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
            
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"PDF no encontrado: {pdfPath}");
                return;
            }

            Console.WriteLine("=== ANÁLISIS RÁPIDO DEL PDF ===");
            Console.WriteLine($"Archivo: {Path.GetFileName(pdfPath)}");
            Console.WriteLine();

            try
            {
                using (PdfReader reader = new PdfReader(pdfPath))
                using (PdfDocument pdfDoc = new PdfDocument(reader))
                {
                    int pageCount = pdfDoc.GetNumberOfPages();
                    Console.WriteLine($"Páginas: {pageCount}");
                    Console.WriteLine();

                    StringBuilder fullText = new StringBuilder();
                    for (int i = 1; i <= pageCount; i++)
                    {
                        var strategy = new SimpleTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                        fullText.AppendLine($"=== PÁGINA {i} ===");
                        fullText.AppendLine(pageText);
                        fullText.AppendLine();
                    }

                    string text = fullText.ToString();
                    
                    // Buscar secciones de Total Importe
                    AnalyzeTotalImporteSections(text);
                    
                    Console.WriteLine("\n=== CONTENIDO COMPLETO ===");
                    Console.WriteLine(text);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPresiona cualquier tecla para cerrar...");
            Console.ReadKey();
        }

        static void AnalyzeTotalImporteSections(string text)
        {
            Console.WriteLine("=== ANÁLISIS DE SECCIONES 'Total Importe' ===");
            
            // Patrón para encontrar todas las apariciones de "Total Importe"
            var totalImportePattern = @"Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)";
            var matches = Regex.Matches(text, totalImportePattern, RegexOptions.IgnoreCase);
            
            Console.WriteLine($"Encontradas {matches.Count} secciones de 'Total Importe':");
            Console.WriteLine();
            
            for (int i = 0; i < matches.Count; i++)
            {
                var match = matches[i];
                string value = match.Groups[1].Value;
                
                Console.WriteLine($"--- Sección {i + 1} ---");
                Console.WriteLine($"Valor: {value}");
                
                // Obtener contexto alrededor del match
                int start = Math.Max(0, match.Index - 200);
                int length = Math.Min(400, text.Length - start);
                string context = text.Substring(start, length);
                
                Console.WriteLine("Contexto:");
                Console.WriteLine(context);
                Console.WriteLine();
                
                // Buscar palabras clave en el contexto
                bool hasOSM = context.Contains("OSM", StringComparison.OrdinalIgnoreCase);
                bool hasMUNI = context.Contains("MUNI", StringComparison.OrdinalIgnoreCase);
                bool hasMunicipalidad = context.Contains("Municipalidad", StringComparison.OrdinalIgnoreCase);
                
                Console.WriteLine($"Contiene OSM: {hasOSM}");
                Console.WriteLine($"Contiene MUNI: {hasMUNI}");
                Console.WriteLine($"Contiene Municipalidad: {hasMunicipalidad}");
                Console.WriteLine(new string('=', 50));
            }
            
            // Intentar extraer con los patrones actuales
            Console.WriteLine("\n=== PRUEBA DE PATRONES ACTUALES ===");
            
            // Patrones OSM
            string[] osmPatterns = {
                @"OSM.*?Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)",
                @"(?:OSM|Obras\s+Sociales).*?Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)",
                @"Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)(?=.*OSM)",
                @"Total\s+Importe\s*:\s*\$?\s*(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?).*?OSM",
                @"OSM.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?).*?Total\s+Importe"
            };
            
            // Patrones MUNI
            string[] muniPatterns = {
                @"(?:MUNI|Municipalidad).*?Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)",
                @"Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)(?=.*(?:MUNI|Municipalidad))",
                @"Total\s+Importe\s*:\s*\$?\s*(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?).*?(?:MUNI|Municipalidad)",
                @"(?:MUNI|Municipalidad).*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?).*?Total\s+Importe",
                @"(?<!OSM.{0,100})(?:MUNI|Municipalidad).*?Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)",
                @"Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)(?!.*OSM)(?=.*(?:MUNI|Municipalidad))",
                @"(?:MUNI|Municipalidad)(?!.*OSM).*?Total\s+Importe.*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)",
                @"Total\s+Importe[^O]*?(\d{1,3}(?:[.,]\d{3})*(?:[.,]\d{2})?)(?=.*(?:MUNI|Municipalidad))"
            };
            
            Console.WriteLine("PATRONES OSM:");
            for (int i = 0; i < osmPatterns.Length; i++)
            {
                var osmMatch = Regex.Match(text, osmPatterns[i], RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Console.WriteLine($"  Patrón {i + 1}: {(osmMatch.Success ? osmMatch.Groups[1].Value : "No encontrado")}");
            }
            
            Console.WriteLine("\nPATRONES MUNI:");
            for (int i = 0; i < muniPatterns.Length; i++)
            {
                var muniMatch = Regex.Match(text, muniPatterns[i], RegexOptions.IgnoreCase | RegexOptions.Singleline);
                Console.WriteLine($"  Patrón {i + 1}: {(muniMatch.Success ? muniMatch.Groups[1].Value : "No encontrado")}");
            }
        }
    }
}
