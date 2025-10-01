using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PdfExtractor.Models;
using PdfExtractor.Services;

namespace PdfExtractor.Modules
{
    public class ArqueoCajaModule
    {
        private readonly Action<string> logAction;
        private const string CONFIG_FILE = "arqueo_config.txt";

        // UI Controls references
        private ComboBox cmbCajaSelection;
        private DatePicker dpFechaInicial;
        private DatePicker dpFechaFinal;
        private TextBox txtLoteHoy;
        private TextBox txtTotalEsperado;
        private TextBox txtTotalContado;
        private TextBox txtDiferencia;
        private System.Windows.Controls.Border borderDiferencia;
        private TextBlock txtDiferenciaLabel;

        // Cash denomination controls
        private TextBox txt20000Count;
        private TextBox txt10000Count;
        private TextBox txt2000Count;
        private TextBox txt1000Count;
        private TextBox txt500Count;
        private TextBox txt200Count;
        private TextBox txt100Count;

        // Total controls
        private TextBox txt20000Total;
        private TextBox txt10000Total;
        private TextBox txt2000Total;
        private TextBox txt1000Total;
        private TextBox txt500Total;
        private TextBox txt200Total;
        private TextBox txt100Total;

        // Additional values
        private TextBox txtValor1;
        private TextBox txtValor2;
        private TextBox txtValor3;
        private TextBox txtValor4;
        private TextBox txtValor5;

        // Additional value totals
        private TextBox txtValor1Total;
        private TextBox txtValor2Total;
        private TextBox txtValor3Total;
        private TextBox txtValor4Total;
        private TextBox txtValor5Total;

        // Additional value labels
        private TextBox txtEtiqueta1;
        private TextBox txtEtiqueta2;
        private TextBox txtEtiqueta3;
        private TextBox txtEtiqueta4;
        private TextBox txtEtiqueta5;

        // New total display controls for modern UI
        private TextBox txtTotalEfectivo;
        private TextBox txtTotalOtros;

        public ArqueoCajaModule(Action<string> logAction)
        {
            this.logAction = logAction ?? (_ => { });
        }

        public void InitializeControls(Window mainWindow)
        {
            try
            {
                // Get control references from main window
                cmbCajaSelection = mainWindow.FindName("cmbCajaSelection") as ComboBox;
                dpFechaInicial = mainWindow.FindName("dpFechaInicial") as DatePicker;
                dpFechaFinal = mainWindow.FindName("dpFechaFinal") as DatePicker;
                txtLoteHoy = mainWindow.FindName("txtLoteHoy") as TextBox;
                txtTotalEsperado = mainWindow.FindName("txtTotalEsperado") as TextBox;
                txtTotalContado = mainWindow.FindName("txtTotalContado") as TextBox;
                txtDiferencia = mainWindow.FindName("txtDiferencia") as TextBox;
                borderDiferencia = mainWindow.FindName("borderDiferencia") as System.Windows.Controls.Border;
                txtDiferenciaLabel = mainWindow.FindName("txtDiferenciaLabel") as TextBlock;

                // Cash denomination count controls
                txt20000Count = mainWindow.FindName("txt20000Count") as TextBox;
                txt10000Count = mainWindow.FindName("txt10000Count") as TextBox;
                txt2000Count = mainWindow.FindName("txt2000Count") as TextBox;
                txt1000Count = mainWindow.FindName("txt1000Count") as TextBox;
                txt500Count = mainWindow.FindName("txt500Count") as TextBox;
                txt200Count = mainWindow.FindName("txt200Count") as TextBox;
                txt100Count = mainWindow.FindName("txt100Count") as TextBox;

                // Cash denomination total controls
                txt20000Total = mainWindow.FindName("txt20000Total") as TextBox;
                txt10000Total = mainWindow.FindName("txt10000Total") as TextBox;
                txt2000Total = mainWindow.FindName("txt2000Total") as TextBox;
                txt1000Total = mainWindow.FindName("txt1000Total") as TextBox;
                txt500Total = mainWindow.FindName("txt500Total") as TextBox;
                txt200Total = mainWindow.FindName("txt200Total") as TextBox;
                txt100Total = mainWindow.FindName("txt100Total") as TextBox;

                // Additional values
                txtValor1 = mainWindow.FindName("txtValor1") as TextBox;
                txtValor2 = mainWindow.FindName("txtValor2") as TextBox;
                txtValor3 = mainWindow.FindName("txtValor3") as TextBox;
                txtValor4 = mainWindow.FindName("txtValor4") as TextBox;
                txtValor5 = mainWindow.FindName("txtValor5") as TextBox;

                // Additional value totals
                txtValor1Total = mainWindow.FindName("txtValor1Total") as TextBox;
                txtValor2Total = mainWindow.FindName("txtValor2Total") as TextBox;
                txtValor3Total = mainWindow.FindName("txtValor3Total") as TextBox;
                txtValor4Total = mainWindow.FindName("txtValor4Total") as TextBox;
                txtValor5Total = mainWindow.FindName("txtValor5Total") as TextBox;

                // Additional value labels
                txtEtiqueta1 = mainWindow.FindName("txtEtiqueta1") as TextBox;
                txtEtiqueta2 = mainWindow.FindName("txtEtiqueta2") as TextBox;
                txtEtiqueta3 = mainWindow.FindName("txtEtiqueta3") as TextBox;
                txtEtiqueta4 = mainWindow.FindName("txtEtiqueta4") as TextBox;
                txtEtiqueta5 = mainWindow.FindName("txtEtiqueta5") as TextBox;

                // New total display controls for modern UI
                txtTotalEfectivo = mainWindow.FindName("txtTotalEfectivo") as TextBox;
                txtTotalOtros = mainWindow.FindName("txtTotalOtros") as TextBox;

                logAction("ArqueoCajaModule inicializado correctamente");
                LoadArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error inicializando ArqueoCajaModule: {ex.Message}");
            }
        }

        public void OnDatePickerChanged()
        {
            try
            {
                CalculateExpectedTotal();
                SaveArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error en DatePicker changed: {ex.Message}");
            }
        }

        public void OnCajaSelectionChanged()
        {
            try
            {
                CalculateExpectedTotal();
                SaveArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error en Caja selection changed: {ex.Message}");
            }
        }

        public void OnCashCountChanged(TextBox countTextBox)
        {
            try
            {
                CalculateCashDenomination(countTextBox);
                UpdateCashTooltips();
                CalculateTotals();
            }
            catch (Exception ex)
            {
                logAction($"Error en Cash count changed: {ex.Message}");
            }
        }

        public void OnLabelChanged()
        {
            try
            {
                SaveArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error en Label changed: {ex.Message}");
            }
        }

        public void OnAdditionalValueChanged()
        {
            try
            {
                // Calculate individual totals for additional values
                CalculateAdditionalValueTotals();
                // Update tooltips for additional value calculations
                UpdateAdditionalValueTooltips();
                CalculateTotals();
                // Save configuration
                SaveArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error en Additional value changed: {ex.Message}");
            }
        }

        public void OnLoteHoyChanged()
        {
            try
            {
                if (txtLoteHoy != null)
                {
                    var result = EvaluateCountExpression(txtLoteHoy.Text);
                    if (result > 0) // Valid expression
                    {
                        txtLoteHoy.ToolTip = $"= {FormatColombianCurrency((decimal)result)}";
                    }
                    else
                    {
                        txtLoteHoy.ToolTip = null;
                    }
                }

                CalculateExpectedTotal();
                SaveArqueoConfig();
            }
            catch (Exception ex)
            {
                logAction($"Error en Lote Hoy changed: {ex.Message}");
            }
        }

        public void ClearCalculator()
        {
            try
            {
                // Clear all count fields
                if (txt20000Count != null) txt20000Count.Text = "0";
                if (txt10000Count != null) txt10000Count.Text = "0";
                if (txt2000Count != null) txt2000Count.Text = "0";
                if (txt1000Count != null) txt1000Count.Text = "0";
                if (txt500Count != null) txt500Count.Text = "0";
                if (txt200Count != null) txt200Count.Text = "0";
                if (txt100Count != null) txt100Count.Text = "0";

                // Clear additional values
                if (txtValor1 != null) txtValor1.Text = "0";
                if (txtValor2 != null) txtValor2.Text = "0";
                if (txtValor3 != null) txtValor3.Text = "0";
                if (txtValor4 != null) txtValor4.Text = "0";
                if (txtValor5 != null) txtValor5.Text = "0";

                // This will automatically trigger totals recalculation
                CalculateTotals();

                logAction("Calculadora de arqueo limpiada");
            }
            catch (Exception ex)
            {
                logAction($"Error limpiando calculadora: {ex.Message}");
            }
        }

        public void UpdateTotal()
        {
            try
            {
                logAction("Actualizando total esperado - forzando recálculo");
                CalculateExpectedTotal();

                // Show informative message if no data
                var config = AppConfig.Load();
                if (string.IsNullOrEmpty(config.SaveLocation))
                {
                    MessageBox.Show("Debe configurar una ubicación de guardado primero.\nVaya a Configuración → Config para establecer la carpeta de datos.",
                        "Configuración Requerida", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var data = DataService.LoadData(config.SaveLocation);
                if (data.Count == 0)
                {
                    MessageBox.Show("No se encontraron datos guardados para calcular el total esperado.\nPrimero debe procesar algunos PDFs para generar datos.",
                        "Sin Datos", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    logAction($"Total actualizado con {data.Count} lotes en la base de datos");
                }
            }
            catch (Exception ex)
            {
                logAction($"Error actualizando total: {ex.Message}");
                MessageBox.Show($"Error actualizando total: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CalculateCashDenomination(TextBox countTextBox)
        {
            try
            {
                // Determine denomination based on control name
                string controlName = countTextBox.Name;
                int denomination = 0;
                TextBox totalTextBox = null;

                switch (controlName)
                {
                    case "txt20000Count":
                        denomination = 20000;
                        totalTextBox = txt20000Total;
                        break;
                    case "txt10000Count":
                        denomination = 10000;
                        totalTextBox = txt10000Total;
                        break;
                    case "txt2000Count":
                        denomination = 2000;
                        totalTextBox = txt2000Total;
                        break;
                    case "txt1000Count":
                        denomination = 1000;
                        totalTextBox = txt1000Total;
                        break;
                    case "txt500Count":
                        denomination = 500;
                        totalTextBox = txt500Total;
                        break;
                    case "txt200Count":
                        denomination = 200;
                        totalTextBox = txt200Total;
                        break;
                    case "txt100Count":
                        denomination = 100;
                        totalTextBox = txt100Total;
                        break;
                }

                if (totalTextBox != null)
                {
                    double count = EvaluateCountExpression(countTextBox.Text);
                    if (count >= 0) // Valid expression
                    {
                        decimal total = (decimal)count * denomination;
                        totalTextBox.Text = $"$ {FormatColombianCurrency(total)}";
                    }
                    else
                    {
                        totalTextBox.Text = "$ 0";
                    }
                }
            }
            catch (Exception ex)
            {
                logAction($"Error calculando denominación: {ex.Message}");
            }
        }

        private void CalculateTotals()
        {
            try
            {
                decimal totalEfectivo = 0;
                decimal totalOtrosValores = 0;

                // Sum all denomination totals (cash only)
                totalEfectivo += GetDenominationTotal(txt20000Total);
                totalEfectivo += GetDenominationTotal(txt10000Total);
                totalEfectivo += GetDenominationTotal(txt2000Total);
                totalEfectivo += GetDenominationTotal(txt1000Total);
                totalEfectivo += GetDenominationTotal(txt500Total);
                totalEfectivo += GetDenominationTotal(txt200Total);
                totalEfectivo += GetDenominationTotal(txt100Total);

                // Sum additional values (other values) from their totals
                totalOtrosValores += GetDenominationTotal(txtValor1Total);
                totalOtrosValores += GetDenominationTotal(txtValor2Total);
                totalOtrosValores += GetDenominationTotal(txtValor3Total);
                totalOtrosValores += GetDenominationTotal(txtValor4Total);
                totalOtrosValores += GetDenominationTotal(txtValor5Total);

                // Update the new individual total displays
                if (txtTotalEfectivo != null)
                    txtTotalEfectivo.Text = $"$ {FormatColombianCurrency(totalEfectivo)}";
                
                if (txtTotalOtros != null)
                    txtTotalOtros.Text = $"$ {FormatColombianCurrency(totalOtrosValores)}";

                // Calculate combined total for compatibility
                decimal totalCombinado = totalEfectivo + totalOtrosValores;

                // Update total counted (combined total for existing functionality)
                if (txtTotalContado != null)
                    txtTotalContado.Text = $"$ {FormatColombianCurrency(totalCombinado)}";

                // Calculate difference
                decimal esperado = GetValueFromTextBox(txtTotalEsperado);
                decimal diferencia = totalCombinado - esperado;
                
                if (txtDiferencia != null)
                {
                    txtDiferencia.Text = $"$ {FormatColombianCurrency(diferencia)}";

                    // Update difference colors and text to show if it's surplus or deficit
                    UpdateDifferenceDisplay(diferencia);
                }
            }
            catch (Exception ex)
            {
                logAction($"Error calculando totales: {ex.Message}");
            }
        }

        private decimal GetDenominationTotal(TextBox totalTextBox)
        {
            try
            {
                if (totalTextBox == null) return 0;
                
                string text = totalTextBox.Text.Replace("$", "").Trim();
                return ParseColombianCurrency(text);
            }
            catch
            {
                return 0;
            }
        }

        private decimal GetValueFromTextBox(TextBox textBox)
        {
            try
            {
                if (textBox == null) return 0;
                
                string text = textBox.Text.Replace("$", "").Trim();

                // First try to evaluate as expression
                double evaluatedValue = EvaluateCountExpression(text);
                if (evaluatedValue >= 0) // Valid expression
                {
                    return (decimal)evaluatedValue;
                }

                // If not a valid expression, try parsing as Colombian currency
                return ParseColombianCurrency(text);
            }
            catch
            {
                return 0;
            }
        }

        private decimal ParseColombianCurrency(string text)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(text))
                    return 0;

                // Remove any currency symbols and extra spaces
                text = text.Replace("$", "").Replace("COP", "").Trim();

                // Handle Spanish format: periods as thousands separators, comma as decimal separator
                // Examples: "1.234.567", "1.234.567,89", "420.000", "12,50"
                
                // Count periods and commas to determine format
                int periodCount = text.Count(c => c == '.');
                int commaCount = text.Count(c => c == ',');
                
                if (commaCount == 0 && periodCount > 0)
                {
                    // Only periods - these are thousands separators (e.g., "420.000")
                    text = text.Replace(".", "");
                    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                }
                else if (commaCount == 1 && periodCount > 0)
                {
                    // Both periods and one comma - periods are thousands, comma is decimal
                    // e.g., "1.234.567,89"
                    var parts = text.Split(',');
                    var integerPart = parts[0].Replace(".", "");
                    var decimalPart = parts[1];
                    text = integerPart + "." + decimalPart;
                    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                }
                else if (commaCount == 1 && periodCount == 0)
                {
                    // Single comma - this is decimal separator (e.g., "12,50")
                    text = text.Replace(",", ".");
                    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                }
                else if (periodCount == 1 && commaCount == 0)
                {
                    // Single period - could be decimal separator or thousands separator
                    var parts = text.Split('.');
                    if (parts[1].Length <= 2)
                    {
                        // Likely decimal separator (e.g., "12.50")
                        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                    }
                    else
                    {
                        // Likely thousands separator (e.g., "12.500")
                        text = text.Replace(".", "");
                        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                    }
                }
                else
                {
                    // No special characters or multiple commas/periods - parse as is
                    return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal value) ? value : 0;
                }
            }
            catch (Exception ex)
            {
                logAction($"Error parsing currency '{text}': {ex.Message}");
                return 0;
            }
        }

        private void CalculateExpectedTotal()
        {
            try
            {
                // Verify all controls are available
                if (dpFechaInicial == null || dpFechaFinal == null || cmbCajaSelection == null || 
                    txtTotalEsperado == null || txtLoteHoy == null)
                {
                    logAction("Controles no están inicializados aún");
                    return;
                }

                if (dpFechaInicial.SelectedDate == null || dpFechaFinal.SelectedDate == null || 
                    cmbCajaSelection.SelectedItem == null)
                {
                    logAction("Fechas o caja no seleccionadas completamente");
                    txtTotalEsperado.Text = "0.00";
                    return;
                }

                DateTime fechaInicial = dpFechaInicial.SelectedDate.Value;
                DateTime fechaFinal = dpFechaFinal.SelectedDate.Value;

                // Validate initial date is not greater than final date
                if (fechaInicial > fechaFinal)
                {
                    logAction("Fecha inicial es mayor que fecha final");
                    txtTotalEsperado.Text = "0.00";
                    //txtTotalEsperado.Background = System.Windows.Media.Brushes.LightPink;
                    return;
                }
                else
                {
                    //txtTotalEsperado.Background = System.Windows.Media.Brushes.LightYellow;
                }

                string cajaSeleccionada = ((ComboBoxItem)cmbCajaSelection.SelectedItem).Content.ToString() ?? "";

                logAction($"Iniciando cálculo: {fechaInicial:dd/MM/yyyy} - {fechaFinal:dd/MM/yyyy} para {cajaSeleccionada}");

                // Calculate expected total from saved data
                decimal totalEsperado = CalculateExpectedCashFromData(fechaInicial, fechaFinal, cajaSeleccionada);

                // Add today's batch value
                decimal loteHoyValue = GetValueFromTextBox(txtLoteHoy);
                totalEsperado += loteHoyValue;

                // Update display
                txtTotalEsperado.Text = FormatColombianCurrency(totalEsperado);
                logAction($"Total esperado actualizado: ${FormatColombianCurrency(totalEsperado)} (incluye Lote de Hoy: ${FormatColombianCurrency(loteHoyValue)})");

                // Recalculate difference
                CalculateTotals();

                // Update background color based on whether there's data
                /*
                if (totalEsperado > 0)
                {
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightYellow;
                }
                */
            }
            catch (Exception ex)
            {
                logAction($"Error calculando total esperado: {ex.Message}");
                if (txtTotalEsperado != null)
                {
                    txtTotalEsperado.Text = "Error";
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightCoral;
                }
            }
        }

        private decimal CalculateExpectedCashFromData(DateTime fechaInicial, DateTime fechaFinal, string caja)
        {
            try
            {
                logAction($"Calculando total esperado para {caja} desde {fechaInicial:dd/MM/yyyy} hasta {fechaFinal:dd/MM/yyyy}");

                // Get configuration for data location
                var config = AppConfig.Load();
                if (string.IsNullOrEmpty(config.SaveLocation))
                {
                    logAction("No hay ubicación de guardado configurada");
                    return 0;
                }

                // Load saved data
                var allData = DataService.LoadData(config.SaveLocation);
                logAction($"Datos cargados: {allData.Count} lotes encontrados");

                if (allData.Count == 0)
                {
                    logAction("No hay datos guardados para calcular");
                    return 0;
                }

                // Filter by dates
                var filteredByDate = allData.Where(lote =>
                    lote.Fecha.Date >= fechaInicial.Date &&
                    lote.Fecha.Date <= fechaFinal.Date).ToList();

                logAction($"Después de filtrar por fechas: {filteredByDate.Count} lotes");

                // Filter by cash register if not "Both Registers"
                List<LoteData> filteredData;
                if (caja == "Ambas Cajas")
                {
                    filteredData = filteredByDate;
                    logAction("Incluyendo ambas cajas");
                }
                else
                {
                    // Normalize cash register name for comparison
                    string cajaToMatch = caja.ToUpper().Replace(" ", "");
                    filteredData = filteredByDate.Where(lote =>
                        lote.Caja.ToUpper().Replace(" ", "") == cajaToMatch ||
                        lote.Caja.ToUpper().Contains(cajaToMatch)).ToList();

                    logAction($"Después de filtrar por caja '{caja}': {filteredData.Count} lotes");
                }

                // Sum all cash amounts
                decimal totalEfectivo = filteredData.Sum(lote => lote.Efectivo);

                logAction($"Total efectivo calculado: ${totalEfectivo.ToString("N2", new CultureInfo("es-CO"))}");

                // Show details in debug
                if (filteredData.Count > 0)
                {
                    logAction("Detalle de lotes incluidos:");
                    foreach (var lote in filteredData.OrderBy(l => l.Fecha))
                    {
                        logAction($"  - {lote.FechaString} | {lote.Lote} | {lote.Caja} | Efectivo: ${lote.Efectivo.ToString("N2", new CultureInfo("es-CO"))}");
                    }
                }
                else
                {
                    logAction("No se encontraron lotes que cumplan los criterios");
                }

                return totalEfectivo;
            }
            catch (Exception ex)
            {
                logAction($"Error calculando total esperado desde datos: {ex.Message}");
                logAction($"StackTrace: {ex.StackTrace}");
                return 0;
            }
        }

        private string FormatColombianCurrency(decimal value)
        {
            // Format with Spanish convention: periods as thousands separators, comma as decimal separator
            // Examples: 1.234.567 or 1.234.567,89
            
            // For integers, don't show decimals
            if (value == Math.Floor(value))
            {
                // Use Spanish culture which naturally handles . for thousands
                var culture = new CultureInfo("es-ES");
                return value.ToString("N0", culture);
            }
            else
            {
                // For decimals, use Spanish format: . for thousands, , for decimals
                var culture = new CultureInfo("es-ES");
                return value.ToString("N2", culture);
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
                if (double.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out double simpleNumber))
                {
                    return simpleNumber;
                }

                // Check if it contains mathematical expressions
                if (System.Text.RegularExpressions.Regex.IsMatch(input, @"^[0-9+\-*/().]+$"))
                {
                    try
                    {
                        // Use DataTable.Compute for simple expression evaluation
                        var table = new System.Data.DataTable();
                        var result = table.Compute(input, null);

                        if (result == System.DBNull.Value)
                            return -1; // Invalid expression

                        return Convert.ToDouble(result);
                    }
                    catch
                    {
                        return -1; // Invalid expression
                    }
                }

                return -1; // Invalid format
            }
            catch
            {
                return -1; // Error
            }
        }

        private void LoadArqueoConfig()
        {
            try
            {
                if (File.Exists(CONFIG_FILE))
                {
                    var configData = File.ReadAllText(CONFIG_FILE);
                    using (var doc = JsonDocument.Parse(configData))
                    {
                        var root = doc.RootElement;

                        // Load saved values
                        if (root.TryGetProperty("CajaSeleccionada", out var cajaElement) && cmbCajaSelection != null)
                        {
                            string caja = cajaElement.GetString() ?? "";
                            for (int i = 0; i < cmbCajaSelection.Items.Count; i++)
                            {
                                if (cmbCajaSelection.Items[i] is ComboBoxItem item && item.Content.ToString() == caja)
                                {
                                    cmbCajaSelection.SelectedIndex = i;
                                    break;
                                }
                            }
                        }

                        if (root.TryGetProperty("FechaInicio", out var fechaElement) && dpFechaInicial != null)
                        {
                            if (DateTime.TryParse(fechaElement.GetString(), out DateTime fecha))
                                dpFechaInicial.SelectedDate = fecha;
                        }

                        if (root.TryGetProperty("LoteHoy", out var loteElement) && txtLoteHoy != null)
                            txtLoteHoy.Text = loteElement.GetString() ?? "";

                        // Load additional value labels
                        if (root.TryGetProperty("Etiqueta1", out var etiq1) && txtEtiqueta1 != null)
                            txtEtiqueta1.Text = etiq1.GetString() ?? "Tarjetas";
                        if (root.TryGetProperty("Etiqueta2", out var etiq2) && txtEtiqueta2 != null)
                            txtEtiqueta2.Text = etiq2.GetString() ?? "Cheques";
                        if (root.TryGetProperty("Etiqueta3", out var etiq3) && txtEtiqueta3 != null)
                            txtEtiqueta3.Text = etiq3.GetString() ?? "Transferencias";
                        if (root.TryGetProperty("Etiqueta4", out var etiq4) && txtEtiqueta4 != null)
                            txtEtiqueta4.Text = etiq4.GetString() ?? "Otros";
                        if (root.TryGetProperty("Etiqueta5", out var etiq5) && txtEtiqueta5 != null)
                            txtEtiqueta5.Text = etiq5.GetString() ?? "Varios";

                        // Load additional values
                        if (root.TryGetProperty("Valor1", out var val1) && txtValor1 != null)
                            txtValor1.Text = val1.GetString() ?? "0";
                        if (root.TryGetProperty("Valor2", out var val2) && txtValor2 != null)
                            txtValor2.Text = val2.GetString() ?? "0";
                        if (root.TryGetProperty("Valor3", out var val3) && txtValor3 != null)
                            txtValor3.Text = val3.GetString() ?? "0";
                        if (root.TryGetProperty("Valor4", out var val4) && txtValor4 != null)
                            txtValor4.Text = val4.GetString() ?? "0";
                        if (root.TryGetProperty("Valor5", out var val5) && txtValor5 != null)
                            txtValor5.Text = val5.GetString() ?? "0";
                    }
                }
            }
            catch (Exception ex)
            {
                logAction($"Error cargando configuración: {ex.Message}");
            }
        }

        private void SaveArqueoConfig()
        {
            try
            {
                var config = new
                {
                    CajaSeleccionada = cmbCajaSelection?.SelectedItem is ComboBoxItem selected ? selected.Content.ToString() : "",
                    FechaInicio = dpFechaInicial?.SelectedDate?.ToString("yyyy-MM-dd"),
                    LoteHoy = txtLoteHoy?.Text ?? "",
                    // Save additional value labels
                    Etiqueta1 = txtEtiqueta1?.Text ?? "Tarjetas",
                    Etiqueta2 = txtEtiqueta2?.Text ?? "Cheques",
                    Etiqueta3 = txtEtiqueta3?.Text ?? "Transferencias",
                    Etiqueta4 = txtEtiqueta4?.Text ?? "Otros",
                    Etiqueta5 = txtEtiqueta5?.Text ?? "Varios",
                    // Save additional values
                    Valor1 = txtValor1?.Text ?? "0",
                    Valor2 = txtValor2?.Text ?? "0",
                    Valor3 = txtValor3?.Text ?? "0",
                    Valor4 = txtValor4?.Text ?? "0",
                    Valor5 = txtValor5?.Text ?? "0"
                };

                File.WriteAllText(CONFIG_FILE, JsonSerializer.Serialize(config));
            }
            catch (Exception ex)
            {
                logAction($"Error guardando configuración: {ex.Message}");
            }
        }

        private void UpdateDifferenceDisplay(decimal diferencia)
        {
            try
            {
                if (borderDiferencia != null && txtDiferenciaLabel != null && txtDiferencia != null)
                {
                    if (diferencia == 0)
                    {
                        // Perfect balance - green
                        borderDiferencia.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(209, 250, 229)); // #D1FAE5
                        borderDiferencia.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)); // #10B981
                        txtDiferenciaLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(4, 120, 87)); // #047857
                        txtDiferencia.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(4, 120, 87)); // #047857
                        txtDiferenciaLabel.Text = "✅ Balanceado";
                    }
                    else if (diferencia > 0)
                    {
                        // Surplus - blue
                        borderDiferencia.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(219, 234, 254)); // #DBEAFE
                        borderDiferencia.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(59, 130, 246)); // #3B82F6
                        txtDiferenciaLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175)); // #1E40AF
                        txtDiferencia.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 64, 175)); // #1E40AF
                        txtDiferenciaLabel.Text = "💰 Sobrante";
                    }
                    else
                    {
                        // Deficit - red
                        borderDiferencia.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(254, 226, 226)); // #FEE2E2
                        borderDiferencia.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)); // #EF4444
                        txtDiferenciaLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 28, 28)); // #B91C1C
                        txtDiferencia.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(185, 28, 28)); // #B91C1C
                        txtDiferenciaLabel.Text = "⚠️ Faltante";
                    }
                }
            }
            catch (Exception ex)
            {
                logAction($"Error actualizando colores de diferencia: {ex.Message}");
            }
        }

        private void CalculateAdditionalValueTotals()
        {
            try
            {
                // Calculate individual totals for each additional value
                CalculateAdditionalValueTotal(txtValor1, txtValor1Total);
                CalculateAdditionalValueTotal(txtValor2, txtValor2Total);
                CalculateAdditionalValueTotal(txtValor3, txtValor3Total);
                CalculateAdditionalValueTotal(txtValor4, txtValor4Total);
                CalculateAdditionalValueTotal(txtValor5, txtValor5Total);
            }
            catch (Exception ex)
            {
                logAction($"Error calculando subtotales de valores adicionales: {ex.Message}");
            }
        }

        private void CalculateAdditionalValueTotal(TextBox valueTextBox, TextBox totalTextBox)
        {
            try
            {
                if (valueTextBox != null && totalTextBox != null)
                {
                    double value = EvaluateCountExpression(valueTextBox.Text);
                    if (value >= 0) // Valid expression
                    {
                        decimal total = (decimal)value;
                        totalTextBox.Text = $"$ {FormatColombianCurrency(total)}";
                    }
                    else
                    {
                        totalTextBox.Text = "$ 0";
                    }
                }
            }
            catch (Exception ex)
            {
                logAction($"Error calculando subtotal de valor adicional: {ex.Message}");
            }
        }

        private void UpdateCashTooltips()
        {
            try
            {
                // Update tooltips for cash denomination fields
                UpdateTooltipForCountField(txt20000Count);
                UpdateTooltipForCountField(txt10000Count);
                UpdateTooltipForCountField(txt2000Count);
                UpdateTooltipForCountField(txt1000Count);
                UpdateTooltipForCountField(txt500Count);
                UpdateTooltipForCountField(txt200Count);
                UpdateTooltipForCountField(txt100Count);
            }
            catch (Exception ex)
            {
                logAction($"Error actualizando tooltips de efectivo: {ex.Message}");
            }
        }

        private void UpdateAdditionalValueTooltips()
        {
            try
            {
                // Update tooltips for additional value fields
                UpdateTooltipForValueField(txtValor1);
                UpdateTooltipForValueField(txtValor2);
                UpdateTooltipForValueField(txtValor3);
                UpdateTooltipForValueField(txtValor4);
                UpdateTooltipForValueField(txtValor5);
            }
            catch (Exception ex)
            {
                logAction($"Error actualizando tooltips de valores adicionales: {ex.Message}");
            }
        }

        private void UpdateTooltipForCountField(TextBox textBox)
        {
            if (textBox == null) return;
            
            try
            {
                double result = EvaluateCountExpression(textBox.Text);
                if (result >= 0 && textBox.Text.Contains("*") || textBox.Text.Contains("+") || textBox.Text.Contains("-") || textBox.Text.Contains("/"))
                {
                    textBox.ToolTip = $"= {FormatColombianCurrency((decimal)result)}";
                }
                else
                {
                    textBox.ToolTip = null;
                }
            }
            catch
            {
                textBox.ToolTip = null;
            }
        }

        private void UpdateTooltipForValueField(TextBox textBox)
        {
            if (textBox == null) return;
            
            try
            {
                double result = EvaluateCountExpression(textBox.Text);
                if (result >= 0 && (textBox.Text.Contains("*") || textBox.Text.Contains("+") || textBox.Text.Contains("-") || textBox.Text.Contains("/")))
                {
                    textBox.ToolTip = $"= ${FormatColombianCurrency((decimal)result)}";
                }
                else
                {
                    textBox.ToolTip = null;
                }
            }
            catch
            {
                textBox.ToolTip = null;
            }
        }
    }
}