using System;
using System.IO;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

class DebugExtractor
{
    static void Main(string[] args)
    {
        string pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
        
        try
        {
            Console.WriteLine($"=== ANÁLISIS DETALLADO DE {Path.GetFileName(pdfPath)} ===");
            
            using (var reader = new PdfReader(pdfPath))
            using (var document = new PdfDocument(reader))
            {
                string fullText = "";
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var page = document.GetPage(i);
                    string pageText = PdfTextExtractor.GetTextFromPage(page);
                    fullText += pageText + "\n";
                }
                
                Console.WriteLine("=== TEXTO COMPLETO DEL PDF ===");
                string[] lines = fullText.Split('\n');
                for (int i = 0; i < Math.Min(lines.Length, 100); i++) // Primeras 100 líneas
                {
                    Console.WriteLine($"{i:D3}: {lines[i]}");
                }
                
                Console.WriteLine("\n=== ANÁLISIS DE PATRONES ===");
                
                // Buscar todas las apariciones de "Total Importe"
                var totalImporteMatches = Regex.Matches(fullText, @"Total\s+Importe\s*:?\s*([\d,\.]+)", RegexOptions.IgnoreCase);
                Console.WriteLine($"\nTotal Importe encontrados: {totalImporteMatches.Count}");
                for (int i = 0; i < totalImporteMatches.Count; i++)
                {
                    var value = ParseDecimal(totalImporteMatches[i].Groups[1].Value);
                    Console.WriteLine($"  {i + 1}. {totalImporteMatches[i].Value} -> Valor: {value:F2}");
                    
                    // Buscar contexto alrededor
                    int startIndex = Math.Max(0, totalImporteMatches[i].Index - 100);
                    int endIndex = Math.Min(fullText.Length - 1, totalImporteMatches[i].Index + totalImporteMatches[i].Length + 100);
                    string context = fullText.Substring(startIndex, endIndex - startIndex);
                    Console.WriteLine($"     Contexto: ...{context.Replace('\n', ' ').Replace('\r', ' ')}...");
                }
                
                // Buscar todas las apariciones de números grandes
                var numberMatches = Regex.Matches(fullText, @"[\d,\.]{5,}", RegexOptions.IgnoreCase);
                Console.WriteLine($"\nNúmeros grandes encontrados: {numberMatches.Count}");
                var uniqueNumbers = new HashSet<decimal>();
                foreach (Match match in numberMatches)
                {
                    var value = ParseDecimal(match.Value);
                    if (value > 1000 && value < 10000000)
                    {
                        uniqueNumbers.Add(value);
                    }
                }
                
                var sortedNumbers = uniqueNumbers.OrderByDescending(x => x).ToList();
                for (int i = 0; i < Math.Min(sortedNumbers.Count, 10); i++)
                {
                    Console.WriteLine($"  {i + 1}. {sortedNumbers[i]:F2}");
                }
                
                // Buscar contexto alrededor de "municipalidad"
                var muniMatches = Regex.Matches(fullText, @".{0,100}municipalidad.{0,100}", RegexOptions.IgnoreCase);
                Console.WriteLine($"\nContexto de 'municipalidad': {muniMatches.Count}");
                foreach (Match match in muniMatches)
                {
                    Console.WriteLine($"  {match.Value.Replace('\n', ' ').Replace('\r', ' ')}");
                }
                
                // Buscar contexto alrededor de "Obras Sanitarias"
                var osmMatches = Regex.Matches(fullText, @".{0,100}obras sanitarias.{0,100}", RegexOptions.IgnoreCase);
                Console.WriteLine($"\nContexto de 'obras sanitarias': {osmMatches.Count}");
                foreach (Match match in osmMatches)
                {
                    Console.WriteLine($"  {match.Value.Replace('\n', ' ').Replace('\r', ' ')}");
                }
                
                // Análisis específico para entender la estructura
                Console.WriteLine("\n=== ANÁLISIS DE ESTRUCTURA ===");
                var recibosMatches = Regex.Matches(fullText, @"Total\s+Recibos.*?(\d+)", RegexOptions.IgnoreCase);
                Console.WriteLine($"Total Recibos encontrados: {recibosMatches.Count}");
                foreach (Match match in recibosMatches)
                {
                    Console.WriteLine($"  {match.Value}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        
        Console.WriteLine("\nPresiona cualquier tecla para continuar...");
        Console.ReadKey();
    }
    
    private static decimal ParseDecimal(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        // Limpiar el valor
        value = value.Trim().Replace(" ", "");
        
        // Si contiene coma como separador decimal
        if (value.Contains(","))
        {
            // Si también contiene punto, asumir que punto es separador de miles
            if (value.Contains("."))
            {
                value = value.Replace(".", "");
                value = value.Replace(",", ".");
            }
            else
            {
                // Solo coma, podría ser separador decimal o de miles
                int comaIndex = value.LastIndexOf(',');
                string afterComa = value.Substring(comaIndex + 1);
                
                if (afterComa.Length == 2) // Probablemente decimal
                {
                    value = value.Replace(",", ".");
                }
                else // Probablemente separador de miles
                {
                    value = value.Replace(",", "");
                }
            }
        }

        if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
        {
            return result;
        }

        return 0;
    }
}