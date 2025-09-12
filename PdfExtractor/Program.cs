using System;
using System.Threading.Tasks;
using System.Windows;

namespace PdfExtractor
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            try
            {
                // Si se pasa argumento --test, ejecutar pruebas automáticas
                var args = Environment.GetCommandLineArgs();
                if (args.Length > 1 && args[1] == "--test")
                {
                    RunTestsSync();
                    return;
                }
                
                // Sino, ejecutar la aplicación WPF normal
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in Main: {ex}");
                throw;
            }
        }
        
        private static void RunTestsSync()
        {
            try
            {
                Task.Run(async () => await TestExtraction.RunTests()).Wait();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error running tests: {ex.Message}");
            }
        }
    }
}