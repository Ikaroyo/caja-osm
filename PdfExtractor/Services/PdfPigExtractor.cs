using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace PdfExtractor.Services
{
    public static class PdfPigExtractor
    {
        // Encuentra el número más cercano alrededor de un ancla, usando coordenadas
        public static (decimal muni, decimal osm, string debug) ExtractAnchoredTotals(byte[] pdfBytes)
        {
            var debug = new System.Text.StringBuilder();
            decimal muni = 0, osm = 0;

            using (var doc = PdfDocument.Open(pdfBytes))
            {
                foreach (var page in doc.GetPages())
                {
                    var words = page.GetWords().ToList();
                    // Buscar anclas más específicas
                    var muniAnchors = words.Where(w => 
                        w.Text.Equals("Municipalidad", StringComparison.OrdinalIgnoreCase) ||
                        w.Text.Contains("Muni", StringComparison.OrdinalIgnoreCase) ||
                        w.Text.Contains("Municipal", StringComparison.OrdinalIgnoreCase)).ToList();
                    
                    var osmAnchors = words.Where(w => 
                        w.Text.Contains("Obras", StringComparison.OrdinalIgnoreCase) || 
                        w.Text.Contains("Sanitarias", StringComparison.OrdinalIgnoreCase) ||
                        w.Text.Contains("OSM", StringComparison.OrdinalIgnoreCase)).ToList();

                    if (muniAnchors.Count > 0)
                    {
                        muni = FindClosestAmountNear(words, muniAnchors);
                        debug.AppendLine($"PdfPig MUNI: {muni:N2} en página {page.Number}");
                    }
                    if (osmAnchors.Count > 0)
                    {
                        osm = FindClosestAmountNear(words, osmAnchors);
                        debug.AppendLine($"PdfPig OSM: {osm:N2} en página {page.Number}");
                    }

                    if (muni > 0 && osm > 0) break;
                }
            }

            return (muni, osm, debug.ToString());
        }

        private static decimal FindClosestAmountNear(List<Word> words, List<Word> anchors)
        {
            // Buscar valores en un radio de 300pts alrededor del ancla más cercana
            var regex = new Regex(@"^[\d]{1,3}(?:[.,][\d]{3})*[.,]\d{2}$");
            decimal best = 0;
            double bestDist = double.MaxValue;

            foreach (var anchor in anchors)
            {
                var ax = (anchor.BoundingBox.Left + anchor.BoundingBox.Right) / 2.0;
                var ay = (anchor.BoundingBox.Bottom + anchor.BoundingBox.Top) / 2.0;
                foreach (var w in words)
                {
                    if (!regex.IsMatch(w.Text)) continue;
                    var wx = (w.BoundingBox.Left + w.BoundingBox.Right) / 2.0;
                    var wy = (w.BoundingBox.Bottom + w.BoundingBox.Top) / 2.0;
                    double d = Math.Sqrt((wx - ax) * (wx - ax) + (wy - ay) * (wy - ay));
                    if (d < bestDist && d <= 300) // 300 pts ~ 4.2 inches
                    {
                        if (TryParseAmount(w.Text, out var val) && val > 1000) // Solo montos significativos
                        {
                            best = val; bestDist = d;
                        }
                    }
                }
            }
            return best;
        }

        private static bool TryParseAmount(string s, out decimal value)
        {
            s = s.Trim();
            var culture = s.Contains(',') ? new CultureInfo("es-AR") : CultureInfo.InvariantCulture;
            return decimal.TryParse(s, NumberStyles.Any, culture, out value);
        }
    }
}
