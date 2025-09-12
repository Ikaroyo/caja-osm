using System;
using System.Threading.Tasks;
using PdfExtractor.Services;

namespace PdfExtractor
{
    class SimpleTest
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PRUEBA SIMPLE DE EXTRACCIÓN ===");
            
            var testUrls = new[]
            {
                "http://localhost:8081/getjobid484197.pdf",
                "http://localhost:8081/getjobid485764.pdf", 
                "http://localhost:8081/getjobid485763.pdf",
                "http://localhost:8081/getjobid485773.pdf"
            };
            
            using var extractor = new PdfDataExtractor();
            
            foreach (var url in testUrls)
            {
                Console.WriteLine($"\nProcesando: {url}");
                try
                {
                    var result = await extractor.ExtractFromUrlAsync(url);
                    
                    Console.WriteLine($"Lote: {result.Lote}");
                    Console.WriteLine($"Usuario: {result.Usuario}");
                    Console.WriteLine($"OSM: {result.TotalOSM:N2}");
                    Console.WriteLine($"MUNI: {result.TotalMunicipalidad:N2}");
                    Console.WriteLine($"TARJETACR: {result.TarjetaCredito:N2}");
                    Console.WriteLine($"TARJETADE: {result.TarjetaDebito:N2}");
                    Console.WriteLine($"CHEQUEDIF: {result.ChequeDiferido:N2}");
                    
                    Console.WriteLine("\n--- DebugInfo ---");
                    Console.WriteLine(result.DebugInfo);
                    Console.WriteLine("--- Fin Debug ---");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
            
            Console.WriteLine("\n=== FIN PRUEBAS ===");
        }
    }
}