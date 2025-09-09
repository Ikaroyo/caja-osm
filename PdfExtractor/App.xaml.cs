using System;
using System.Windows;

namespace PdfExtractor
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Configurar para compatibilidad con Java applets
            ConfigureJavaSupport();
            
            // Manejar excepciones no controladas
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            
            base.OnStartup(e);
        }

        private void ConfigureJavaSupport()
        {
            try
            {
                // Configuración adicional para soportar Java en WebBrowser
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var mainModule = process.MainModule;
                if (mainModule?.FileName != null)
                {
                    var appName = System.IO.Path.GetFileName(mainModule.FileName);
                    
                    // Habilitar controles ActiveX y plugins
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_OBJECT_CACHING", 
                        appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        
                    Microsoft.Win32.Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_GPU_RENDERING", 
                        appName, 1, Microsoft.Win32.RegistryValueKind.DWord);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring Java support: {ex.Message}");
            }
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"UNHANDLED EXCEPTION: {e.ExceptionObject}");
            
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Error no controlado: {ex.Message}\n\nTipo: {ex.GetType().Name}\n\nStack: {ex.StackTrace}", 
                    "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"DISPATCHER UNHANDLED EXCEPTION: {e.Exception}");
            
            MessageBox.Show($"Error del dispatcher: {e.Exception.Message}\n\nTipo: {e.Exception.GetType().Name}\n\nStack: {e.Exception.StackTrace}", 
                "Error del Dispatcher", MessageBoxButton.OK, MessageBoxImage.Error);
            
            e.Handled = true; // Evita que la aplicación se cierre
        }
    }
}
