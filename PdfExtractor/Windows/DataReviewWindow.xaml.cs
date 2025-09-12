using System;
using System.Windows;
using PdfExtractor.Models;

namespace PdfExtractor.Windows
{
    public partial class DataReviewWindow : Window
    {
        public LoteData Data { get; private set; }
        public bool Saved { get; private set; } = false;

        public DataReviewWindow(LoteData data)
        {
            try
            {
                InitializeComponent();
                Data = data ?? throw new ArgumentNullException(nameof(data));
                DataContext = Data;
                
                // Cargar configuración de caja
                var config = AppConfig.Load();
                Data.Caja = config.CajaSeleccionada;
                chkCaja2.IsChecked = (config.CajaSeleccionada == "CAJA 2");
                
                // Si no hay cheque, mostrar opción para ingreso manual
                if (Data.Cheque == 0 || true)
                {
                    chkIngresarCheque.IsChecked = true;
                    txtCheque.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al inicializar ventana de revisión: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private void ChkCaja_Changed(object sender, RoutedEventArgs e)
        {
            Data.Caja = chkCaja2.IsChecked == true ? "CAJA 2" : "CAJA 1";
            
            // Guardar preferencia
            var config = AppConfig.Load();
            config.CajaSeleccionada = Data.Caja;
            config.Save();
        }

        private void ChkIngresarCheque_Checked(object sender, RoutedEventArgs e)
        {
            txtCheque.IsEnabled = chkIngresarCheque.IsChecked == true;
            if (chkIngresarCheque.IsChecked == false)
            {
                txtCheque.Text = "0";
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validar datos
                if (string.IsNullOrEmpty(Data.Lote))
                {
                    System.Windows.MessageBox.Show("El número de lote es requerido.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (Data.Fecha == DateTime.MinValue)
                {
                    System.Windows.MessageBox.Show("La fecha es requerida.", "Validación", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                Saved = true;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error al guardar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TxtCheque_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (decimal.TryParse(txtCheque.Text, out var chequeValue))
            {
                Data.Cheque = chequeValue;
            }
            else
            {
                Data.Cheque = 0;
            }
        }
    }
}