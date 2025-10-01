using System;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using PdfExtractor.Models;

namespace PdfExtractor.Services
{
    public static class PdfParser
    {
        /// <summary>
        /// Extract data using the original iText-based extractor (legacy method)
        /// </summary>
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
                    TotalRecibosMuni = extractedData.TotalRecibosMuni,
                    Credito = extractedData.TarjetaCredito,
                    Debito = extractedData.TarjetaDebito,
                    Cheque = extractedData.ChequeDiferido
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al analizar el PDF: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Extract data using the improved PdfPig extractor with robust pattern matching.
        /// This method implements best practices for OSM document processing including
        /// page concatenation, artifact removal, and intelligent conditional logic.
        /// </summary>
        public static LoteData ExtractDataWithPdfPig(string pdfFilePath)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[PdfParser] ExtractDataWithPdfPig called for: {Path.GetFileName(pdfFilePath)}");
                
                var extractor = new PdfPigExtractor();
                var result = extractor.ExtractData(pdfFilePath);
                
                System.Diagnostics.Debug.WriteLine($"[PdfParser] PdfPig raw results:");
                System.Diagnostics.Debug.WriteLine($"  Lote: '{result.Lote ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"  Usuario: '{result.Usuario ?? "NULL"}'");
                System.Diagnostics.Debug.WriteLine($"  OSM: {result.Osm?.ToString("N2") ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  MUNI: {result.Muni?.ToString("N2") ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  TarjetaCredito: {result.TarjetaCredito?.ToString("N2") ?? "NULL"}");
                System.Diagnostics.Debug.WriteLine($"  TarjetaDebito: {result.TarjetaDebito?.ToString("N2") ?? "NULL"}");
                
                var loteData = new LoteData
                {
                    Lote = result.Lote ?? "",
                    Usuario = result.Usuario ?? "",
                    OSM = result.Osm ?? 0m,
                    MUNI = result.Muni ?? 0m,
                    Credito = result.TarjetaCredito ?? 0m,
                    Debito = result.TarjetaDebito ?? 0m,
                    Cheque = result.ChequeDiferido ?? 0m,
                    Fecha = DateTime.Today // Will be updated by the UI if needed
                };
                
                System.Diagnostics.Debug.WriteLine($"[PdfParser] Final LoteData converted:");
                System.Diagnostics.Debug.WriteLine($"  OSM: {loteData.OSM:N2}");
                System.Diagnostics.Debug.WriteLine($"  MUNI: {loteData.MUNI:N2}");
                System.Diagnostics.Debug.WriteLine($"  Credito: {loteData.Credito:N2}");
                System.Diagnostics.Debug.WriteLine($"  Debito: {loteData.Debito:N2}");
                return loteData;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PdfParser] ERROR in ExtractDataWithPdfPig: {ex.Message}");
                throw new Exception($"Error al analizar el PDF con PdfPig: {ex.Message}", ex);
            }
        }
    }
}
