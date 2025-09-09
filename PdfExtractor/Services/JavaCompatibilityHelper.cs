using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

namespace PdfExtractor.Services
{
    public static class JavaCompatibilityHelper
    {
        public static bool IsJavaInstalled()
        {
            try
            {
                // Verificar si Java está instalado
                var javaKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment");
                if (javaKey != null)
                {
                    var version = javaKey.GetValue("CurrentVersion")?.ToString();
                    return !string.IsNullOrEmpty(version);
                }
                
                // Verificar en 32-bit en sistemas 64-bit
                javaKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment");
                if (javaKey != null)
                {
                    var version = javaKey.GetValue("CurrentVersion")?.ToString();
                    return !string.IsNullOrEmpty(version);
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static string GetJavaVersion()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "-version",
                        UseShellExecute = false,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardError.ReadToEnd();
                process.WaitForExit();
                
                return output;
            }
            catch
            {
                return "No disponible";
            }
        }

        public static bool IsInternetExplorerAvailable()
        {
            try
            {
                var iePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), 
                    "Internet Explorer", "iexplore.exe");
                
                if (File.Exists(iePath))
                    return true;
                
                // Verificar en Program Files (x86)
                iePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), 
                    "Internet Explorer", "iexplore.exe");
                
                return File.Exists(iePath);
            }
            catch
            {
                return false;
            }
        }

        public static void ConfigureIEForJava()
        {
            try
            {
                var appName = Path.GetFileName(Process.GetCurrentProcess().MainModule?.FileName ?? "PdfExtractor.exe");
                
                // Configurar IE para usar modo de compatibilidad
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", 
                    appName, 11001, RegistryValueKind.DWord);
                
                // Habilitar controles ActiveX
                Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_OBJECT_CACHING", 
                    appName, 0, RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configuring IE for Java: {ex.Message}");
            }
        }

        public static void ConfigureWebBrowserForJava()
        {
            try
            {
                var process = System.Diagnostics.Process.GetCurrentProcess();
                var mainModule = process.MainModule;
                if (mainModule?.FileName != null)
                {
                    var appName = Path.GetFileName(mainModule.FileName);
                    
                    // Configurar emulación de navegador para IE11 (11001 = IE11)
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", 
                        appName, 11001, RegistryValueKind.DWord);
                    
                    // Habilitar controles ActiveX y plugins (CRÍTICO para Java)
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_OBJECT_CACHING", 
                        appName, 0, RegistryValueKind.DWord);
                    
                    // Deshabilitar restricciones de seguridad para aplicación local
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_LOCALMACHINE_LOCKDOWN", 
                        appName, 0, RegistryValueKind.DWord);
                    
                    // Habilitar descarga de archivos y controles
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_SAFE_BINDTOOBJECT", 
                        appName, 0, RegistryValueKind.DWord);
                    
                    // Habilitar ejecución de scripts y Java
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_ENABLE_CLIPCHILDREN_OPTIMIZATION", 
                        appName, 1, RegistryValueKind.DWord);
                    
                    // Configuraciones adicionales para Java applets
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_DISABLE_NAVIGATION_SOUNDS", 
                        appName, 1, RegistryValueKind.DWord);
                    
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_WEBOC_POPUPMANAGEMENT", 
                        appName, 0, RegistryValueKind.DWord);
                    
                    // Configurar zona de seguridad para permitir Java (Zona 3 = Internet)
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1C00", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX marcados como seguros
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1001", 0, RegistryValueKind.DWord); // Habilitar Java applets
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1004", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1200", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX sin firma
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1201", 0, RegistryValueKind.DWord); // Inicializar controles ActiveX
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1400", 0, RegistryValueKind.DWord); // Habilitar scripting activo
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\3", 
                        "1402", 0, RegistryValueKind.DWord); // Habilitar scripting de applets Java
                    
                    // Configurar zona local (Zona 1 = Intranet local) - MÁS PERMISIVA
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1C00", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX marcados como seguros
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1001", 0, RegistryValueKind.DWord); // Habilitar Java applets
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1004", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1200", 0, RegistryValueKind.DWord); // Habilitar controles ActiveX sin firma
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1201", 0, RegistryValueKind.DWord); // Inicializar controles ActiveX
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1400", 0, RegistryValueKind.DWord); // Habilitar scripting activo
                    Registry.SetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Internet Settings\Zones\1", 
                        "1402", 0, RegistryValueKind.DWord); // Habilitar scripting de applets Java
                    
                    System.Diagnostics.Debug.WriteLine($"Configuración Java completada para: {appName}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando WebBrowser para Java: {ex.Message}");
            }
        }

        public static string GetJNLPPath()
        {
            try
            {
                // Buscar javaws.exe para ejecutar archivos JNLP
                var javaKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\JavaSoft\Java Runtime Environment");
                if (javaKey != null)
                {
                    var version = javaKey.GetValue("CurrentVersion")?.ToString();
                    if (!string.IsNullOrEmpty(version))
                    {
                        var versionKey = javaKey.OpenSubKey(version);
                        var javaHome = versionKey?.GetValue("JavaHome")?.ToString();
                        if (!string.IsNullOrEmpty(javaHome))
                        {
                            var javawsPath = Path.Combine(javaHome, "bin", "javaws.exe");
                            if (File.Exists(javawsPath))
                                return javawsPath;
                        }
                    }
                }
                
                // Verificar ubicaciones comunes
                var commonPaths = new[]
                {
                    @"C:\Program Files\Java\jre8\bin\javaws.exe",
                    @"C:\Program Files (x86)\Java\jre8\bin\javaws.exe",
                    @"C:\Program Files\Java\jre7\bin\javaws.exe",
                    @"C:\Program Files (x86)\Java\jre7\bin\javaws.exe"
                };
                
                foreach (var path in commonPaths)
                {
                    if (File.Exists(path))
                        return path;
                }
                
                return "javaws"; // Usar PATH del sistema
            }
            catch
            {
                return "javaws";
            }
        }

        public static string GetJavaInfo()
        {
            try
            {
                var info = new System.Text.StringBuilder();
                info.AppendLine("=== INFORMACIÓN DE JAVA ===");
                info.AppendLine($"Java instalado: {(IsJavaInstalled() ? "Sí" : "No")}");
                
                if (IsJavaInstalled())
                {
                    info.AppendLine($"Versión de Java:");
                    info.AppendLine(GetJavaVersion());
                    
                    var jnlpPath = GetJNLPPath();
                    info.AppendLine($"Ruta de JNLP: {jnlpPath}");
                    info.AppendLine($"JNLP disponible: {File.Exists(jnlpPath)}");
                }
                
                info.AppendLine($"Internet Explorer disponible: {(IsInternetExplorerAvailable() ? "Sí" : "No")}");
                
                return info.ToString();
            }
            catch (Exception ex)
            {
                return $"Error obteniendo información de Java: {ex.Message}";
            }
        }

        public static bool LaunchJNLP(string jnlpUrl)
        {
            try
            {
                var javawsPath = GetJNLPPath();
                
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = javawsPath,
                        Arguments = $"\"{jnlpUrl}\"",
                        UseShellExecute = true
                    }
                };
                
                return process.Start();
            }
            catch
            {
                return false;
            }
        }
    }
}
