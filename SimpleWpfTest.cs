using System;
using System.Windows;
using System.Threading.Tasks;
using PdfExtractor.Services;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("=== SIMPLE WPF TEST ===");
            Console.WriteLine("Probando si hay problemas con el Dispatcher...");
            
            // Crear una aplicación WPF simple
            var app = new Application();
            
            Console.WriteLine("Application creada exitosamente");
            
            // Probar la funcionalidad de PDF extraction
            Console.WriteLine("Probando extracción de PDF...");
            
            var extractor = new PdfDataExtractor();
            var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
            
            if (System.IO.File.Exists(pdfPath))
            {
                var result = extractor.ExtractFromFile(pdfPath);
                Console.WriteLine($"Extracción exitosa:");
                Console.WriteLine($"  OSM: {result.TotalOSM:N2}");
                Console.WriteLine($"  MUNI: {result.TotalMunicipalidad:N2}");
                Console.WriteLine($"  Lote: {result.Lote}");
                Console.WriteLine($"  Usuario: {result.Usuario}");
                
                // Verificar valores esperados
                bool osmOk = Math.Abs(result.TotalOSM - 1478575.98m) < 0.1m;
                bool muniOk = Math.Abs(result.TotalMunicipalidad - 33016.10m) < 0.1m;
                
                Console.WriteLine($"\n=== VALIDACIÓN ===");
                Console.WriteLine($"OSM: {(osmOk ? "✅" : "❌")} (esperado: 1,478,575.98)");
                Console.WriteLine($"MUNI: {(muniOk ? "✅" : "❌")} (esperado: 33,016.10)");
                
                if (osmOk && muniOk)
                {
                    Console.WriteLine("🎉 ¡EXTRACCIÓN PERFECTA!");
                }
                else
                {
                    Console.WriteLine("❌ Valores no coinciden");
                }
            }
            else
            {
                Console.WriteLine($"Archivo PDF no encontrado: {pdfPath}");
            }
            
            Console.WriteLine("\n=== PRUEBA DE DISPATCHER ===");
            
            // Probar dispatcher operations
            var dispatcher = app.Dispatcher;
            bool dispatcherWorking = false;
            
            dispatcher.Invoke(() => {
                Console.WriteLine("✅ Dispatcher.Invoke funcionando");
                dispatcherWorking = true;
            });
            
            if (dispatcherWorking)
            {
                Console.WriteLine("✅ Dispatcher funciona correctamente");
            }
            else
            {
                Console.WriteLine("❌ Problema con Dispatcher");
            }
            
            Console.WriteLine("\n=== RESULTADO ===");
            Console.WriteLine("Si llegaste hasta aquí, no hay problemas con el Dispatcher básico.");
            Console.WriteLine("El problema puede estar en el MainWindow o en los controles específicos.");
            
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ ERROR: {ex.Message}");
            Console.WriteLine($"Tipo: {ex.GetType().Name}");
            Console.WriteLine($"Stack: {ex.StackTrace}");
        }
        
        Console.WriteLine("\nPresiona cualquier tecla para continuar...");
        Console.ReadKey();
    }
}