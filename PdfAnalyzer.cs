using System;
using System.IO;
using PdfExtractor.Services;

// Programa de consola simple para probar la extracción
class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== ANÁLISIS DE PATRONES DE EXTRACCIÓN PDF ===");
        Console.WriteLine($"Fecha: {DateTime.Now}");
        Console.WriteLine();

        var testCases = new[]
        {
            new { File = "getjobid484197.pdf", Lote = "403279116", OSM = 1478575.98m, MUNI = 33016.1m, Usuario = "CMAGALLANES" },
            new { File = "getjobid485764.pdf", Lote = "403279119", OSM = 3243111.06m, MUNI = 234994.47m, Usuario = "FGONZALEZ" },
            new { File = "getjobid485763.pdf", Lote = "403279120", OSM = 5159825.54m, MUNI = 223836.74m, Usuario = "SCRESPO" },
            new { File = "getjobid485773.pdf", Lote = "403279121", OSM = 6154845.11m, MUNI = 356596.81m, Usuario = "FGONZALEZ" }
        };

        foreach (var testCase in testCases)
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), testCase.File);
            Console.WriteLine($"Analizando: {testCase.File}");
            Console.WriteLine($"Esperado - Lote: {testCase.Lote}, OSM: {testCase.OSM:F2}, MUNI: {testCase.MUNI:F2}, Usuario: {testCase.Usuario}");

            if (File.Exists(filePath))
            {
                try
                {
                    using var extractor = new PdfDataExtractor();
                    var data = extractor.ExtractFromFile(filePath);

                    Console.WriteLine($"Extraído - Lote: '{data.Lote}', OSM: {data.TotalOSM:F2}, MUNI: {data.TotalMunicipalidad:F2}, Usuario: '{data.Usuario}'");
                    
                    // Análisis de precisión
                    var loteMatch = data.Lote == testCase.Lote ? "✓" : "✗";
                    var osmDiff = Math.Abs(data.TotalOSM - testCase.OSM);
                    var osmMatch = osmDiff < 1 ? "✓" : "✗";
                    var muniDiff = Math.Abs(data.TotalMunicipalidad - testCase.MUNI);
                    var muniMatch = muniDiff < 1 ? "✓" : "✗";
                    var usuarioMatch = data.Usuario.Equals(testCase.Usuario, StringComparison.OrdinalIgnoreCase) ? "✓" : "✗";

                    Console.WriteLine($"Resultados - Lote: {loteMatch}, OSM: {osmMatch} (diff: {osmDiff:F2}), MUNI: {muniMatch} (diff: {muniDiff:F2}), Usuario: {usuarioMatch}");
                    
                    if (muniDiff >= 1)
                    {
                        Console.WriteLine($"⚠️  MUNI INCORRECTO - Diferencia: {muniDiff:F2}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERROR: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"ARCHIVO NO ENCONTRADO: {filePath}");
            }
            
            Console.WriteLine();
        }

        Console.WriteLine("Análisis completado. Presiona cualquier tecla para continuar...");
        Console.ReadKey();
    }
}