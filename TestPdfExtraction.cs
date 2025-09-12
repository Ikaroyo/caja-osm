using System;
using PdfExtractor.Services;

namespace PdfExtractor.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var extractor = new PdfDataExtractor();
                var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
                
                Console.WriteLine("Extrayendo datos del PDF...");
                var data = extractor.ExtractFromFile(pdfPath);
                
                Console.WriteLine($"Lote: {data.Lote}");
                Console.WriteLine($"Fecha: {data.FechaString}");
                Console.WriteLine($"Usuario: {data.Usuario}");
                Console.WriteLine($"OSM: {data.TotalOSM}");
                Console.WriteLine($"MUNI: {data.TotalMunicipalidad}");
                Console.WriteLine($"Total: {data.TotalGeneral}");
                
                Console.WriteLine("\nPresiona cualquier tecla para continuar...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.ReadKey();
            }
        }
    }
}