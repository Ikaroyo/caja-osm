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

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
