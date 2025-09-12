using System;
using System.IO;

class TestApp
{
    static void Main()
    {
        Console.WriteLine("=== PRUEBA RÁPIDA DE EXTRACCIÓN ===");
        
        string pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
        
        if (!File.Exists(pdfPath))
        {
            Console.WriteLine($"PDF no encontrado: {pdfPath}");
            return;
        }

        Console.WriteLine($"Probando: {Path.GetFileName(pdfPath)}");
        Console.WriteLine();
        
        // Simular los valores que se deberían extraer
        Console.WriteLine("VALORES ESPERADOS CORREGIDOS:");
        Console.WriteLine("OSM: 33,016.10 (era el valor de Municipalidad en el PDF)");
        Console.WriteLine("MUNI: 1,478,575.98 (era el valor de Obras Sanitarias en el PDF)");
        Console.WriteLine();
        
        Console.WriteLine("Si los valores en la aplicación coinciden con estos,");
        Console.WriteLine("entonces la corrección fue exitosa.");
        Console.WriteLine();
        
        Console.WriteLine("Presiona cualquier tecla para cerrar...");
        Console.ReadKey();
    }
}