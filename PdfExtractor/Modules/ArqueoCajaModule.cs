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
                CalculateTotals();
            }
            catch (Exception ex)
            {
                logAction($"Error en Cash count changed: {ex.Message}");
            }
        }

        public void OnAdditionalValueChanged()
        {
            try
            {
                CalculateTotals();
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

                // Sum additional values (other values)
                totalOtrosValores += GetValueFromTextBox(txtValor1);
                totalOtrosValores += GetValueFromTextBox(txtValor2);
                totalOtrosValores += GetValueFromTextBox(txtValor3);
                totalOtrosValores += GetValueFromTextBox(txtValor4);
                totalOtrosValores += GetValueFromTextBox(txtValor5);

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

                    // Change difference color based on result
                    if (diferencia == 0)
                    {
                        txtDiferencia.Background = System.Windows.Media.Brushes.LightGreen;
                    }
                    else if (diferencia > 0)
                    {
                        txtDiferencia.Background = System.Windows.Media.Brushes.LightBlue;
                    }
                    else
                    {
                        txtDiferencia.Background = System.Windows.Media.Brushes.LightCoral;
                    }
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
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightPink;
                    return;
                }
                else
                {
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightYellow;
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
                if (totalEsperado > 0)
                {
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightGreen;
                }
                else
                {
                    txtTotalEsperado.Background = System.Windows.Media.Brushes.LightYellow;
                }
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
                    LoteHoy = txtLoteHoy?.Text ?? ""
                };

                File.WriteAllText(CONFIG_FILE, JsonSerializer.Serialize(config));
            }
            catch (Exception ex)
            {
                logAction($"Error guardando configuración: {ex.Message}");
            }
        }
    }
}