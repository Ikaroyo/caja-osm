using System;
using System.Globalization;
using System.Text.RegularExpressions;
using PdfExtractor.Models;

namespace PdfExtractor.Services
{
    public static class PdfParser
    {
        public static LoteData ExtractData(string pdfText)
        {
            try
            {
                using var extractor = new PdfDataExtractor();
                var extractedData = extractor.ExtractFromText(pdfText);
                
                return new LoteData
                {
                    Lote = extractedData.Lote,
                    Fecha = extractedData.Fecha,
                    Usuario = extractedData.Usuario,
                    OSM = extractedData.TotalOSM,
                    MUNI = extractedData.TotalMunicipalidad,
                    Credito = extractedData.TarjetaCredito,
                    Debito = extractedData.TarjetaDebito,
                    Cheque = extractedData.Cheque
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al analizar el PDF: {ex.Message}", ex);
            }
        }
    }
}
