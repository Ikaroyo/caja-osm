using System;
using System.Threading.Tasks;
using PdfExtractor.Services;

namespace PdfExtractor
{
    public class TestExtraction
    {
        private static readonly (string url, string lote, string fecha, string usuario, decimal osm, decimal muni, decimal tarjetaCr, decimal tarjetaDe, decimal cheque, decimal chequeDif)[] expectedData = 
        {
            ("http://localhost:8081/getjobid484197.pdf", "403279116", "5-sept-25", "CMAGALLANES", 1478575.98m, 33016.1m, 527538.04m, 406934.64m, 0m, 0m),
            ("http://localhost:8081/getjobid485764.pdf", "403279119", "8-sept-25", "FGONZALEZ", 3243111.06m, 234994.47m, 1115156.88m, 1218373.94m, 0m, 0m),
            ("http://localhost:8081/getjobid485763.pdf", "403279120", "9-sept-25", "SCRESPO", 5159825.54m, 223836.74m, 1158704.34m, 749363.52m, 0m, 0m),
            ("http://localhost:8081/getjobid485773.pdf", "403279121", "9-sept-25", "FGONZALEZ", 6154845.11m, 356596.81m, 138107.56m, 1372943.02m, 0m, 2612475.91m)
        };

        public static async Task RunTests()
        {
            Console.WriteLine("=== INICIANDO PRUEBAS DE EXTRACCIÓN ===");
            using var extractor = new PdfDataExtractor();
            
            Console.WriteLine("=== PRUEBA DE EXTRACCIÓN DE PDFs ===\n");
            
            for (int i = 0; i < expectedData.Length; i++)
            {
                var expected = expectedData[i];
                Console.WriteLine($"Procesando: {expected.url}");
                
                try
                {
                    var result = await extractor.ExtractFromUrlAsync(expected.url);
                    
                    Console.WriteLine($"Lote: {result.Lote} (esperado: {expected.lote}) {(result.Lote == expected.lote ? "✓" : "✗")}");
                    Console.WriteLine($"Usuario: {result.Usuario} (esperado: {expected.usuario}) {(result.Usuario == expected.usuario ? "✓" : "✗")}");
                    Console.WriteLine($"OSM: {result.TotalOSM:N2} (esperado: {expected.osm:N2}) {(Math.Abs(result.TotalOSM - expected.osm) < 0.01m ? "✓" : "✗")}");
                    Console.WriteLine($"MUNI: {result.TotalMunicipalidad:N2} (esperado: {expected.muni:N2}) {(Math.Abs(result.TotalMunicipalidad - expected.muni) < 0.01m ? "✓" : "✗")}");
                    Console.WriteLine($"TARJETACR: {result.TarjetaCredito:N2} (esperado: {expected.tarjetaCr:N2}) {(Math.Abs(result.TarjetaCredito - expected.tarjetaCr) < 0.01m ? "✓" : "✗")}");
                    Console.WriteLine($"TARJETADE: {result.TarjetaDebito:N2} (esperado: {expected.tarjetaDe:N2}) {(Math.Abs(result.TarjetaDebito - expected.tarjetaDe) < 0.01m ? "✓" : "✗")}");
                    
                    if (expected.chequeDif > 0)
                        Console.WriteLine($"CHEQUEDIF: {result.ChequeDiferido:N2} (esperado: {expected.chequeDif:N2}) {(Math.Abs(result.ChequeDiferido - expected.chequeDif) < 0.01m ? "✓" : "✗")}");
                    
                    Console.WriteLine("\n--- DebugInfo ---");
                    Console.WriteLine(result.DebugInfo);
                    Console.WriteLine("=================\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                    Console.WriteLine($"Stack: {ex.StackTrace}\n");
                }
            }
            
            Console.WriteLine("=== PRUEBAS COMPLETADAS ===");
        }
    }
}