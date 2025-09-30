using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using PdfExtractor.Services;
using PdfExtractor.Windows;
using PdfExtractor.Models;
using PdfExtractor.Modules;

namespace PdfExtractor
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient httpClient;
        private readonly ArqueoCajaModule arqueoCajaModule;
        private WindowLayoutConfig windowLayoutConfig;
        private bool isInitializationComplete = false;
        private const string BASE_URL = "http://192.168.100.80:7778/reports/rwservlet/getjobid{0}?SERVER=rep_vmo_bck";
        private const string SIGEMI_URL = "http://192.168.100.80:7778/forms/frmservlet?config=sigemi-vmo";
        private const string NOTES_FILE = "quick_notes.txt";
        private string currentPdfContent = "";
        private byte[]? currentPdfData;
        
        // Calculator variables
        private double calculatorResult = 0;
        private double calculatorOperand = 0;
        private string calculatorOperation = "";
        private bool isNewCalculation = true;
        
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
                Console.WriteLine(logEntry.Trim()); // Temporary console output for debugging
                
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
            
            // Initialize ArqueoCajaModule
            arqueoCajaModule = new ArqueoCajaModule(LogDebug);
            
            // Initialize window layout configuration
            windowLayoutConfig = WindowLayoutConfig.Load();
            
            // Set up event handlers
            this.Loaded += MainWindow_Loaded;
            this.ContentRendered += MainWindow_ContentRendered;
            this.SizeChanged += MainWindow_SizeChanged;
            
            // Initialize module after window is loaded
            this.Loaded += (s, e) => arqueoCajaModule.InitializeControls(this);
            
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
            
            // Arqueo configuration loading moved to ArqueoCajaModule
            
            // Delay para asegurar que todos los controles estén inicializados
            Dispatcher.BeginInvoke(new Action(() => {
                // Total calculation moved to ArqueoCajaModule
            }), System.Windows.Threading.DispatcherPriority.Background);
            
            // Inicializar formulario de datos extraídos
            ClearExtractedDataForm();
            
            // EJECUTAR PRUEBAS CON DATOS DE REFERENCIA
            TestAllPdfsWithReferenceData();
            
            // Inicializar cálculo de arqueo después de que la ventana esté cargada
            Dispatcher.BeginInvoke(new Action(() => {
                try
                {
                // Arqueo total calculation moved to ArqueoCajaModule
                }
                catch (Exception ex)
                {
                    LogDebug($"Error en inicialización de arqueo: {ex.Message}");
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                LogDebug("MainWindow_Loaded - Initial window setup");
            }
            catch (Exception ex)
            {
                LogDebug($"Error in MainWindow_Loaded: {ex.Message}");
            }
        }

        private void MainWindow_ContentRendered(object sender, EventArgs e)
        {
            try
            {
                // Use Dispatcher.BeginInvoke to ensure layout is fully ready
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    RestoreWindowLayout();
                    LogDebug("Window layout restored after content rendered via Dispatcher");
                    
                    // Also schedule a delayed restore to ensure it takes effect
                    var timer = new System.Windows.Threading.DispatcherTimer();
                    timer.Interval = TimeSpan.FromMilliseconds(500);
                    timer.Tick += (s, args) =>
                    {
                        timer.Stop();
                        RestoreWindowLayoutDelayed();
                    };
                    timer.Start();
                    
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            catch (Exception ex)
            {
                LogDebug($"Error restoring window layout in ContentRendered: {ex.Message}");
            }
        }

        private void RestoreWindowLayoutDelayed()
        {
            try
            {
                LogDebug($"=== Delayed Layout Restore ===");
                if (windowLayoutConfig.IsValid())
                {
                    if (Content is Grid grid && grid.ColumnDefinitions.Count > 0)
                    {
                        var leftColumn = grid.ColumnDefinitions[0];
                        var targetWidth = windowLayoutConfig.LeftPanelWidth;
                        
                        // Respect MinWidth constraint in delayed restore too
                        var minWidth = leftColumn.MinWidth;
                        if (targetWidth < minWidth)
                        {
                            LogDebug($"Delayed: Target width {targetWidth}px is less than MinWidth {minWidth}px, using MinWidth");
                            targetWidth = minWidth;
                        }
                        
                        LogDebug($"Delayed restore: Current={leftColumn.ActualWidth}px, Target={targetWidth}px");
                        
                        // Force the width with explicit pixel units
                        leftColumn.Width = new GridLength(targetWidth, GridUnitType.Pixel);
                        
                        // Force immediate layout update
                        grid.UpdateLayout();
                        
                        LogDebug($"Delayed restore applied: {leftColumn.Width.Value} {leftColumn.Width.GridUnitType}");
                    }
                }
                
                // Mark initialization as complete AFTER restoration
                isInitializationComplete = true;
                LogDebug("=== Initialization Complete - Auto-save enabled ===");
            }
            catch (Exception ex)
            {
                LogDebug($"Error in delayed layout restore: {ex.Message}");
                isInitializationComplete = true; // Enable auto-save even if restoration failed
            }
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            try
            {
                if (!isInitializationComplete)
                {
                    LogDebug("SizeChanged during initialization - skipping auto-save");
                    return;
                }
                
                if (WindowState != System.Windows.WindowState.Minimized)
                {
                    SaveCurrentLayout();
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error saving layout on size change: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                SaveCurrentLayout();
                LogDebug("Window layout saved on closing");
            }
            catch (Exception ex)
            {
                LogDebug($"Error saving layout on closing: {ex.Message}");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        private void RestoreWindowLayout()
        {
            try
            {
                LogDebug($"=== Starting RestoreWindowLayout ===");
                LogDebug($"Config values: LeftPanel={windowLayoutConfig.LeftPanelWidth}px, Window={windowLayoutConfig.WindowWidth}x{windowLayoutConfig.WindowHeight}, Maximized={windowLayoutConfig.IsWindowMaximized}");
                
                if (windowLayoutConfig.IsValid())
                {
                    // Restore window size and state
                    if (!windowLayoutConfig.IsWindowMaximized)
                    {
                        Width = windowLayoutConfig.WindowWidth;
                        Height = windowLayoutConfig.WindowHeight;
                        WindowState = System.Windows.WindowState.Normal;
                        LogDebug($"Window size restored to: {Width}x{Height}");
                    }
                    else
                    {
                        WindowState = System.Windows.WindowState.Maximized;
                        LogDebug("Window state set to Maximized");
                    }
                    
                    // Restore splitter position by setting the left column width
                    if (Content is Grid grid)
                    {
                        LogDebug($"Grid found with {grid.ColumnDefinitions.Count} columns");
                        
                        if (grid.ColumnDefinitions.Count > 0)
                        {
                            var leftColumn = grid.ColumnDefinitions[0];
                            
                            // Log current and target values
                            LogDebug($"Current left column width: {leftColumn.Width.Value} {leftColumn.Width.GridUnitType}");
                            LogDebug($"Target left column width: {windowLayoutConfig.LeftPanelWidth}px");
                            
                            // Set the new width with explicit pixel units
                            var newWidth = new GridLength(windowLayoutConfig.LeftPanelWidth, GridUnitType.Pixel);
                            leftColumn.Width = newWidth;
                            
                            LogDebug($"Layout restored - Left panel set to: {leftColumn.Width.Value} {leftColumn.Width.GridUnitType}");
                            
                            // Force layout update
                            grid.UpdateLayout();
                            LogDebug("Grid.UpdateLayout() called");
                        }
                        else
                        {
                            LogDebug("ERROR: No column definitions found in grid");
                        }
                    }
                    else
                    {
                        LogDebug($"ERROR: Content is not a Grid, it's a {Content?.GetType().Name ?? "null"}");
                    }
                }
                else
                {
                    LogDebug("Invalid layout config, using defaults");
                }
                
                LogDebug($"=== RestoreWindowLayout Complete ===");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in RestoreWindowLayout: {ex.Message}");
                LogDebug($"Exception details: {ex}");
            }
        }

        private void GridSplitter_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            try
            {
                LogDebug("GridSplitter drag completed - saving layout...");
                SaveCurrentLayout();
                LogDebug("GridSplitter position saved after drag");
            }
            catch (Exception ex)
            {
                LogDebug($"Error saving layout after splitter drag: {ex.Message}");
            }
        }

        private void SaveCurrentLayout()
        {
            try
            {
                if (!isInitializationComplete)
                {
                    LogDebug("SaveCurrentLayout called during initialization - skipping to preserve JSON values");
                    return;
                }
                
                LogDebug($"=== Starting SaveCurrentLayout ===");
                
                if (Content is Grid grid)
                {
                    LogDebug($"Grid found with {grid.ColumnDefinitions.Count} columns");
                    
                    if (grid.ColumnDefinitions.Count > 0)
                    {
                        var leftColumn = grid.ColumnDefinitions[0];
                        
                        // Use ActualWidth for accurate current measurements
                        double leftPanelWidth = leftColumn.ActualWidth;
                        
                        LogDebug($"Current ActualWidth: {leftPanelWidth}px");
                        LogDebug($"Current Width: {leftColumn.Width.Value} {leftColumn.Width.GridUnitType}");
                        LogDebug($"Previous JSON value: {windowLayoutConfig.LeftPanelWidth}px");
                        
                        windowLayoutConfig.LeftPanelWidth = leftPanelWidth;
                        windowLayoutConfig.WindowWidth = ActualWidth;
                        windowLayoutConfig.WindowHeight = ActualHeight;
                        windowLayoutConfig.IsWindowMaximized = WindowState == System.Windows.WindowState.Maximized;
                        
                        windowLayoutConfig.Save();
                        
                        LogDebug($"Layout saved - Left panel: {windowLayoutConfig.LeftPanelWidth:F0}px, Window: {windowLayoutConfig.WindowWidth:F0}x{windowLayoutConfig.WindowHeight:F0}, Maximized: {windowLayoutConfig.IsWindowMaximized}");
                    }
                    else
                    {
                        LogDebug("ERROR: No column definitions found in grid during save");
                    }
                }
                else
                {
                    LogDebug($"ERROR: Content is not a Grid during save, it's a {Content?.GetType().Name ?? "null"}");
                }
                
                LogDebug($"=== SaveCurrentLayout Complete ===");
            }
            catch (Exception ex)
            {
                LogDebug($"ERROR in SaveCurrentLayout: {ex.Message}");
                LogDebug($"Exception details: {ex}");
            }
        }

        private void ExecuteDebugExtraction()
        {
            try
            {
                var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
                if (System.IO.File.Exists(pdfPath))
                {
                    LogDebug("Ejecutando extracción de debug...");
                    var extractor = new PdfDataExtractor();
                    var data = extractor.ExtractFromFile(pdfPath);
                    LogDebug("Extracción de debug completada. Revise los archivos debug_*.txt");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en debug: {ex.Message}");
            }
        }

        private void TestAllPdfsWithReferenceData()
        {
            var testCases = new[]
            {
                new { File = "getjobid484197.pdf", Lote = "403279116", OSM = 1478575.98m, MUNI = 33016.1m, Usuario = "CMAGALLANES" },
                new { File = "getjobid485764.pdf", Lote = "403279119", OSM = 3243111.06m, MUNI = 234994.47m, Usuario = "FGONZALEZ" },
                new { File = "getjobid485763.pdf", Lote = "403279120", OSM = 5159825.54m, MUNI = 223836.74m, Usuario = "SCRESPO" },
                new { File = "getjobid485773.pdf", Lote = "403279121", OSM = 6154845.11m, MUNI = 356596.81m, Usuario = "FGONZALEZ" }
            };

            var resultsFile = @"d:\Users\Ikaros\Desktop\control-caja-osm\test_results.txt";
            var results = new System.Text.StringBuilder();
            results.AppendLine("=== RESULTADOS DE PRUEBAS DE EXTRACCIÓN ===");
            results.AppendLine($"Fecha: {DateTime.Now}");
            results.AppendLine();

            foreach (var testCase in testCases)
            {
                var filePath = @$"d:\Users\Ikaros\Desktop\control-caja-osm\{testCase.File}";
                results.AppendLine($"Probando archivo: {testCase.File}");
                results.AppendLine($"Valores esperados - Lote: {testCase.Lote}, OSM: {testCase.OSM}, MUNI: {testCase.MUNI}, Usuario: {testCase.Usuario}");

                if (System.IO.File.Exists(filePath))
                {
                    try
                    {
                        var extractor = new PdfDataExtractor();
                        var data = extractor.ExtractFromFile(filePath);

                        results.AppendLine($"Valores extraídos - Lote: '{data.Lote}', OSM: {data.TotalOSM}, MUNI: {data.TotalMunicipalidad}, Usuario: '{data.Usuario}'");
                        
                        // Análisis de precisión
                        var loteMatch = data.Lote == testCase.Lote;
                        var osmMatch = Math.Abs(data.TotalOSM - testCase.OSM) < 1; // Tolerancia de 1 peso
                        var muniMatch = Math.Abs(data.TotalMunicipalidad - testCase.MUNI) < 1;
                        var usuarioMatch = data.Usuario.Equals(testCase.Usuario, StringComparison.OrdinalIgnoreCase);

                        results.AppendLine($"Coincidencias - Lote: {(loteMatch ? "✓" : "✗")}, OSM: {(osmMatch ? "✓" : "✗")}, MUNI: {(muniMatch ? "✓" : "✗")}, Usuario: {(usuarioMatch ? "✓" : "✗")}");
                        
                        if (!muniMatch)
                        {
                            var difference = data.TotalMunicipalidad - testCase.MUNI;
                            results.AppendLine($"MUNI - Diferencia: {difference:F2} (Extraído: {data.TotalMunicipalidad}, Esperado: {testCase.MUNI})");
                        }
                        
                        results.AppendLine();
                    }
                    catch (Exception ex)
                    {
                        results.AppendLine($"ERROR: {ex.Message}");
                        results.AppendLine();
                    }
                }
                else
                {
                    results.AppendLine($"ARCHIVO NO ENCONTRADO: {filePath}");
                    results.AppendLine();
                }
            }

            try
            {
                System.IO.File.WriteAllText(resultsFile, results.ToString());
                LogDebug($"Resultados de pruebas escritos en: {resultsFile}");
            }
            catch (Exception ex)
            {
                LogDebug($"Error escribiendo resultados: {ex.Message}");
            }
        }

        private void TestPdfExtraction()
        {
            try
            {
                var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
                if (System.IO.File.Exists(pdfPath))
                {
                    LogDebug("=== INICIANDO TEST DE EXTRACCIÓN PDF ===");
                    var extractor = new PdfDataExtractor();
                    var data = extractor.ExtractFromFile(pdfPath);
                    
                    LogDebug($"Resultados de extracción:");
                    LogDebug($"- Lote: {data.Lote}");
                    LogDebug($"- Fecha: {data.FechaString}");
                    LogDebug($"- Usuario: {data.Usuario}");
                    LogDebug($"- OSM: {data.TotalOSM}");
                    LogDebug($"- MUNI: {data.TotalMunicipalidad}");
                    LogDebug($"- Total: {data.TotalGeneral}");
                    LogDebug("=== FIN TEST DE EXTRACCIÓN PDF ===");
                }
                else
                {
                    LogDebug("Archivo PDF de prueba no encontrado");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en test de extracción: {ex.Message}");
            }
        }

        private async Task TestPdfExtractionDebug()
        {
            try
            {
                var pdfPath = @"d:\Users\Ikaros\Desktop\control-caja-osm\getjobid484197.pdf";
                if (System.IO.File.Exists(pdfPath))
                {
                    LogDebug("=== INICIANDO DEBUG DE EXTRACCIÓN PDF ===");
                    var extractor = new PdfDataExtractor();
                    
                    // Leer el archivo y extraer texto
                    var pdfBytes = System.IO.File.ReadAllBytes(pdfPath);
                    var fullText = extractor.ExtractTextFromPdf(pdfBytes);
                    
                    // Mostrar el texto completo en la consola
                    System.Diagnostics.Debug.WriteLine("=== TEXTO COMPLETO DEL PDF ===");
                    System.Diagnostics.Debug.WriteLine(fullText);
                    System.Diagnostics.Debug.WriteLine("=== FIN TEXTO PDF ===");
                    
                    // Extraer datos estructurados
                    var data = extractor.ExtractStructuredData(fullText);
                    
                    LogDebug($"Resultados de extracción después de debug:");
                    LogDebug($"- Lote: '{data.Lote}'");
                    LogDebug($"- Fecha: '{data.FechaString}'");
                    LogDebug($"- Usuario: '{data.Usuario}'");
                    LogDebug($"- OSM: {data.TotalOSM}");
                    LogDebug($"- MUNI: {data.TotalMunicipalidad}");
                    LogDebug($"- Total: {data.TotalGeneral}");
                    LogDebug("=== FIN DEBUG DE EXTRACCIÓN PDF ===");
                }
                else
                {
                    LogDebug("Archivo PDF de prueba no encontrado para debug");
                }
            }
            catch (Exception ex)
            {
                LogDebug($"Error en debug de extracción: {ex.Message}");
                LogDebug($"StackTrace: {ex.StackTrace}");
            }
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
                txtStatus.Text = "Procesando...";
                txtContent.Text = "";
                
                // Limpiar formulario antes de procesar
                ClearExtractedDataForm();

                string url = BuildUrl(input);
                txtStatus.Text = $"Descargando PDF desde: {url}";

                byte[] pdfData = await DownloadPdfAsync(url);
                currentPdfData = pdfData; // Guardar datos para imprimir
                txtStatus.Text = "Extrayendo contenido del PDF...";

                string extractedText = ExtractTextFromPdf(pdfData);
                currentPdfContent = extractedText;
                txtContent.Text = extractedText;

                txtStatus.Text = "Extrayendo y guardando datos estructurados...";
                
                // Llamar automáticamente a la extracción y guardado de datos
                await ExtractAndSaveDataAutomatically();

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

        private async Task ExtractAndSaveDataAutomatically()
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
                    
                    // NO mostrar datos en formulario principal - solo en modal de revisión
                    // DisplayExtractedData(extractedData); // Comentado para no mostrar en vista principal
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
                
                // Mostrar ventana de revisión para confirmar datos
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
                        txtStatus.Text = "✅ Datos guardados correctamente.";
                        
                        // Limpiar formulario después de guardar exitosamente
                        ClearExtractedDataForm();
                        
                        MessageBox.Show($"✅ Datos procesados y guardados exitosamente!\n\n" +
                                      $"Lote: {extractedData.Lote}\n" +
                                      $"OSM: ${extractedData.OSM:N2}\n" +
                                      $"MUNI: ${extractedData.MUNI:N2}\n" +
                                      $"Total: ${(extractedData.OSM + extractedData.MUNI):N2}", 
                                      "Proceso Completado", 
                                      MessageBoxButton.OK, 
                                      MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error guardando datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        txtStatus.Text = "Error guardando datos";
                    }
                }
                else
                {
                    txtStatus.Text = "Extracción cancelada por el usuario.";
                    // Mantener datos en formulario si se cancela
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error general: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Error en proceso automático";
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

                txtStatus.Text = "Extrayendo datos estructurados con PdfPig...";
                
                LoteData extractedData;
                try
                {
                    // Try the improved PdfPig extractor first
                    if (currentPdfData != null && currentPdfData.Length > 0)
                    {
                        // Create temporary PDF file for PdfPig
                        string tempFile = Path.Combine(Path.GetTempPath(), $"pdf_extract_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                        File.WriteAllBytes(tempFile, currentPdfData);
                        
                        try
                        {
                            extractedData = PdfParser.ExtractDataWithPdfPig(tempFile);
                            txtStatus.Text = "Datos extraídos con PdfPig (método mejorado)";
                        }
                        finally
                        {
                            // Clean up temporary file
                            try { File.Delete(tempFile); } catch { }
                        }
                    }
                    else
                    {
                        // Fallback to original method
                        extractedData = PdfParser.ExtractData(currentPdfContent);
                        txtStatus.Text = "Datos extraídos con método original";
                    }
                    
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

                txtStatus.Text = "Guardando datos automáticamente...";
                
                // Guardar datos directamente sin mostrar ventana de revisión
                try
                {
                    DataService.AddLote(extractedData, config.SaveLocation);
                    txtStatus.Text = "Datos extraídos y guardados correctamente.";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error guardando datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error guardando datos";
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

                System.Diagnostics.Debug.WriteLine("Step 2: Starting data extraction with improved PdfPig...");
                txtStatus.Text = "Extrayendo datos con PdfPig (método mejorado)...";
                
                LoteData extractedData;
                try
                {
                    // Use the improved PdfPig extractor like in ExtractDataAutomatically
                    if (currentPdfData != null && currentPdfData.Length > 0)
                    {
                        // Create temporary PDF file for PdfPig
                        string tempFile = Path.Combine(Path.GetTempPath(), $"pdf_extract_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                        File.WriteAllBytes(tempFile, currentPdfData);
                        
                        try
                        {
                            System.Diagnostics.Debug.WriteLine($"Using PdfPig extractor on temp file: {tempFile}");
                            extractedData = PdfParser.ExtractDataWithPdfPig(tempFile);
                            System.Diagnostics.Debug.WriteLine("Data extraction with PdfPig completed successfully");
                        }
                        finally
                        {
                            // Clean up temporary file
                            try { File.Delete(tempFile); } catch { }
                        }
                    }
                    else
                    {
                        // Fallback to original method if no PDF data
                        System.Diagnostics.Debug.WriteLine($"Falling back to original method. PDF Content length: {currentPdfContent.Length}");
                        extractedData = PdfParser.ExtractData(currentPdfContent);
                        System.Diagnostics.Debug.WriteLine("Data extraction with fallback completed successfully");
                    }
                    
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

                System.Diagnostics.Debug.WriteLine("Step 3: Saving data directly...");
                txtStatus.Text = "Guardando datos automáticamente...";
                
                // Guardar datos directamente sin mostrar ventana de revisión
                try
                {
                    DataService.AddLote(extractedData, config.SaveLocation);
                    txtStatus.Text = "Datos extraídos y guardados correctamente.";
                    System.Diagnostics.Debug.WriteLine("Data saved successfully");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving data: {ex}");
                    MessageBox.Show($"Error guardando datos: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "Error guardando datos";
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
            // Este método ya no se usa porque los datos se muestran solo en el modal
            // Los datos aparecerán únicamente en la DataReviewWindow
            LogDebug($"Datos extraídos para mostrar en modal: OSM={data.OSM:N2}, MUNI={data.MUNI:N2}");
            
            // Log extra debug info
            System.Diagnostics.Debug.WriteLine($"=== EXTRACTED DATA SUMMARY ===");
            System.Diagnostics.Debug.WriteLine($"Lote: {data.Lote}");
            System.Diagnostics.Debug.WriteLine($"Usuario: {data.Usuario}");
            System.Diagnostics.Debug.WriteLine($"Credito: {data.Credito:N2}");
            System.Diagnostics.Debug.WriteLine($"Debito: {data.Debito:N2}");
            System.Diagnostics.Debug.WriteLine($"OSM: {data.OSM:N2}");
            System.Diagnostics.Debug.WriteLine($"MUNI: {data.MUNI:N2}");
            System.Diagnostics.Debug.WriteLine($"==============================");
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
            
            // Limpiar formulario reorganizado
            ClearExtractedDataForm();
        }

        private void ClearExtractedDataForm()
        {
            // Este método ya no es necesario porque no hay formulario en la vista principal
            // Los datos se manejan únicamente en el modal DataReviewWindow
            LogDebug("Vista principal limpiada - no hay formulario de datos que limpiar");
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
        
        // Arqueo configuration loading moved to ArqueoCajaModule

        // Arqueo configuration saving moved to ArqueoCajaModule

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

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            // Delay calculation to ensure both date pickers are updated
            Dispatcher.BeginInvoke(new Action(() => {
                arqueoCajaModule?.OnDatePickerChanged();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void CmbCajaSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Delay calculation to ensure selection is fully updated
            Dispatcher.BeginInvoke(new Action(() => {
                arqueoCajaModule?.OnCajaSelectionChanged();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void CashCount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                arqueoCajaModule?.OnCashCountChanged(textBox);
            }
        }

        // Cash denomination calculation moved to ArqueoCajaModule
        // Arqueo functionality has been modularized

        // Total calculation moved to ArqueoCajaModule

        // Denomination total calculation moved to ArqueoCajaModule

        // Value parsing moved to ArqueoCajaModule

        // Expected total calculation moved to ArqueoCajaModule

        // Expected cash calculation moved to ArqueoCajaModule

        private void BtnLimpiarCalculadora_Click(object sender, RoutedEventArgs e)
        {
            arqueoCajaModule?.ClearCalculator();
        }

        private void BtnActualizarTotal_Click(object sender, RoutedEventArgs e)
        {
            arqueoCajaModule?.UpdateTotal();
        }

        private void AdditionalValue_TextChanged(object sender, TextChangedEventArgs e)
        {
            arqueoCajaModule?.OnAdditionalValueChanged();
        }

        private void LoteHoy_TextChanged(object sender, TextChangedEventArgs e)
        {
            arqueoCajaModule?.OnLoteHoyChanged();
        }

        // Colombian currency formatting moved to ArqueoCajaModule

        // Calculator functionality
        private void BtnCalc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string buttonContent = button.Content.ToString();
                
                switch (buttonContent)
                {
                    case "C":
                        ClearCalculator();
                        break;
                    case "±":
                        ToggleSign();
                        break;
                    case "=":
                        PerformCalculation();
                        break;
                    case "+":
                    case "-":
                    case "×":
                    case "÷":
                    case "%":
                        SetOperation(buttonContent);
                        break;
                    case ",":
                        AddDecimalPoint();
                        break;
                    default:
                        AddDigit(buttonContent);
                        break;
                }
            }
        }

        private void ClearCalculator()
        {
            calculatorResult = 0;
            calculatorOperand = 0;
            calculatorOperation = "";
            isNewCalculation = true;
            txtCalculatorDisplay.Text = "0";
        }

        private void AddDigit(string digit)
        {
            if (isNewCalculation)
            {
                txtCalculatorDisplay.Text = digit;
                isNewCalculation = false;
            }
            else
            {
                if (txtCalculatorDisplay.Text == "0")
                    txtCalculatorDisplay.Text = digit;
                else
                    txtCalculatorDisplay.Text += digit;
            }
        }

        private void AddDecimalPoint()
        {
            if (isNewCalculation)
            {
                txtCalculatorDisplay.Text = "0,";
                isNewCalculation = false;
            }
            else if (!txtCalculatorDisplay.Text.Contains(","))
            {
                txtCalculatorDisplay.Text += ",";
            }
        }

        private void ToggleSign()
        {
            if (double.TryParse(txtCalculatorDisplay.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                value = -value;
                txtCalculatorDisplay.Text = value.ToString("0.##", CultureInfo.InvariantCulture).Replace(".", ",");
            }
        }

        private void SetOperation(string operation)
        {
            if (double.TryParse(txtCalculatorDisplay.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double value))
            {
                if (!string.IsNullOrEmpty(calculatorOperation) && !isNewCalculation)
                {
                    PerformCalculation();
                }
                
                calculatorResult = value;
                calculatorOperation = operation;
                isNewCalculation = true;
            }
        }

        private void PerformCalculation()
        {
            if (double.TryParse(txtCalculatorDisplay.Text.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out double operand))
            {
                double result = calculatorResult;
                
                switch (calculatorOperation)
                {
                    case "+":
                        result = calculatorResult + operand;
                        break;
                    case "-":
                        result = calculatorResult - operand;
                        break;
                    case "×":
                        result = calculatorResult * operand;
                        break;
                    case "÷":
                        result = operand != 0 ? calculatorResult / operand : 0;
                        break;
                    case "%":
                        result = calculatorResult % operand;
                        break;
                }
                
                txtCalculatorDisplay.Text = result.ToString("0.##", CultureInfo.InvariantCulture).Replace(".", ",");
                calculatorResult = result;
                calculatorOperation = "";
                isNewCalculation = true;
            }
        }

        // Expression evaluation methods
        private void TxtCalculatorDisplay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                EvaluateExpression();
                e.Handled = true;
            }
        }

        private void TxtCalculatorDisplay_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset calculator state when user starts typing
            if (!isNewCalculation)
            {
                calculatorResult = 0;
                calculatorOperand = 0;
                calculatorOperation = "";
                isNewCalculation = true;
            }
        }

        private void BtnEvaluateExpression_Click(object sender, RoutedEventArgs e)
        {
            EvaluateExpression();
        }

        private void EvaluateExpression()
        {
            try
            {
                string expression = txtCalculatorDisplay.Text.Trim();
                
                if (string.IsNullOrEmpty(expression) || expression == "0")
                    return;

                // Replace × and ÷ with * and /
                expression = expression.Replace("×", "*").Replace("÷", "/");
                
                // Replace comma decimal separator with period for calculation
                expression = expression.Replace(",", ".");
                
                // Evaluate the expression
                double result = EvaluateMathExpression(expression);
                
                // Display result with Colombian format
                txtCalculatorDisplay.Text = result.ToString("0.##", CultureInfo.InvariantCulture).Replace(".", ",");
                
                // Update calculator state
                calculatorResult = result;
                calculatorOperation = "";
                isNewCalculation = true;
            }
            catch (Exception ex)
            {
                txtCalculatorDisplay.Text = "Error: " + ex.Message;
                LogDebug($"Calculator error: {ex.Message}");
            }
        }

        private double EvaluateMathExpression(string expression)
        {
            // Simple math expression evaluator
            // Supports: +, -, *, /, (, ), numbers
            
            try
            {
                // Remove any spaces
                expression = expression.Replace(" ", "");
                
                // Validate expression contains only valid characters
                if (!System.Text.RegularExpressions.Regex.IsMatch(expression, @"^[0-9+\-*/().]+$"))
                {
                    throw new ArgumentException("Expresión contiene caracteres inválidos");
                }
                
                // Use DataTable.Compute for simple expression evaluation
                var table = new System.Data.DataTable();
                var result = table.Compute(expression, null);
                
                if (result == DBNull.Value)
                    throw new ArgumentException("Expresión inválida");
                
                return Convert.ToDouble(result);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Error evaluando expresión: {ex.Message}");
            }
        }

        private double EvaluateCountExpression(string input)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(input))
                    return 0;

                // Remove spaces
                input = input.Trim().Replace(" ", "");

                // If it's just a simple number, parse it directly
                if (double.TryParse(input, out double simpleNumber))
                {
                    return simpleNumber;
                }

                // Check if it contains mathematical expressions
                if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^[0-9+\-*/().]+$"))
                {
                    try
                    {
                        // Use DataTable.Compute for expression evaluation
                        var table = new System.Data.DataTable();
                        var result = table.Compute(input, null);
                        
                        if (result != DBNull.Value && double.TryParse(result.ToString(), out double evalResult))
                        {
                            return Math.Max(0, evalResult); // Ensure non-negative result
                        }
                    }
                    catch
                    {
                        // If evaluation fails, return -1 to indicate error
                        return -1;
                    }
                }

                // If it's not a valid expression, return -1 to indicate error
                return -1;
            }
            catch
            {
                return -1;
            }
        }


    }
}