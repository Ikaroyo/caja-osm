using System;
using System.ComponentModel;
using System.Globalization;

namespace PdfExtractor.Models
{
    public class LoteData : INotifyPropertyChanged
    {
        private string _dia = "";
        private DateTime _fecha;
        private string _lote = "";
        private decimal _osm;
        private decimal _muni;
        private decimal _credito;
        private decimal _debito;
        private decimal _cheque;
        private decimal _efectivo;
        private string _caja = "CAJA 1";
        private string _usuario = ""; // Nueva propiedad
        private bool _depositado = false; // Nueva propiedad
        private string _observaciones = ""; // Campo Observaciones
        private bool _isSelected = false; // Para selección en lista

        public string Dia
        {
            get => _dia;
            set { _dia = value; OnPropertyChanged(nameof(Dia)); }
        }

        public DateTime Fecha
        {
            get => _fecha;
            set { _fecha = value; OnPropertyChanged(nameof(Fecha)); OnPropertyChanged(nameof(FechaString)); UpdateDia(); }
        }

        public string FechaString => _fecha.ToString("dd/MM/yyyy");

        public string Lote
        {
            get => _lote;
            set { _lote = value; OnPropertyChanged(nameof(Lote)); }
        }

        public string Caja
        {
            get => _caja;
            set { _caja = value; OnPropertyChanged(nameof(Caja)); }
        }

        public string Usuario
        {
            get => _usuario;
            set { _usuario = value; OnPropertyChanged(nameof(Usuario)); }
        }

        public bool Depositado
        {
            get => _depositado;
            set { _depositado = value; OnPropertyChanged(nameof(Depositado)); OnPropertyChanged(nameof(EstadoDepositado)); }
        }

        public string EstadoDepositado => _depositado ? "Depositado" : "Pendiente";

        public string Observaciones
        {
            get => _observaciones;
            set { _observaciones = value; OnPropertyChanged(nameof(Observaciones)); }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        // Notificar cambios en todas las propiedades formateadas cuando cambian los valores base
        public decimal OSM
        {
            get => _osm;
            set { _osm = value; OnPropertyChanged(nameof(OSM)); OnPropertyChanged(nameof(OSMFormatted)); CalcularEfectivo(); }
        }

        public decimal MUNI
        {
            get => _muni;
            set { _muni = value; OnPropertyChanged(nameof(MUNI)); OnPropertyChanged(nameof(MUNIFormatted)); }
        }

        public decimal Credito
        {
            get => _credito;
            set { _credito = value; OnPropertyChanged(nameof(Credito)); OnPropertyChanged(nameof(CreditoFormatted)); CalcularEfectivo(); }
        }

        public decimal Debito
        {
            get => _debito;
            set { _debito = value; OnPropertyChanged(nameof(Debito)); OnPropertyChanged(nameof(DebitoFormatted)); CalcularEfectivo(); }
        }

        public decimal Cheque
        {
            get => _cheque;
            set { _cheque = value; OnPropertyChanged(nameof(Cheque)); OnPropertyChanged(nameof(ChequeFormatted)); CalcularEfectivo(); }
        }

        public decimal Efectivo
        {
            get => _efectivo;
            set { 
                _efectivo = value; 
                OnPropertyChanged(nameof(Efectivo));
                OnPropertyChanged(nameof(EfectivoFormatted));
            }
        }

        // Propiedades formateadas para mostrar - CORREGIDAS
        public string OSMFormatted 
        { 
            get => FormatCurrency(OSM);
        }
        
        public string MUNIFormatted 
        { 
            get => FormatCurrency(MUNI);
        }
        
        public string CreditoFormatted 
        { 
            get => FormatCurrency(Credito);
        }
        
        public string DebitoFormatted 
        { 
            get => FormatCurrency(Debito);
        }
        
        public string ChequeFormatted 
        { 
            get => FormatCurrency(Cheque);
        }
        
        public string EfectivoFormatted 
        { 
            get => FormatCurrency(Efectivo);
        }
        
        public string CreditoDebitoFormatted 
        { 
            get => FormatCurrency(Credito + Debito);
        }

        private void CalcularEfectivo()
        {
            try
            {
                _efectivo = OSM - Credito - Debito - Cheque;
                OnPropertyChanged(nameof(Efectivo));
                OnPropertyChanged(nameof(EfectivoFormatted));
                OnPropertyChanged(nameof(CreditoDebitoFormatted));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating Efectivo: {ex.Message}");
            }
        }

        private void UpdateDia()
        {
            try
            {
                if (_fecha != DateTime.MinValue)
                {
                    var culture = new System.Globalization.CultureInfo("es-ES");
                    Dia = culture.DateTimeFormat.GetDayName(_fecha.DayOfWeek).ToUpper();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating day: {ex.Message}");
                Dia = "";
            }
        }

        private string FormatCurrency(decimal value)
        {
            if (value == 0) return "";
            // Usar punto como separador de miles y coma como decimal
            return value.ToString("C2", new CultureInfo("es-AR")).Replace("$", "$ ");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        
        // Cambiar a público para que sea accesible desde otras clases
        public virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Método público para forzar actualización desde fuera - RENOMBRADO
        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LoteData()
        {
            System.Diagnostics.Debug.WriteLine("LoteData constructor called");
            try
            {
                // Inicializar con valores por defecto seguros
                _dia = "";
                _fecha = DateTime.MinValue;
                _lote = "";
                _osm = 0;
                _muni = 0;
                _credito = 0;
                _debito = 0;
                _cheque = 0;
                _efectivo = 0;
                _caja = "CAJA 1"; // Inicializar caja
                _usuario = "";
                _depositado = false;
                _observaciones = ""; // Inicializar observaciones
                System.Diagnostics.Debug.WriteLine("LoteData constructor completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in LoteData constructor: {ex}");
                throw;
            }
        }
    }
}
