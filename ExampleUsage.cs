using System;
using System.Threading.Tasks;
using PdfExtractor.Services;

namespace PdfExtractor.Examples
{
    /// <summary>
    /// Ejemplos de uso del extractor de datos PDF independiente
    /// </summary>
    public class ExampleUsage
    {
        public async Task ExampleExtractFromUrl()
        {
            using var extractor = new PdfDataExtractor();
            
            try
            {
                var url = "http://192.168.100.80:7778/reports/rwservlet/getjobid484197?SERVER=rep_vmo_bck";
                var data = await extractor.ExtractFromUrlAsync(url);
                
                Console.WriteLine($"Lote: {data.Lote}");
                Console.WriteLine($"Fecha: {data.FechaString}");
                Console.WriteLine($"Usuario: {data.Usuario}");
                Console.WriteLine($"Total OSM: {data.TotalOSM:C}");
                Console.WriteLine($"Total Municipalidad: {data.TotalMunicipalidad:C}");
                Console.WriteLine($"Total Tarjetas: {data.TotalTarjetas:C}");
            }
            catch (PdfExtractionException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        public void ExampleExtractFromFile()
        {
            using var extractor = new PdfDataExtractor();
            
            try
            {
                var filePath = @"C:\path\to\your\file.pdf";
                var data = extractor.ExtractFromFile(filePath);
                
                Console.WriteLine($"Datos extraídos del archivo: {filePath}");
                Console.WriteLine($"Lote: {data.Lote}, Fecha: {data.FechaString}");
            }
            catch (PdfExtractionException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
