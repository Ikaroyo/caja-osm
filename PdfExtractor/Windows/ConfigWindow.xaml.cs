using System;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using PdfExtractor.Models;

namespace PdfExtractor.Windows
{
    public partial class ConfigWindow : Window
    {
        private AppConfig config;

        public ConfigWindow()
        {
            InitializeComponent();
            config = AppConfig.Load();
            txtSaveLocation.Text = config.SaveLocation;
            txtWebPageUrl.Text = config.WebPageUrl; // Nueva línea
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Seleccione la carpeta donde guardar los datos";
            dialog.UseDescriptionForTitle = true;
            
            if (!string.IsNullOrEmpty(txtSaveLocation.Text) && Directory.Exists(txtSaveLocation.Text))
            {
                dialog.InitialDirectory = txtSaveLocation.Text;
            }
            
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtSaveLocation.Text = dialog.SelectedPath;
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtSaveLocation.Text))
            {
                System.Windows.MessageBox.Show("Debe seleccionar una ubicación de guardado.", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            config.SaveLocation = txtSaveLocation.Text;
            config.WebPageUrl = txtWebPageUrl.Text; // Nueva línea
            config.Save();
            
            System.Windows.MessageBox.Show("Configuración guardada correctamente.", "Éxito", 
                MessageBoxButton.OK, MessageBoxImage.Information);
            
            DialogResult = true;
            Close();
        }

        private void BtnMoverDatos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dialog = new FolderBrowserDialog();
                dialog.Description = "Seleccione la nueva ubicación para mover los datos";
                dialog.UseDescriptionForTitle = true;
                
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string nuevaUbicacion = dialog.SelectedPath;
                    
                    if (nuevaUbicacion == config.SaveLocation)
                    {
                        System.Windows.MessageBox.Show("La nueva ubicación es la misma que la actual.", "Aviso", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                    
                    // Confirmar la operación
                    string mensaje = $"¿Está seguro que desea mover todos los datos desde:\n\n";
                    mensaje += $"ORIGEN: {config.SaveLocation}\n";
                    mensaje += $"DESTINO: {nuevaUbicacion}\n\n";
                    mensaje += "Esta operación copiará todos los archivos JSON a la nueva ubicación y actualizará la configuración.";
                    
                    var result = System.Windows.MessageBox.Show(mensaje, "Confirmar Mover Datos", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Realizar el movimiento
                        if (MoverDatos(config.SaveLocation, nuevaUbicacion))
                        {
                            // Actualizar configuración
                            config.SaveLocation = nuevaUbicacion;
                            txtSaveLocation.Text = nuevaUbicacion;
                            
                            System.Windows.MessageBox.Show("Datos movidos exitosamente a la nueva ubicación.", "Éxito", 
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error moviendo datos: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private bool MoverDatos(string origen, string destino)
        {
            try
            {
                // Crear directorio destino si no existe
                Directory.CreateDirectory(destino);
                
                // Buscar todos los archivos JSON en el origen
                string[] archivosJson = Directory.GetFiles(origen, "*.json");
                
                if (archivosJson.Length == 0)
                {
                    System.Windows.MessageBox.Show("No se encontraron archivos de datos para mover.", "Aviso", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return true;
                }
                
                int archivosCopied = 0;
                foreach (string archivo in archivosJson)
                {
                    string nombreArchivo = Path.GetFileName(archivo);
                    string archivoDestino = Path.Combine(destino, nombreArchivo);
                    
                    File.Copy(archivo, archivoDestino, true);
                    archivosCopied++;
                }
                
                System.Windows.MessageBox.Show($"Se movieron {archivosCopied} archivo(s) de datos exitosamente.", "Información", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error durante el movimiento: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
