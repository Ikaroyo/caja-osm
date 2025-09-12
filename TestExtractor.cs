using System;
using System.IO;
using PdfExtractor.Services;

class Program
{
    static void Main()
    {
        try
        {
            var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine("PDF no encontrado");
                return;
            }

            var extractor = new PdfDataExtractor();
            
            // Primero extraer el texto para ver el contenido
            var pdfBytes = File.ReadAllBytes(pdfPath);
            var fullText = extractor.ExtractTextFromPdf(pdfBytes);
            
            Console.WriteLine("=== CONTENIDO COMPLETO DEL PDF ===");
            Console.WriteLine(fullText);
            Console.WriteLine("=== FIN CONTENIDO PDF ===");
            
            // Ahora extraer los datos estructurados
            var data = extractor.ExtractStructuredData(fullText);
            
            Console.WriteLine("\n=== DATOS EXTRAÍDOS ===");
            Console.WriteLine($"Lote: '{data.Lote}'");
            Console.WriteLine($"Fecha: '{data.FechaString}'");
            Console.WriteLine($"Usuario: '{data.Usuario}'");
            Console.WriteLine($"OSM: {data.TotalOSM}");
            Console.WriteLine($"MUNI: {data.TotalMunicipalidad}");
            Console.WriteLine($"Total: {data.TotalGeneral}");
            
            Console.WriteLine("\nPresiona Enter para continuar...");
            Console.ReadLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ReadLine();
        }
    }
}