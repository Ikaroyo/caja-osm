using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PdfExtractor.Models;
using PdfExtractor.Services;

namespace PdfExtractor.Windows
{
    public partial class LotesListWindow : Window
    {
        private List<LoteData> allLotes = new();
        private ObservableCollection<LoteData> filteredLotes = new();
        private string saveLocation;

        public LotesListWindow(string saveLocation)
        {
            InitializeComponent();
            this.saveLocation = saveLocation;
            LoadData();
            SetupFilters();
        }

        private void LoadData()
        {
            allLotes = DataService.LoadData(saveLocation);
            filteredLotes = new ObservableCollection<LoteData>(allLotes.OrderByDescending(l => l.Fecha));
            
            // Forzar actualización de propiedades formateadas usando el método público
            foreach (var lote in filteredLotes)
            {
                lote.OnPropertyChanged(nameof(lote.OSMFormatted));
                lote.OnPropertyChanged(nameof(lote.MUNIFormatted));
                lote.OnPropertyChanged(nameof(lote.CreditoFormatted));
                lote.OnPropertyChanged(nameof(lote.DebitoFormatted));
                lote.OnPropertyChanged(nameof(lote.ChequeFormatted));
                lote.OnPropertyChanged(nameof(lote.EfectivoFormatted));
                lote.OnPropertyChanged(nameof(lote.CreditoDebitoFormatted));
            }
            
            dgLotes.ItemsSource = filteredLotes;
            UpdateTotals();
        }

        private void SetupFilters()
        {
            // Llenar combo de días
            cmbDiaFilter.Items.Add("Todos");
            var dias = allLotes.Select(l => l.Dia).Distinct().OrderBy(d => d).ToList();
            foreach (var dia in dias)
            {
                cmbDiaFilter.Items.Add(dia);
            }
            cmbDiaFilter.SelectedIndex = 0;
            
            // Llenar combo de cajas
            cmbCajaFilter.Items.Add("Todas");
            cmbCajaFilter.Items.Add("CAJA 1");
            cmbCajaFilter.Items.Add("CAJA 2");
            cmbCajaFilter.SelectedIndex = 0;
            
            // Llenar combo de usuarios
            cmbUsuarioFilter.Items.Add("Todos");
            var usuarios = allLotes.Select(l => l.Usuario).Where(u => !string.IsNullOrEmpty(u)).Distinct().OrderBy(u => u).ToList();
            foreach (var usuario in usuarios)
            {
                cmbUsuarioFilter.Items.Add(usuario);
            }
            cmbUsuarioFilter.SelectedIndex = 0;
            
            // Llenar combo de estados
            cmbEstadoFilter.Items.Add("Todos");
            cmbEstadoFilter.Items.Add("Pendiente");
            cmbEstadoFilter.Items.Add("Depositado");
            cmbEstadoFilter.SelectedIndex = 0;
        }

        private void Filter_Changed(object sender, RoutedEventArgs e)
        {
            ApplyFilters();
        }

        private void Filter_Changed(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = allLotes.AsEnumerable();

            // Filtro por día
            if (cmbDiaFilter.SelectedItem != null && cmbDiaFilter.SelectedItem.ToString() != "Todos")
            {
                string? selectedDia = cmbDiaFilter.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedDia))
                {
                    filtered = filtered.Where(l => l.Dia == selectedDia);
                }
            }

            // Filtro por caja
            if (cmbCajaFilter.SelectedItem != null && cmbCajaFilter.SelectedItem.ToString() != "Todas")
            {
                string? selectedCaja = cmbCajaFilter.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedCaja))
                {
                    filtered = filtered.Where(l => l.Caja == selectedCaja);
                }
            }

            // Filtro por usuario
            if (cmbUsuarioFilter.SelectedItem != null && cmbUsuarioFilter.SelectedItem.ToString() != "Todos")
            {
                string? selectedUsuario = cmbUsuarioFilter.SelectedItem.ToString();
                if (!string.IsNullOrEmpty(selectedUsuario))
                {
                    filtered = filtered.Where(l => l.Usuario == selectedUsuario);
                }
            }

            // Filtro por estado
            if (cmbEstadoFilter.SelectedItem != null && cmbEstadoFilter.SelectedItem.ToString() != "Todos")
            {
                string? selectedEstado = cmbEstadoFilter.SelectedItem.ToString();
                if (selectedEstado == "Pendiente")
                {
                    filtered = filtered.Where(l => !l.Depositado);
                }
                else if (selectedEstado == "Depositado")
                {
                    filtered = filtered.Where(l => l.Depositado);
                }
            }

            // Filtro por fecha desde
            if (dpFechaDesde.SelectedDate.HasValue)
            {
                filtered = filtered.Where(l => l.Fecha >= dpFechaDesde.SelectedDate.Value);
            }

            // Filtro por fecha hasta
            if (dpFechaHasta.SelectedDate.HasValue)
            {
                filtered = filtered.Where(l => l.Fecha <= dpFechaHasta.SelectedDate.Value);
            }

            filteredLotes.Clear();
            foreach (var item in filtered.OrderByDescending(l => l.Fecha))
            {
                filteredLotes.Add(item);
            }

            UpdateTotals();
        }

        private void UpdateTotals()
        {
            var totalOSM = filteredLotes.Sum(l => l.OSM);
            var totalMUNI = filteredLotes.Sum(l => l.MUNI);
            var totalEfectivo = filteredLotes.Sum(l => l.Efectivo);
            var totalCreditoDebito = filteredLotes.Sum(l => l.Credito + l.Debito);

            txtTotalOSM.Text = FormatCurrency(totalOSM);
            txtTotalMUNI.Text = FormatCurrency(totalMUNI);
            txtTotalEfectivo.Text = FormatCurrency(totalEfectivo);
            txtTotalCreditoDebito.Text = FormatCurrency(totalCreditoDebito);
        }

        private string FormatCurrency(decimal value)
        {
            return value.ToString("C2", new CultureInfo("es-AR")).Replace("$", "$ ");
        }

        private void BtnMarcarDepositado_Click(object sender, RoutedEventArgs e)
        {
            var selectedLotes = dgLotes.SelectedItems.Cast<LoteData>().Select(l => l.Lote).ToList();
            
            if (!selectedLotes.Any())
            {
                MessageBox.Show("Seleccione al menos un lote para marcar como depositado.", "Información", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show($"¿Marcar {selectedLotes.Count} lote(s) como depositado?", "Confirmar", 
                MessageBoxButton.YesNo, MessageBoxImage.Question);
                
            if (result == MessageBoxResult.Yes)
            {
                DataService.UpdateDepositado(selectedLotes, true, saveLocation);
                LoadData(); // Recargar datos
            }
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            cmbDiaFilter.SelectedIndex = 0;
            cmbCajaFilter.SelectedIndex = 0;
            cmbUsuarioFilter.SelectedIndex = 0;
            cmbEstadoFilter.SelectedIndex = 0;
            dpFechaDesde.SelectedDate = null;
            dpFechaHasta.SelectedDate = null;
            ApplyFilters();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void DgLotes_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try
            {
                if (dgLotes.SelectedItem is LoteData selectedLote)
                {
                    // Crear ventana de edición
                    var editWindow = new Window
                    {
                        Title = $"Editar Lote {selectedLote.Lote}",
                        Width = 500,
                        Height = 400,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this,
                        ResizeMode = ResizeMode.NoResize
                    };

                    // Crear grid principal
                    var mainGrid = new Grid();
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                    mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                    // Crear scroll viewer con form
                    var scrollViewer = new ScrollViewer
                    {
                        Margin = new Thickness(10),
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto
                    };

                    var formGrid = new Grid();
                    formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
                    formGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                    // Crear campos editables
                    var fields = new Dictionary<string, TextBox>();
                    var labels = new[] { "Lote:", "Usuario:", "Caja:", "OSM:", "MUNI:", "Crédito:", "Débito:", "Cheque:", "Observaciones:" };
                    var values = new[] { 
                        selectedLote.Lote, 
                        selectedLote.Usuario, 
                        selectedLote.Caja, 
                        selectedLote.OSM.ToString("F2"), 
                        selectedLote.MUNI.ToString("F2"), 
                        selectedLote.Credito.ToString("F2"), 
                        selectedLote.Debito.ToString("F2"), 
                        selectedLote.Cheque.ToString("F2"),
                        selectedLote.Observaciones
                    };

                    for (int i = 0; i < labels.Length; i++)
                    {
                        formGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                        var label = new TextBlock
                        {
                            Text = labels[i],
                            VerticalAlignment = VerticalAlignment.Center,
                            Margin = new Thickness(0, 5, 10, 5),
                            FontWeight = FontWeights.Bold
                        };
                        Grid.SetRow(label, i);
                        Grid.SetColumn(label, 0);
                        formGrid.Children.Add(label);

                        var textBox = new TextBox
                        {
                            Text = values[i] ?? "",
                            Margin = new Thickness(0, 5, 0, 5),
                            Height = labels[i] == "Observaciones:" ? 60 : 25,
                            TextWrapping = labels[i] == "Observaciones:" ? TextWrapping.Wrap : TextWrapping.NoWrap,
                            AcceptsReturn = labels[i] == "Observaciones:",
                            VerticalScrollBarVisibility = labels[i] == "Observaciones:" ? ScrollBarVisibility.Auto : ScrollBarVisibility.Hidden
                        };
                        
                        // Solo permitir editar ciertos campos
                        if (labels[i] == "Lote:")
                        {
                            textBox.IsReadOnly = true;
                            textBox.Background = System.Windows.Media.Brushes.LightGray;
                        }

                        Grid.SetRow(textBox, i);
                        Grid.SetColumn(textBox, 1);
                        formGrid.Children.Add(textBox);

                        fields[labels[i]] = textBox;
                    }

                    scrollViewer.Content = formGrid;
                    Grid.SetRow(scrollViewer, 0);
                    mainGrid.Children.Add(scrollViewer);

                    // Crear botones
                    var buttonPanel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Margin = new Thickness(10)
                    };

                    var btnSave = new Button
                    {
                        Content = "💾 Guardar",
                        Width = 80,
                        Height = 30,
                        Margin = new Thickness(0, 0, 10, 0),
                        Background = System.Windows.Media.Brushes.LightGreen
                    };

                    var btnCancel = new Button
                    {
                        Content = "❌ Cancelar",
                        Width = 80,
                        Height = 30,
                        Background = System.Windows.Media.Brushes.LightCoral
                    };

                    btnSave.Click += (s, args) =>
                    {
                        try
                        {
                            // Actualizar el lote con los nuevos valores
                            selectedLote.Usuario = fields["Usuario:"].Text;
                            selectedLote.Caja = fields["Caja:"].Text;
                            selectedLote.Observaciones = fields["Observaciones:"].Text;
                            
                            if (decimal.TryParse(fields["OSM:"].Text, out decimal osm))
                                selectedLote.OSM = osm;
                            if (decimal.TryParse(fields["MUNI:"].Text, out decimal muni))
                                selectedLote.MUNI = muni;
                            if (decimal.TryParse(fields["Crédito:"].Text, out decimal credito))
                                selectedLote.Credito = credito;
                            if (decimal.TryParse(fields["Débito:"].Text, out decimal debito))
                                selectedLote.Debito = debito;
                            if (decimal.TryParse(fields["Cheque:"].Text, out decimal cheque))
                                selectedLote.Cheque = cheque;

                            // Guardar cambios
                            DataService.AddLote(selectedLote, saveLocation);
                            
                            // Recargar datos
                            LoadData();
                            
                            editWindow.DialogResult = true;
                            editWindow.Close();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error guardando cambios: {ex.Message}", "Error", 
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    };

                    btnCancel.Click += (s, args) =>
                    {
                        editWindow.DialogResult = false;
                        editWindow.Close();
                    };

                    buttonPanel.Children.Add(btnSave);
                    buttonPanel.Children.Add(btnCancel);
                    Grid.SetRow(buttonPanel, 1);
                    mainGrid.Children.Add(buttonPanel);

                    editWindow.Content = mainGrid;
                    editWindow.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error abriendo editor: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}