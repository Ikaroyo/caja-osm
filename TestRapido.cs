using System;
using PdfExtractor.Services;

// Prueba rápida de la solución implementada
class TestRapido 
{
    static void Main()
    {
        var extractor = new PdfDataExtractor();
        
        Console.WriteLine("=== PRUEBA RÁPIDA PDF EXTRACTION ===");
        Console.WriteLine("Probando URL del PDF 484197...\n");
        
        try 
        {
            string testUrl = "http://localhost:8081/getjobid484197.pdf";
            var result = extractor.ExtractFromUrlAsync(testUrl).Result;
            
            Console.WriteLine($"Lote: {result.Lote}");
            Console.WriteLine($"Usuario: {result.Usuario}");
            Console.WriteLine($"OSM: {result.TotalOSM:N2}");
            Console.WriteLine($"MUNI: {result.TotalMunicipalidad:N2}");
            Console.WriteLine($"EFECTIVO: {result.Efectivo:N2}");
            Console.WriteLine($"TARJETA CR: {result.TarjetaCredito:N2}");
            Console.WriteLine($"TARJETA DB: {result.TarjetaDebito:N2}");
            Console.WriteLine($"CHEQUE DIF: {result.ChequeDiferido:N2}");
            Console.WriteLine();
            Console.WriteLine("=== DEBUG INFO ===");
            Console.WriteLine(result.DebugInfo);
            
            // Validar contra valores esperados
            Console.WriteLine("\n=== VALIDACIÓN ===");
            bool osmOk = Math.Abs(result.TotalOSM - 1478575.98m) < 0.1m;
            bool muniOk = Math.Abs(result.TotalMunicipalidad - 33016.10m) < 0.1m;
            bool efectivoOk = Math.Abs(result.Efectivo - 577119.40m) < 0.1m;
            bool tcOk = Math.Abs(result.TarjetaCredito - 527538.04m) < 0.1m;
            bool tdOk = Math.Abs(result.TarjetaDebito - 406934.64m) < 0.1m;
            bool chqOk = Math.Abs(result.ChequeDiferido - 0) < 0.1m;
            
            Console.WriteLine($"OSM: {(osmOk ? "✅" : "❌")} (esperado: 1,478,575.98, obtenido: {result.TotalOSM:N2})");
            Console.WriteLine($"MUNI: {(muniOk ? "✅" : "❌")} (esperado: 33,016.10, obtenido: {result.TotalMunicipalidad:N2})");
            Console.WriteLine($"EFECTIVO: {(efectivoOk ? "✅" : "❌")} (esperado: 577,119.40, obtenido: {result.Efectivo:N2})");
            Console.WriteLine($"TARJETA CR: {(tcOk ? "✅" : "❌")} (esperado: 527,538.04, obtenido: {result.TarjetaCredito:N2})");
            Console.WriteLine($"TARJETA DB: {(tdOk ? "✅" : "❌")} (esperado: 406,934.64, obtenido: {result.TarjetaDebito:N2})");
            Console.WriteLine($"CHEQUE DIF: {(chqOk ? "✅" : "❌")} (esperado: 0.00, obtenido: {result.ChequeDiferido:N2})");
            
            if (osmOk && muniOk && efectivoOk && tcOk && tdOk && chqOk)
            {
                Console.WriteLine("\n🎉 ÉXITO TOTAL: Todos los valores son correctos!");
            }
            else
            {
                Console.WriteLine("\n⚠️ ALGUNOS VALORES NO COINCIDEN");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
        }
        
        Console.WriteLine("\nPresiona cualquier tecla para salir...");
        Console.ReadKey();
    }
}