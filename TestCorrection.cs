using System;
using System.IO;
using PdfExtractor.Services;

namespace TestPdfCorrection
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

            Console.WriteLine("=== PRUEBA DE CORRECCIÓN DE EXTRACCIÓN ===");
            Console.WriteLine($"Archivo: {Path.GetFileName(pdfPath)}");
            Console.WriteLine();

            try
            {
                var extractor = new PdfDataExtractor();
                var resultado = extractor.ExtractPdfData(pdfPath);

                Console.WriteLine("RESULTADOS EXTRAÍDOS:");
                Console.WriteLine($"Fecha: {resultado.Fecha}");
                Console.WriteLine($"Lote: {resultado.Lote}");
                Console.WriteLine($"Usuario: {resultado.Usuario}");
                Console.WriteLine($"Total OSM: {resultado.TotalOSM:N2}");
                Console.WriteLine($"Total MUNI: {resultado.TotalMunicipalidad:N2}");
                Console.WriteLine($"Total General: {resultado.TotalGeneral:N2}");
                Console.WriteLine();

                Console.WriteLine("VALORES ESPERADOS:");
                Console.WriteLine("Total OSM: 33,016.10");
                Console.WriteLine("Total MUNI: 1,478,575.98");
                Console.WriteLine();

                // Verificar si los valores son correctos
                bool osmCorrecto = Math.Abs(resultado.TotalOSM - 33016.10m) < 1;
                bool muniCorrecto = Math.Abs(resultado.TotalMunicipalidad - 1478575.98m) < 1;

                Console.WriteLine("VERIFICACIÓN:");
                Console.WriteLine($"OSM correcto: {(osmCorrecto ? "✓ SÍ" : "✗ NO")}");
                Console.WriteLine($"MUNI correcto: {(muniCorrecto ? "✓ SÍ" : "✗ NO")}");
                Console.WriteLine($"Extracción exitosa: {(osmCorrecto && muniCorrecto ? "✓ SÍ" : "✗ NO")}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            Console.WriteLine("\nPresiona cualquier tecla para cerrar...");
            Console.ReadKey();
        }
    }
}