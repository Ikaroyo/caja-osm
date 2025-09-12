using System;
using System.Threading.Tasks;
using PdfExtractor.Services;

namespace PdfTestConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== PRUEBA DE EXTRACCIÓN PDF ===");
            
            var expectedResults = new[]
            {
                new { url = "http://localhost:8081/getjobid484197.pdf", lote = "403279116", usuario = "CMAGALLANES", osm = 1478575.98m, muni = 33016.1m, tarjetaCr = 527538.04m, tarjetaDe = 406934.64m, chequeDif = 0m },
                new { url = "http://localhost:8081/getjobid485764.pdf", lote = "403279119", usuario = "FGONZALEZ", osm = 3243111.06m, muni = 234994.47m, tarjetaCr = 1115156.88m, tarjetaDe = 1218373.94m, chequeDif = 0m },
                new { url = "http://localhost:8081/getjobid485763.pdf", lote = "403279120", usuario = "SCRESPO", osm = 5159825.54m, muni = 223836.74m, tarjetaCr = 1158704.34m, tarjetaDe = 749363.52m, chequeDif = 0m },
                new { url = "http://localhost:8081/getjobid485773.pdf", lote = "403279121", usuario = "FGONZALEZ", osm = 6154845.11m, muni = 356596.81m, tarjetaCr = 138107.56m, tarjetaDe = 1372943.02m, chequeDif = 2612475.91m }
            };
            
            using var extractor = new PdfDataExtractor();
            
            for (int i = 0; i < expectedResults.Length; i++)
            {
                var expected = expectedResults[i];
                Console.WriteLine($"\n--- PROCESANDO {i+1}/4: {expected.url} ---");
                
                try
                {
                    var result = await extractor.ExtractFromUrlAsync(expected.url);
                    
                    Console.WriteLine($"Lote: '{result.Lote}' (esperado: '{expected.lote}') {GetStatus(result.Lote == expected.lote)}");
                    Console.WriteLine($"Usuario: '{result.Usuario}' (esperado: '{expected.usuario}') {GetStatus(result.Usuario == expected.usuario)}");
                    Console.WriteLine($"OSM: {result.TotalOSM:F2} (esperado: {expected.osm:F2}) {GetStatus(Math.Abs(result.TotalOSM - expected.osm) < 0.01m)}");
                    Console.WriteLine($"MUNI: {result.TotalMunicipalidad:F2} (esperado: {expected.muni:F2}) {GetStatus(Math.Abs(result.TotalMunicipalidad - expected.muni) < 0.01m)}");
                    Console.WriteLine($"TARJETACR: {result.TarjetaCredito:F2} (esperado: {expected.tarjetaCr:F2}) {GetStatus(Math.Abs(result.TarjetaCredito - expected.tarjetaCr) < 0.01m)}");
                    Console.WriteLine($"TARJETADE: {result.TarjetaDebito:F2} (esperado: {expected.tarjetaDe:F2}) {GetStatus(Math.Abs(result.TarjetaDebito - expected.tarjetaDe) < 0.01m)}");
                    
                    if (expected.chequeDif > 0)
                        Console.WriteLine($"CHEQUEDIF: {result.ChequeDiferido:F2} (esperado: {expected.chequeDif:F2}) {GetStatus(Math.Abs(result.ChequeDiferido - expected.chequeDif) < 0.01m)}");
                    
                    // Solo mostrar texto completo para el primer PDF que falle
                    if (i == 0 && Math.Abs(result.TotalOSM - expected.osm) > 0.01m)
                    {
                        Console.WriteLine("\n>>> TEXTO COMPLETO DEL PDF (solo primer fallo):");
                        Console.WriteLine(result.RawText ?? "Sin texto extraído");
                        Console.WriteLine("<<< FIN TEXTO COMPLETO");
                    }
                    
                    Console.WriteLine("\n>>> DebugInfo:");
                    Console.WriteLine(result.DebugInfo ?? "Sin debug info");
                    Console.WriteLine("<<< Fin DebugInfo");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ ERROR: {ex.Message}");
                    if (ex.InnerException != null)
                        Console.WriteLine($"Inner: {ex.InnerException.Message}");
                }
            }
            
            Console.WriteLine("\n=== PRUEBAS COMPLETADAS ===");
        }
        
        static string GetStatus(bool isCorrect) => isCorrect ? "✅" : "❌";
    }
}