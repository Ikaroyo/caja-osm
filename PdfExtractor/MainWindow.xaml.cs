using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfExtractor.Services;
using PdfExtractor.Windows;
using PdfExtractor.Models;

namespace PdfExtractor
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private const string BASE_URL = "http://192.168.100.80:7778/reports/rwservlet/getjobid{0}?SERVER=rep_vmo_bck";
        private const string SIGEMI_URL = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo";
        private const string NOTES_FILE = "quick_notes.txt";
        private string currentPdfContent = "";
        private byte[] currentPdfData;

        // Variables para la calculadora
        private string currentInput = "0";
        private string operation = "";
        private decimal firstOperand = 0;
        private bool isNewEntry = true;
        private StringBuilder calculatorHistory = new StringBuilder();
        
        // Variables para el navegador web

        // Debug logging
        private void LogDebug(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}{Environment.NewLine}";
                
                // Log to Debug output
                System.Diagnostics.Debug.WriteLine(logEntry.Trim());
                
                // Log to visual debug window if available
                if (txtDebugLog != null)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        txtDebugLog.AppendText(logEntry);
                        if (DebugScrollViewer != null)
                        {
                            DebugScrollViewer.ScrollToEnd();
                        }
                    }));
                }
            }
            catch
            {
                // Ignore errors in logging
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromMinutes(5);
            
            // Debug: Mostrar información de inicio
            LogDebug("=== INICIO DE APLICACIÓN ===");
            LogDebug($"Fecha: {DateTime.Now}");
            LogDebug($"URL SIGEMI: {SIGEMI_URL}");
            
            // Inicializar WebBrowser
            InitializeWebBrowser();
            
            // Cargar información del sistema
            LoadSystemInfo();
            
            // Cargar notas rápidas guardadas
            LoadQuickNotes();
        }

        private void InitializeWebBrowser()
        {
            try
            {
                LogDebug("=== INICIALIZANDO WEBBROWSER ===");
                
                // Verificar que el WebBrowser existe
                if (WebBrowser == null)
                {
                    LogDebug("ERROR: WebBrowser es null!");
                    MessageBox.Show("Error: WebBrowser no está inicializado", "Error Crítico", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                LogDebug("WebBrowser encontrado correctamente");
                
                // Usar exactamente la misma configuración del proyecto que funciona
                WebBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                WebBrowser.Navigated += WebBrowser_Navigated;
                WebBrowser.NewWindow += WebBrowser_NewWindow;
                WebBrowser.Navigating += WebBrowser_Navigating;
                WebBrowser.ScriptErrorsSuppressed = false; // MOSTRAR errores para debugging
                
                // Agregar manejo de errores para debugging
                WebBrowser.DocumentCompleted += (s, e) => {
                    // Verificar applets después de que el documento esté completo
                    CheckForJavaApplets();
                };
                
                LogDebug("Eventos del WebBrowser configurados - errores de script visibles");
                
                // Verificar estado inicial
                LogDebug($"WebBrowser ReadyState: {WebBrowser.ReadyState}");
                LogDebug($"WebBrowser Version: {WebBrowser.Version}");
                
                // Navegar a la URL inicial con delay
                LogDebug($"Intentando navegar a: {SIGEMI_URL}");
                
                // Usar Dispatcher para asegurar que el WebBrowser esté completamente inicializado
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        NavigateToUrl(SIGEMI_URL);
                    }
                    catch (Exception navEx)
                    {
                        LogDebug($"Error en navegación inicial: {navEx}");
                        MessageBox.Show($"Error navegando a URL inicial: {navEx.Message}", "Error de Navegación", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
                
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR CRÍTICO en InitializeWebBrowser: {ex}");
                MessageBox.Show($"Error inicializando navegador: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebBrowser_Navigated(object sender, System.Windows.Forms.WebBrowserNavigatedEventArgs e)
        {
            try
            {
                LogDebug($"=== NAVEGADO ===");
                LogDebug($"URL navegada: {e.Url}");
                
                if (e.Url != null)
                {
                    LogDebug($"URL actualizada en evento Navigated: {e.Url}");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en WebBrowser_Navigated: {ex.Message}");
            }
        }

        private void WebBrowser_NewWindow(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                // Permitir ventanas emergentes para Java applets
                LogDebug("Nueva ventana detectada - permitiendo para Java applets");
                // No cancelar - permitir que se abra la ventana
            }
            catch (Exception ex)
            {
                LogDebug($"Error en WebBrowser_NewWindow: {ex.Message}");
            }
        }

        private void NavigateToUrl(string url)
        {
            try
            {
                LogDebug($"=== NAVEGANDO A URL ===");
                LogDebug($"URL recibida: {url}");
                
                if (string.IsNullOrWhiteSpace(url))
                {
                    LogDebug("ERROR: URL es null o vacía");
                    return;
                }

                // Asegurar que la URL tenga protocolo
                if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                {
                    url = "http://" + url;
                    LogDebug($"URL corregida con protocolo: {url}");
                }

                // Verificar WebBrowser
                if (WebBrowser == null)
                {
                    LogDebug("ERROR: WebBrowser es null en NavigateToUrl");
                    MessageBox.Show("Error: WebBrowser no disponible", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                LogDebug($"WebBrowser ReadyState antes de navegar: {WebBrowser.ReadyState}");
                
                LogDebug("Navegando a URL especificada");
                
                // Navegar
                LogDebug("Llamando a WebBrowser.Navigate()...");
                WebBrowser.Navigate(url);
                LogDebug("WebBrowser.Navigate() ejecutado");
                
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR en NavigateToUrl: {ex}");
                MessageBox.Show($"Error navegando a {url}: {ex.Message}", "Error de Navegación", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void NavigateButton_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl("http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo");
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("=== REFRESH BUTTON CLICKED ===");
                System.Diagnostics.Debug.WriteLine($"WebBrowser estado: {WebBrowser?.ReadyState}");
                System.Diagnostics.Debug.WriteLine($"URL actual: {WebBrowser?.Url}");
                
                WebBrowser.Refresh();
                System.Diagnostics.Debug.WriteLine("WebBrowser.Refresh() ejecutado");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en RefreshButton_Click: {ex}");
                MessageBox.Show($"Error actualizando página: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método removido: UrlTextBox_KeyDown ya no es necesario

        private async void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== TEST DE CONECTIVIDAD ===");
                var testUrl = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo";
                
                if (string.IsNullOrEmpty(testUrl))
                {
                    testUrl = SIGEMI_URL;
                }
                
                LogDebug($"Testeando URL: {testUrl}");
                
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(10);
                    var response = await client.GetAsync(testUrl);
                    
                    LogDebug($"Response Status: {response.StatusCode}");
                    LogDebug($"Response Headers: {response.Headers}");
                    
                    var content = await response.Content.ReadAsStringAsync();
                    LogDebug($"Content Length: {content.Length}");
                    LogDebug($"Content Type: {response.Content.Headers.ContentType}");
                    
                    if (content.Length > 0)
                    {
                        LogDebug($"Primeros 500 chars: {content.Substring(0, Math.Min(500, content.Length))}");
                    }
                    
                    MessageBox.Show($"✅ CONECTIVIDAD OK\n\nStatus: {response.StatusCode}\nTamaño: {content.Length} bytes\nTipo: {response.Content.Headers.ContentType}", 
                        "Test de Conectividad", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR en test de conectividad: {ex}");
                MessageBox.Show($"❌ ERROR DE CONECTIVIDAD\n\nError: {ex.Message}\n\nRevisa:\n- Conexión a internet\n- URL correcta\n- Servidor funcionando", 
                    "Error de Conectividad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                // Actualizar la URL en la barra de dirección
                if (WebBrowser.Url != null)
                {
                    LogDebug($"WebBrowser URL: {WebBrowser.Url}");
                }

                LogDebug($"Página cargada: {e.Url}");
                
                // Debugging específico para Java applets
                if (WebBrowser.Document != null)
                {
                    var doc = WebBrowser.Document;
                    LogDebug($"Document.Title: {doc.Title}");
                    LogDebug($"Document.Body.InnerText Length: {doc.Body?.InnerText?.Length ?? 0}");
                    
                    // Buscar applets en la página
                    var applets = doc.GetElementsByTagName("applet");
                    LogDebug($"Número de applets encontrados: {applets.Count}");
                    
                    for (int i = 0; i < applets.Count; i++)
                    {
                        var applet = applets[i];
                        LogDebug($"Applet {i}: {applet.GetAttribute("code")} - {applet.GetAttribute("archive")}");
                    }
                    
                    // Buscar objetos embebidos
                    var objects = doc.GetElementsByTagName("object");
                    LogDebug($"Número de objects encontrados: {objects.Count}");
                    
                    // Verificar si hay errores de script
                    LogDebug($"Document existe y está disponible");
                    
                    // Intentar forzar Java después de que la página esté completamente cargada
                    if (e.Url.ToString().Contains("frmservlet"))
                    {
                        LogDebug("Página de Oracle Forms detectada - aplicando configuraciones Java...");
                        
                        // Usar timer para aplicar configuraciones después de un momento
                        var timer = new System.Windows.Threading.DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(1);
                        timer.Tick += (s, args) => {
                            timer.Stop();
                            ForceEnableJavaInBrowser();
                            
                            // Verificar si funcionó
                            CheckJavaStatus();
                        };
                        timer.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en DocumentCompleted: {ex.Message}");
            }
        }

        private void CheckJavaStatus()
        {
            try
            {
                LogDebug("=== VERIFICANDO ESTADO DE JAVA POST-CONFIGURACIÓN ===");
                
                if (WebBrowser.Document != null)
                {
                    try
                    {
                        var result = WebBrowser.Document.InvokeScript("eval", new object[] { "navigator.javaEnabled()" });
                        LogDebug($"Java habilitado después de configuración: {result}");
                        
                        if (result != null && result.ToString().ToLower() == "true")
                        {
                            LogDebug("🎉 ¡JAVA HABILITADO EXITOSAMENTE!");
                            
                            // Verificar applets nuevamente
                            var applets = WebBrowser.Document.GetElementsByTagName("applet");
                            var objects = WebBrowser.Document.GetElementsByTagName("object");
                            LogDebug($"Applets después de habilitar Java: {applets.Count}");
                            LogDebug($"Objects después de habilitar Java: {objects.Count}");
                        }
                        else
                        {
                            LogDebug("❌ Java aún no está habilitado - se necesita configuración adicional");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error verificando estado Java: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en CheckJavaStatus: {ex.Message}");
            }
        }

        private void BtnForceJava_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== BOTÓN FORCE JAVA PRESIONADO ===");
                ForceEnableJavaInBrowser();
                
                // Verificar inmediatamente después
                var timer = new System.Windows.Threading.DispatcherTimer();
                timer.Interval = TimeSpan.FromSeconds(2);
                timer.Tick += (s, args) => {
                    timer.Stop();
                    CheckJavaStatus();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                LogDebug($"Error en BtnForceJava_Click: {ex.Message}");
            }
        }

        private void BtnCheckJavaStatus_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== BOTÓN CHECK JAVA STATUS PRESIONADO ===");
                CheckJavaStatus();
            }
            catch (Exception ex)
            {
                LogDebug($"Error en BtnCheckJavaStatus_Click: {ex.Message}");
            }
        }

        private void BtnFixPermissions_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== REPARANDO PERMISOS DE SCRIPT Y JAVA ===");
                
                // Aplicar configuraciones avanzadas
                ConfigureAdvancedJavaSettings();
                
                // Configuraciones específicas para errores de script
                var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                
                // Deshabilitar protección de scripts
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ZONE_ELEVATION"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug("Deshabilitada elevación de zona");
                    }
                }
                
                // Permitir scripting cross-frame
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_WINDOW_RESTRICTIONS"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug("Deshabilitadas restricciones de ventana");
                    }
                }
                
                // Configurar políticas de seguridad para Oracle Forms específicamente
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3"))
                {
                    if (key != null)
                    {
                        // Permitir todo para Oracle Forms
                        key.SetValue("1A00", 0, Microsoft.Win32.RegistryValueKind.DWord); // Credenciales de usuario
                        key.SetValue("1C00", 0, Microsoft.Win32.RegistryValueKind.DWord); // Descargas automáticas
                        key.SetValue("1E05", 0, Microsoft.Win32.RegistryValueKind.DWord); // Software no firmado
                        key.SetValue("2101", 0, Microsoft.Win32.RegistryValueKind.DWord); // Descargas de fuentes
                        LogDebug("Configuradas políticas específicas para Oracle Forms");
                    }
                }
                
                LogDebug("✅ Reparación de permisos completada");
                MessageBox.Show("✅ Permisos reparados\n\nReinicia la navegación para aplicar cambios.", 
                    "Permisos Reparados", MessageBoxButton.OK, MessageBoxImage.Information);
                    
            }
            catch (Exception ex)
            {
                LogDebug($"Error reparando permisos: {ex.Message}");
                MessageBox.Show($"Error reparando permisos: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLaunchJNLP_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== BOTÓN LAUNCH JNLP PRESIONADO ===");
                
                // Opción 1: Intentar encontrar y lanzar JNLP
                var jnlpUrl = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo&format=jnlp";
                LogDebug($"Intentando lanzar JNLP: {jnlpUrl}");
                
                try
                {
                    var launched = JavaCompatibilityHelper.LaunchJNLP(jnlpUrl);
                    if (launched)
                    {
                        LogDebug("✅ JNLP lanzado exitosamente");
                        MessageBox.Show("✅ Oracle Forms lanzado via Java Web Start\n\nLa aplicación debería abrirse en una ventana separada.", 
                            "JNLP Lanzado", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        LogDebug("❌ JNLP no pudo lanzarse");
                        TryAlternativeMethods();
                    }
                }
                catch (Exception jnlpEx)
                {
                    LogDebug($"Error lanzando JNLP: {jnlpEx.Message}");
                    TryAlternativeMethods();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en BtnLaunchJNLP_Click: {ex.Message}");
            }
        }

        private void TryAlternativeMethods()
        {
            try
            {
                LogDebug("=== PROBANDO MÉTODOS ALTERNATIVOS ===");
                
                var result = MessageBox.Show(
                    "El WebBrowser interno no puede ejecutar Java applets.\n\n" +
                    "¿Quieres abrir SIGEMI en tu navegador por defecto?\n" +
                    "(Chrome, Firefox, Edge pueden tener mejor soporte para Java)",
                    "Abrir en Navegador Externo", 
                    MessageBoxButton.YesNo, 
                    MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    LogDebug("Usuario eligió abrir en navegador externo");
                    var url = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo";
                    
                    LogDebug($"Abriendo URL en navegador externo: {url}");
                    
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true
                    };
                    System.Diagnostics.Process.Start(psi);
                    
                    LogDebug("✅ URL abierta en navegador externo");
                    MessageBox.Show("✅ SIGEMI abierto en tu navegador por defecto\n\n" +
                        "Si Java está habilitado en ese navegador, la aplicación debería funcionar correctamente.",
                        "Navegador Externo", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en métodos alternativos: {ex.Message}");
                MessageBox.Show($"Error abriendo navegador externo: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSigemiAuto_Click(object sender, RoutedEventArgs e)
        {
            LogDebug("=== 🎯 SIGEMI AUTO: ABRIENDO EN NAVEGADOR EXTERNO ===");
            try
            {
                string sigemiUrl = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo";
                
                LogDebug($"📱 Abriendo SIGEMI directamente en navegador predeterminado");
                LogDebug($"🔗 URL: {sigemiUrl}");
                
                // Abrir en navegador predeterminado del sistema
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sigemiUrl,
                    UseShellExecute = true
                };
                System.Diagnostics.Process.Start(psi);
                
                LogDebug("✅ SIGEMI abierto en navegador externo");
                LogDebug("💡 VENTAJAS del navegador externo:");
                LogDebug("   • Mejor compatibilidad con Java applets antiguos");
                LogDebug("   • Soporte para múltiples versiones de Java");
                LogDebug("   • Configuraciones de seguridad más flexibles");
                LogDebug("   • Plugin Java nativo del navegador");
                
                // Mostrar mensaje informativo sin bloquear
                MessageBox.Show(
                    "🎯 ¡SIGEMI abierto en su navegador predeterminado!\n\n" +
                    "✅ VENTAJAS:\n" +
                    "• Mejor compatibilidad con Java applets Oracle Forms\n" +
                    "• Soporte nativo para Java 1.4.2 y versiones superiores\n" +
                    "• Sin restricciones del WebBrowser .NET\n" +
                    "• Configuraciones de seguridad del navegador\n\n" +
                    "💡 Si no funciona inmediatamente:\n" +
                    "• Asegúrese de que Java esté habilitado en su navegador\n" +
                    "• Verifique la configuración de seguridad Java\n" +
                    "• Pruebe con diferentes navegadores (Chrome, Firefox, Edge)",
                    "🎯 SIGEMI Auto - Navegador Externo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                
                // URL de referencia actualizada
            }
            catch (Exception ex)
            {
                LogDebug($"❌ Error abriendo SIGEMI en navegador externo: {ex.Message}");
                MessageBox.Show(
                    $"❌ Error al abrir SIGEMI en navegador externo:\n\n{ex.Message}\n\n" +
                    "🔧 SOLUCIÓN MANUAL:\n" +
                    "Copie y pegue esta URL en su navegador:\n" +
                    "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo",
                    "Error - SIGEMI Auto",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void WebBrowser_Navigating(object sender, System.Windows.Forms.WebBrowserNavigatingEventArgs e)
        {
            try
            {
                LogDebug($"Navegando a: {e.Url}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error en Navigating: {ex.Message}");
            }
        }

        private void LoadSystemInfo()
        {
            try
            {
                var info = new StringBuilder();
                info.AppendLine("=== INFORMACIÓN DEL SISTEMA ===");
                info.AppendLine($"Sistema Operativo: {Environment.OSVersion}");
                info.AppendLine($"Versión .NET: {Environment.Version}");
                info.AppendLine($"Máquina: {Environment.MachineName}");
                info.AppendLine($"Usuario: {Environment.UserName}");
                info.AppendLine($"Directorio de trabajo: {Environment.CurrentDirectory}");
                info.AppendLine();
                info.AppendLine("=== CONFIGURACIÓN JAVA ===");
                
                // Verificar Java
                var javaInfo = JavaCompatibilityHelper.GetJavaInfo();
                info.AppendLine(javaInfo);
                
                txtSystemInfo.Text = info.ToString();
            }
            catch (Exception ex)
            {
                txtSystemInfo.Text = $"Error cargando información del sistema: {ex.Message}";
            }
        }

        private async void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            string input = txtInput.Text.Trim();
            if (string.IsNullOrEmpty(input))
            {
                MessageBox.Show("Por favor ingrese un link o número.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                btnProcess.IsEnabled = false;
                btnExtractData.IsEnabled = false;
                txtStatus.Text = "Procesando...";
                txtContent.Text = "";

                string url = BuildUrl(input);
                txtStatus.Text = $"Descargando PDF desde: {url}";

                byte[] pdfData = await DownloadPdfAsync(url);
                currentPdfData = pdfData; // Guardar datos para imprimir
                txtStatus.Text = "Extrayendo contenido del PDF...";

                string extractedText = ExtractTextFromPdf(pdfData);
                currentPdfContent = extractedText;
                txtContent.Text = extractedText;

                txtStatus.Text = "Extrayendo datos estructurados...";
                
                // Llamar automáticamente a la extracción de datos
                await ExtractDataAutomatically();

                txtStatus.Text = $"Completado. Datos extraídos y procesados automáticamente.";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error al procesar";
            }
            finally
            {
                btnProcess.IsEnabled = true;
            }
        }

        private async Task ExtractDataAutomatically()
        {
            if (string.IsNullOrEmpty(currentPdfContent))
            {
                return;
            }

            try
            {
                txtStatus.Text = "Verificando configuración...";
                
                AppConfig config;
                try
                {
                    config = AppConfig.Load();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (string.IsNullOrEmpty(config.SaveLocation))
                {
                    MessageBox.Show("Debe configurar una ubicación de guardado primero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    var configWindow = new ConfigWindow();
                    configWindow.Owner = this;
                    if (configWindow.ShowDialog() != true)
                        return;
                    config = AppConfig.Load();
                }

                txtStatus.Text = "Extrayendo datos estructurados...";
                
                LoteData extractedData;
                try
                {
                    extractedData = PdfParser.ExtractData(currentPdfContent);
                    DisplayExtractedData(extractedData);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error extrayendo datos del PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error en extracción de datos";
                    return;
                }
                
                if (extractedData == null)
                {
                    MessageBox.Show("Error al extraer datos del PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error en extracción de datos";
                    return;
                }

                txtStatus.Text = "Abriendo ventana de revisión...";
                
                DataReviewWindow reviewWindow;
                try
                {
                    reviewWindow = new DataReviewWindow(extractedData);
                    reviewWindow.Owner = this;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creando ventana de revisión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error creando ventana";
                    return;
                }
                
                bool? result = reviewWindow.ShowDialog();
                
                if (result == true && reviewWindow.Saved)
                {
                    try
                    {
                        DataService.AddLote(extractedData, config.SaveLocation);
                        txtStatus.Text = "Datos guardados correctamente.";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error guardando datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtStatus.Text = "Error guardando datos";
                    }
                }
                else
                {
                    txtStatus.Text = "Extracción cancelada.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error general: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error en proceso automático";
            }
        }

        private void BtnExtractData_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("=== STARTING BtnExtractData_Click ===");
            
            if (string.IsNullOrEmpty(currentPdfContent))
            {
                MessageBox.Show("Primero debe procesar un PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                System.Diagnostics.Debug.WriteLine("Step 1: Checking configuration...");
                txtStatus.Text = "Verificando configuración...";
                
                AppConfig config;
                try
                {
                    config = AppConfig.Load();
                    System.Diagnostics.Debug.WriteLine($"Config loaded: SaveLocation = '{config.SaveLocation}'");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading config: {ex}");
                    MessageBox.Show($"Error cargando configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                if (string.IsNullOrEmpty(config.SaveLocation))
                {
                    System.Diagnostics.Debug.WriteLine("No save location configured, opening config window...");
                    MessageBox.Show("Debe configurar una ubicación de guardado primero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    
                    try
                    {
                        var configWindow = new ConfigWindow();
                        configWindow.Owner = this;
                        if (configWindow.ShowDialog() != true)
                            return;
                        config = AppConfig.Load();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error opening config window: {ex}");
                        MessageBox.Show($"Error abriendo ventana de configuración: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                System.Diagnostics.Debug.WriteLine("Step 2: Starting data extraction...");
                txtStatus.Text = "Extrayendo datos estructurados...";
                
                LoteData extractedData;
                try
                {
                    System.Diagnostics.Debug.WriteLine($"PDF Content length: {currentPdfContent.Length}");
                    extractedData = PdfParser.ExtractData(currentPdfContent);
                    System.Diagnostics.Debug.WriteLine("Data extraction completed successfully");
                    
                    // Mostrar datos extraídos en el panel
                    DisplayExtractedData(extractedData);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in PdfParser.ExtractData: {ex}");
                    MessageBox.Show($"Error extrayendo datos del PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error en extracción de datos";
                    return;
                }
                
                if (extractedData == null)
                {
                    System.Diagnostics.Debug.WriteLine("extractedData is null");
                    MessageBox.Show("Error al extraer datos del PDF.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error en extracción de datos";
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Step 3: Creating review window...");
                txtStatus.Text = "Abriendo ventana de revisión...";
                
                DataReviewWindow reviewWindow;
                try
                {
                    reviewWindow = new DataReviewWindow(extractedData);
                    reviewWindow.Owner = this;
                    System.Diagnostics.Debug.WriteLine("Review window created successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error creating review window: {ex}");
                    MessageBox.Show($"Error creando ventana de revisión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error creando ventana";
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine("Step 4: Showing review window...");
                txtStatus.Text = "Mostrando ventana de revisión...";
                
                bool? result;
                try
                {
                    result = reviewWindow.ShowDialog();
                    System.Diagnostics.Debug.WriteLine($"Review window closed with result: {result}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error showing review window: {ex}");
                    MessageBox.Show($"Error mostrando ventana de revisión: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error mostrando ventana";
                    return;
                }
                
                if (result == true && reviewWindow.Saved)
                {
                    System.Diagnostics.Debug.WriteLine("Step 5: Saving data...");
                    try
                    {
                        DataService.AddLote(extractedData, config.SaveLocation);
                        txtStatus.Text = "Datos guardados correctamente.";
                        System.Diagnostics.Debug.WriteLine("Data saved successfully");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving data: {ex}");
                        MessageBox.Show($"Error guardando datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtStatus.Text = "Error guardando datos";
                    }
                }
                else
                {
                    txtStatus.Text = "Extracción cancelada.";
                    System.Diagnostics.Debug.WriteLine("Extraction cancelled by user");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FATAL ERROR in BtnExtractData_Click: {ex}");
                try
                {
                    MessageBox.Show($"Error fatal: {ex.Message}\n\nTipo: {ex.GetType().Name}\n\nStack: {ex.StackTrace}", "Error Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch
                {
                    // Si hasta el MessageBox falla, al menos loguear
                    System.Diagnostics.Debug.WriteLine("Even MessageBox failed!");
                }
                txtStatus.Text = "Error fatal en extracción";
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine("=== FINISHED BtnExtractData_Click ===");
            }
        }

        private void DisplayExtractedData(LoteData data)
        {
            try
            {
                extractedDataPanel.Children.Clear();
                
                // Crear elementos visuales para mostrar los datos extraídos
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                
                int row = 0;
                
                AddDataRow(grid, "Lote:", data.Lote, row++);
                AddDataRow(grid, "Fecha:", data.FechaString, row++);
                AddDataRow(grid, "Usuario:", data.Usuario, row++);
                AddDataRow(grid, "", "", row++); // Separador
                AddDataRow(grid, "Efectivo:", $"${data.Efectivo:N2}", row++);
                AddDataRow(grid, "T. Crédito:", $"${data.Credito:N2}", row++);
                AddDataRow(grid, "T. Débito:", $"${data.Debito:N2}", row++);
                AddDataRow(grid, "Cheque:", $"${data.Cheque:N2}", row++);
                AddDataRow(grid, "", "", row++); // Separador
                AddDataRow(grid, "Total OSM:", $"${data.OSM:N2}", row++);
                AddDataRow(grid, "Total MUNI:", $"${data.MUNI:N2}", row++);
                AddDataRow(grid, "TOTAL:", $"${(data.OSM + data.MUNI):N2}", row++, true);
                
                extractedDataPanel.Children.Add(grid);
            }
            catch (Exception ex)
            {
                var errorText = new TextBlock 
                { 
                    Text = $"Error mostrando datos: {ex.Message}", 
                    Foreground = System.Windows.Media.Brushes.Red 
                };
                extractedDataPanel.Children.Clear();
                extractedDataPanel.Children.Add(errorText);
            }
        }

        private void AddDataRow(Grid grid, string label, string value, int row, bool isBold = false)
        {
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var labelText = new TextBlock 
            { 
                Text = label, 
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 2, 5, 2)
            };
            Grid.SetRow(labelText, row);
            Grid.SetColumn(labelText, 0);
            grid.Children.Add(labelText);
            
            var valueText = new TextBlock 
            { 
                Text = value, 
                FontWeight = isBold ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(0, 2, 0, 2)
            };
            if (isBold) valueText.Foreground = System.Windows.Media.Brushes.DarkGreen;
            Grid.SetRow(valueText, row);
            Grid.SetColumn(valueText, 1);
            grid.Children.Add(valueText);
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            var configWindow = new ConfigWindow();
            configWindow.Owner = this;
            if (configWindow.ShowDialog() == true)
            {
                // Recargar información del sistema después de guardar
                LoadSystemInfo();
            }
        }

        private void BtnViewLotes_Click(object sender, RoutedEventArgs e)
        {
            var config = AppConfig.Load();
            if (string.IsNullOrEmpty(config.SaveLocation))
            {
                MessageBox.Show("Debe configurar una ubicación de guardado primero.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var listWindow = new LotesListWindow(config.SaveLocation);
            listWindow.Owner = this;
            listWindow.Show();
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            txtInput.Text = "";
            txtContent.Text = "";
            currentPdfContent = "";
            txtStatus.Text = "Listo";
            btnExtractData.IsEnabled = false;
            
            // Limpiar panel de datos extraídos
            extractedDataPanel.Children.Clear();
            var infoText = new TextBlock 
            { 
                Text = "Los datos extraídos aparecerán aquí...", 
                Foreground = System.Windows.Media.Brushes.Gray, 
                FontStyle = FontStyles.Italic 
            };
            extractedDataPanel.Children.Add(infoText);
        }

        private string BuildUrl(string input)
        {
            // Si ya es una URL completa, la devuelve tal como está
            if (input.StartsWith("http://") || input.StartsWith("https://"))
            {
                return input;
            }

            // Si es solo un número, construye la URL
            if (int.TryParse(input, out _))
            {
                return string.Format(BASE_URL, input);
            }

            // Si contiene 'getjobid' extrae el número
            if (input.Contains("getjobid"))
            {
                int startIndex = input.IndexOf("getjobid") + 8;
                int endIndex = input.IndexOf("?", startIndex);
                if (endIndex == -1) endIndex = input.Length;
                
                string jobId = input.Substring(startIndex, endIndex - startIndex);
                return string.Format(BASE_URL, jobId);
            }

            throw new ArgumentException("Formato de entrada no válido. Use una URL completa o solo el número del job.");
        }

        private async Task<byte[]> DownloadPdfAsync(string url)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Error al descargar el PDF: {ex.Message}");
            }
            catch (TaskCanceledException)
            {
                throw new Exception("Timeout al descargar el PDF. Verifique la conexión y la URL.");
            }
        }

        private string ExtractTextFromPdf(byte[] pdfData)
        {
            try
            {
                // Crear nuevo extractor cada vez para evitar disposed object
                using var extractor = new PdfDataExtractor();
                return extractor.ExtractTextFromPdf(pdfData);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al extraer texto del PDF: {ex.Message}");
            }
        }

        // Event handlers para herramientas
        private void BtnCheckJava_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = new StringBuilder();
                info.AppendLine("=== INFORMACIÓN DEL NAVEGADOR ===");
                info.AppendLine($"Versión del WebBrowser: {WebBrowser.Version}");
                info.AppendLine($"Estado del documento: {WebBrowser.ReadyState}");
                info.AppendLine($"URL actual: {WebBrowser.Url}");
                info.AppendLine($"Puede navegar hacia atrás: {WebBrowser.CanGoBack}");
                info.AppendLine($"Puede navegar hacia adelante: {WebBrowser.CanGoForward}");
                info.AppendLine();
                
                // Información básica de Java del sistema
                var javaInfo = JavaCompatibilityHelper.GetJavaInfo();
                info.AppendLine(javaInfo);
                
                MessageBox.Show(info.ToString(), "Información del Navegador y Java", MessageBoxButton.OK, MessageBoxImage.Information);
                LoadSystemInfo(); // Actualizar información del sistema
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error verificando información: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnOpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var config = AppConfig.Load();
                string folder = !string.IsNullOrEmpty(config.SaveLocation) ? config.SaveLocation : Environment.CurrentDirectory;
                
                if (Directory.Exists(folder))
                {
                    Process.Start("explorer.exe", folder);
                }
                else
                {
                    MessageBox.Show("La carpeta no existe", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo carpeta: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnRestartApp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro que desea reiniciar la aplicación?", 
                "Reiniciar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string appPath = Process.GetCurrentProcess().MainModule?.FileName ?? "";
                    Process.Start(appPath);
                    Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error reiniciando: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Eventos de la calculadora
        private void Calculator_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string buttonContent = button.Content.ToString() ?? "";
                ProcessCalculatorInput(buttonContent);
            }
        }

        private void ProcessCalculatorInput(string input)
        {
            try
            {
                string previousDisplay = txtCalculatorDisplay.Text;
                
                switch (input)
                {
                    case "C":
                        currentInput = "0";
                        operation = "";
                        firstOperand = 0;
                        isNewEntry = true;
                        calculatorHistory.AppendLine($"Limpiar todo");
                        break;
                        
                    case "CE":
                        currentInput = "0";
                        isNewEntry = true;
                        break;
                        
                    case "←":
                        if (currentInput.Length > 1)
                            currentInput = currentInput.Substring(0, currentInput.Length - 1);
                        else
                            currentInput = "0";
                        break;
                        
                    case "+":
                    case "-":
                    case "×":
                    case "÷":
                        if (!string.IsNullOrEmpty(operation) && !isNewEntry)
                        {
                            CalculateResult();
                        }
                        firstOperand = decimal.Parse(currentInput.Replace(",", "."), CultureInfo.InvariantCulture);
                        operation = input;
                        isNewEntry = true;
                        break;
                        
                    case "=":
                        if (!string.IsNullOrEmpty(operation))
                        {
                            decimal secondOperand = decimal.Parse(currentInput.Replace(",", "."), CultureInfo.InvariantCulture);
                            string calculation = $"{firstOperand} {operation} {secondOperand} = ";
                            CalculateResult();
                            calculation += currentInput;
                            calculatorHistory.AppendLine(calculation);
                            UpdateCalculatorHistory();
                        }
                        operation = "";
                        isNewEntry = true;
                        break;
                        
                    case ",":
                        if (!currentInput.Contains(","))
                            currentInput += ",";
                        isNewEntry = false;
                        break;
                        
                    default: // Números
                        if (isNewEntry)
                        {
                            currentInput = input;
                            isNewEntry = false;
                        }
                        else
                        {
                            if (currentInput == "0")
                                currentInput = input;
                            else
                                currentInput += input;
                        }
                        break;
                }
                
                txtCalculatorDisplay.Text = FormatCalculatorDisplay(currentInput);
            }
            catch (Exception ex)
            {
                txtCalculatorDisplay.Text = "Error";
                System.Diagnostics.Debug.WriteLine($"Calculator error: {ex.Message}");
            }
        }

        private void UpdateCalculatorHistory()
        {
            var historyLines = calculatorHistory.ToString().Split('\n');
            var recentHistory = historyLines.Reverse().Take(10).Reverse().Where(line => !string.IsNullOrWhiteSpace(line));
            txtCalculatorHistory.Text = string.Join(Environment.NewLine, recentHistory);
        }

        private void CalculateResult()
        {
            try
            {
                decimal secondOperand = decimal.Parse(currentInput.Replace(",", "."), CultureInfo.InvariantCulture);
                decimal result = 0;

                switch (operation)
                {
                    case "+":
                        result = firstOperand + secondOperand;
                        break;
                    case "-":
                        result = firstOperand - secondOperand;
                        break;
                    case "×":
                        result = firstOperand * secondOperand;
                        break;
                    case "÷":
                        if (secondOperand != 0)
                            result = firstOperand / secondOperand;
                        else
                        {
                            txtCalculatorDisplay.Text = "Error";
                            return;
                        }
                        break;
                }

                currentInput = result.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",");
                firstOperand = result;
            }
            catch
            {
                txtCalculatorDisplay.Text = "Error";
            }
        }

        private string FormatCalculatorDisplay(string value)
        {
            if (decimal.TryParse(value.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal number))
            {
                return number.ToString("N2", new CultureInfo("es-AR"));
            }
            return value;
        }

        // Métodos para debug
        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== BOTÓN DEBUG PRESIONADO ===");
                LogDebug($"WebBrowser.ReadyState: {WebBrowser?.ReadyState}");
                LogDebug($"WebBrowser.Url: {WebBrowser?.Url}");
                LogDebug($"WebBrowser.DocumentTitle: {WebBrowser?.DocumentTitle}");
                LogDebug($"WebBrowser.Version: {WebBrowser?.Version}");
                
                // Cambiar a la tab de debug
                mainTabControl.SelectedIndex = 3; // Debug tab
            }
            catch (Exception ex)
            {
                LogDebug($"Error en DebugButton_Click: {ex.Message}");
            }
        }

        private void BtnClearDebug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtDebugLog.Clear();
                LogDebug("=== LOG LIMPIADO ===");
            }
            catch (Exception ex)
            {
                LogDebug($"Error limpiando debug: {ex.Message}");
            }
        }

        private void BtnTestNav_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== TEST NAVEGACIÓN MANUAL ===");
                NavigateToUrl(SIGEMI_URL);
            }
            catch (Exception ex)
            {
                LogDebug($"Error en test navegación: {ex.Message}");
            }
        }

        private void BtnCheckBrowser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("=== VERIFICACIÓN DE BROWSER ===");
                
                if (WebBrowser == null)
                {
                    LogDebug("ERROR: WebBrowser es null");
                    return;
                }
                
                LogDebug($"ReadyState: {WebBrowser.ReadyState}");
                LogDebug($"Version: {WebBrowser.Version}");
                LogDebug($"URL actual: {WebBrowser.Url}");
                LogDebug($"Título del documento: {WebBrowser.DocumentTitle}");
                LogDebug($"CanGoBack: {WebBrowser.CanGoBack}");
                LogDebug($"CanGoForward: {WebBrowser.CanGoForward}");
                LogDebug($"IsWebBrowserContextMenuEnabled: {WebBrowser.IsWebBrowserContextMenuEnabled}");
                LogDebug($"ScriptErrorsSuppressed: {WebBrowser.ScriptErrorsSuppressed}");
                
                // Verificar registro de IE
                LogDebug("Verificando configuración de registro de IE...");
                JavaCompatibilityHelper.ConfigureIEForJava();
                JavaCompatibilityHelper.ConfigureWebBrowserForJava();
                LogDebug("Configuración de registro completada");
                
                // Verificar Java específicamente
                LogDebug("=== VERIFICACIÓN DE JAVA ===");
                LogDebug($"Java instalado: {JavaCompatibilityHelper.IsJavaInstalled()}");
                LogDebug($"Versión Java: {JavaCompatibilityHelper.GetJavaVersion()}");
                LogDebug($"IE disponible: {JavaCompatibilityHelper.IsInternetExplorerAvailable()}");
                
                // Habilitar temporalmente errores de script para debugging
                WebBrowser.ScriptErrorsSuppressed = false;
                LogDebug("ScriptErrorsSuppressed desactivado para debugging");
                
                // Configuración adicional para Java
                try
                {
                    LogDebug("=== CONFIGURACIÓN ADICIONAL JAVA ===");
                    
                    // Verificar si Java está habilitado en el control
                    if (WebBrowser.Document != null)
                    {
                        // Intentar habilitar Java via registry settings
                        ConfigureJavaForWebBrowser();
                        
                        // Forzar recarga para aplicar configuración
                        LogDebug("Forzando recarga para aplicar configuración Java...");
                        WebBrowser.Navigate("about:blank");
                        
                        // Usar timer para navegar de vuelta después de un momento
                        var timer = new System.Windows.Threading.DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(2);
                        timer.Tick += (s, args) => {
                            timer.Stop();
                            LogDebug("Navegando de vuelta a SIGEMI después de configurar Java...");
                            NavigateToUrl(SIGEMI_URL);
                        };
                        timer.Start();
                    }
                }
                catch (Exception javaEx)
                {
                    LogDebug($"Error en configuración Java adicional: {javaEx.Message}");
                }
                
            }
            catch (Exception ex)
            {
                LogDebug($"Error verificando browser: {ex.Message}");
            }
        }

        private void CheckForJavaApplets()
        {
            try
            {
                LogDebug("=== VERIFICANDO APPLETS JAVA ===");
                
                if (WebBrowser.Document == null)
                {
                    LogDebug("ERROR: Document es null");
                    return;
                }
                
                var doc = WebBrowser.Document;
                
                // Buscar tags de applet
                var applets = doc.GetElementsByTagName("applet");
                LogDebug($"Applets encontrados: {applets.Count}");
                
                if (applets.Count > 0)
                {
                    for (int i = 0; i < applets.Count; i++)
                    {
                        var applet = applets[i];
                        LogDebug($"Applet {i}:");
                        LogDebug($"  - Code: {applet.GetAttribute("code")}");
                        LogDebug($"  - Archive: {applet.GetAttribute("archive")}");
                        LogDebug($"  - Width: {applet.GetAttribute("width")}");
                        LogDebug($"  - Height: {applet.GetAttribute("height")}");
                        LogDebug($"  - CodeBase: {applet.GetAttribute("codebase")}");
                    }
                }
                
                // Buscar objetos embed
                var embeds = doc.GetElementsByTagName("embed");
                LogDebug($"Embeds encontrados: {embeds.Count}");
                
                // Buscar objetos
                var objects = doc.GetElementsByTagName("object");
                LogDebug($"Objects encontrados: {objects.Count}");
                
                // Verificar si Java está funcionando ejecutando un script
                try
                {
                    var scriptResult = WebBrowser.Document.InvokeScript("eval", new object[] { "navigator.javaEnabled()" });
                    LogDebug($"JavaScript navigator.javaEnabled(): {scriptResult}");
                }
                catch (Exception ex)
                {
                    LogDebug($"Error verificando Java via JavaScript: {ex.Message}");
                }
                
                // Obtener HTML completo para análisis
                var htmlContent = doc.Body?.OuterHtml ?? "";
                if (htmlContent.Contains("applet") || htmlContent.Contains("java"))
                {
                    LogDebug("HTML contiene referencias a Java/applets");
                    // Mostrar una porción del HTML relevante
                    var javaIndex = htmlContent.ToLower().IndexOf("applet");
                    if (javaIndex >= 0)
                    {
                        var start = Math.Max(0, javaIndex - 100);
                        var end = Math.Min(htmlContent.Length, javaIndex + 500);
                        var relevantHtml = htmlContent.Substring(start, end - start);
                        LogDebug($"HTML relevante: {relevantHtml}");
                    }
                }
                
            }
            catch (Exception ex)
            {
                LogDebug($"Error en CheckForJavaApplets: {ex.Message}");
            }
        }

        private void ConfigureJavaForWebBrowser()
        {
            try
            {
                LogDebug("=== CONFIGURANDO JAVA PARA WEBBROWSER ===");
                
                // Configurar registry para habilitar Java en WebBrowser Control
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                        key.SetValue(appName, 11001, Microsoft.Win32.RegistryValueKind.DWord); // IE11 mode
                        LogDebug($"Configurado FEATURE_BROWSER_EMULATION para {appName}: 11001");
                    }
                }
                
                // Habilitar Java específicamente
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_JSCRIPT_JRE"))
                {
                    if (key != null)
                    {
                        var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                        key.SetValue(appName, 1, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_ENABLE_JSCRIPT_JRE para {appName}: 1");
                    }
                }
                
                // Permitir applets locales
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_LOCALMACHINE_LOCKDOWN"))
                {
                    if (key != null)
                    {
                        var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                        key.SetValue(appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_LOCALMACHINE_LOCKDOWN para {appName}: 0");
                    }
                }
                
                // Configurar zonas de seguridad para permitir Java
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3"))
                {
                    if (key != null)
                    {
                        key.SetValue("1004", 0, Microsoft.Win32.RegistryValueKind.DWord); // Permitir Java applets
                        key.SetValue("1A04", 0, Microsoft.Win32.RegistryValueKind.DWord); // Permitir Java secure
                        key.SetValue("1400", 0, Microsoft.Win32.RegistryValueKind.DWord); // Permitir scripts activos
                        key.SetValue("1001", 0, Microsoft.Win32.RegistryValueKind.DWord); // ActiveX firmados
                        key.SetValue("1200", 0, Microsoft.Win32.RegistryValueKind.DWord); // ActiveX no firmados
                        key.SetValue("1208", 0, Microsoft.Win32.RegistryValueKind.DWord); // Permitir scripts de sitio web
                        LogDebug("Configurado zona de Internet para permitir Java applets y scripts");
                    }
                }
                
                LogDebug("Configuración de Java para WebBrowser completada");
            }
            catch (Exception ex)
            {
                LogDebug($"Error configurando Java para WebBrowser: {ex.Message}");
            }
        }

        private void ForceEnableJavaInBrowser()
        {
            try
            {
                LogDebug("=== FORZANDO HABILITACIÓN DE JAVA ===");
                
                if (WebBrowser.Document != null)
                {
                    // Método 1: Intentar ejecutar script para habilitar Java
                    try
                    {
                        WebBrowser.Document.InvokeScript("eval", new object[] { 
                            "if (window.navigator && window.navigator.javaEnabled) { window.navigator.javaEnabled = function() { return true; }; }" 
                        });
                        LogDebug("Script para habilitar Java ejecutado");
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error ejecutando script Java: {ex.Message}");
                    }
                    
                    // Método 2: Configurar headers HTTP para Java
                    try
                    {
                        var headers = WebBrowser.Document.GetElementsByTagName("head");
                        if (headers.Count > 0)
                        {
                            var head = headers[0];
                            var meta = WebBrowser.Document.CreateElement("meta");
                            meta.SetAttribute("http-equiv", "X-UA-Compatible");
                            meta.SetAttribute("content", "IE=11");
                            head.AppendChild(meta);
                            LogDebug("Meta tag para IE11 agregado");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"Error agregando meta tag: {ex.Message}");
                    }
                }
                
                // Método 3: Configuraciones adicionales del registro
                try
                {
                    ConfigureAdvancedJavaSettings();
                }
                catch (Exception ex)
                {
                    LogDebug($"Error en configuraciones avanzadas: {ex.Message}");
                }
                
            }
            catch (Exception ex)
            {
                LogDebug($"Error en ForceEnableJavaInBrowser: {ex.Message}");
            }
        }

        private void ConfigureAdvancedJavaSettings()
        {
            try
            {
                LogDebug("=== CONFIGURACIONES AVANZADAS DE JAVA ===");
                
                var appName = System.Diagnostics.Process.GetCurrentProcess().ProcessName + ".exe";
                
                // Habilitar controles ActiveX
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_OBJECT_CACHING"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 1, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_OBJECT_CACHING: 1");
                    }
                }
                
                // Deshabilitar seguridad mixta
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_SECURITYBAND"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_SECURITYBAND: 0");
                    }
                }
                
                // Permitir scripts sin restricciones
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_RESTRICT_ACTIVEXINSTALL"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_RESTRICT_ACTIVEXINSTALL: 0");
                    }
                }
                
                // Permitir scripts de confianza
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_TRUST_LABEL"))
                {
                    if (key != null)
                    {
                        key.SetValue(appName, 1, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug($"Configurado FEATURE_TRUST_LABEL: 1");
                    }
                }
                
                // Configurar Java directamente en el registro
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\JavaSoft\Java Runtime Environment\1.8\MSIProperties"))
                {
                    if (key != null)
                    {
                        key.SetValue("WEBSTARTICON", 1, Microsoft.Win32.RegistryValueKind.DWord);
                        key.SetValue("JAVAUPDATE", 0, Microsoft.Win32.RegistryValueKind.DWord);
                        LogDebug("Configuraciones Java en registro aplicadas");
                    }
                }
                
                // Zona de seguridad local
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\0"))
                {
                    if (key != null)
                    {
                        key.SetValue("1004", 0, Microsoft.Win32.RegistryValueKind.DWord); // Java applets
                        key.SetValue("1001", 0, Microsoft.Win32.RegistryValueKind.DWord); // ActiveX signed
                        key.SetValue("1200", 0, Microsoft.Win32.RegistryValueKind.DWord); // ActiveX unsigned
                        key.SetValue("1400", 0, Microsoft.Win32.RegistryValueKind.DWord); // Scripts activos
                        key.SetValue("1208", 0, Microsoft.Win32.RegistryValueKind.DWord); // Scripts de sitio
                        key.SetValue("1A04", 0, Microsoft.Win32.RegistryValueKind.DWord); // Java secure
                        LogDebug("Configurada zona local para Java y scripts");
                    }
                }
                
                LogDebug("Configuraciones avanzadas completadas");
            }
            catch (Exception ex)
            {
                LogDebug($"Error en configuraciones avanzadas: {ex.Message}");
            }
        }

        private void LoadQuickNotes()
        {
            try
            {
                if (File.Exists(NOTES_FILE))
                {
                    string notes = File.ReadAllText(NOTES_FILE);
                    txtQuickNotes.Text = notes;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error cargando notas: {ex.Message}");
            }
        }

        private void SaveQuickNotes()
        {
            try
            {
                File.WriteAllText(NOTES_FILE, txtQuickNotes.Text);
            }
            catch (Exception ex)
            {
                LogDebug($"Error guardando notas: {ex.Message}");
            }
        }

        private void BtnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (currentPdfData == null || currentPdfData.Length == 0)
                {
                    MessageBox.Show("No hay un PDF cargado para imprimir.", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Crear archivo temporal
                string tempFile = Path.Combine(Path.GetTempPath(), $"pdf_temp_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                File.WriteAllBytes(tempFile, currentPdfData);

                // Abrir con aplicación predeterminada para PDF (que permitirá imprimir)
                var psi = new ProcessStartInfo
                {
                    FileName = tempFile,
                    UseShellExecute = true,
                    Verb = "print"
                };

                Process.Start(psi);
                
                LogDebug($"PDF enviado a impresión: {tempFile}");
                txtStatus.Text = "PDF enviado a impresión";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al imprimir: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                LogDebug($"Error imprimiendo PDF: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // Guardar notas rápidas antes de cerrar
                SaveQuickNotes();
                
                WebBrowser?.Dispose();
            }
            catch { }
            
            httpClient?.Dispose();
            base.OnClosed(e);
        }
    }
}