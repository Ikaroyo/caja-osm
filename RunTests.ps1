# Script para probar extracción de PDFs
Write-Host "Iniciando pruebas de extracción de PDFs..."

$testCases = @(
    @{ File = "getjobid484197.pdf"; Lote = "403279116"; OSM = 1478575.98; MUNI = 33016.1; Usuario = "CMAGALLANES" },
    @{ File = "getjobid485764.pdf"; Lote = "403279119"; OSM = 3243111.06; MUNI = 234994.47; Usuario = "FGONZALEZ" },
    @{ File = "getjobid485763.pdf"; Lote = "403279120"; OSM = 5159825.54; MUNI = 223836.74; Usuario = "SCRESPO" },
    @{ File = "getjobid485773.pdf"; Lote = "403279121"; OSM = 6154845.11; MUNI = 356596.81; Usuario = "FGONZALEZ" }
)

$results = @()

foreach ($testCase in $testCases) {
    Write-Host "Procesando $($testCase.File)..."
    $results += "Archivo: $($testCase.File)"
    $results += "Esperado - Lote: $($testCase.Lote), OSM: $($testCase.OSM), MUNI: $($testCase.MUNI), Usuario: $($testCase.Usuario)"
    $results += "---"
}

# Guardar resultados
$results | Out-File -FilePath "test_results_manual.txt" -Encoding UTF8
Write-Host "Resultados guardados en test_results_manual.txt"

# Intentar ejecutar extracción directa usando el ejecutable compilado
try {
    $exePath = ".\PdfExtractor\bin\Debug\net8.0-windows\PdfExtractor.exe"
    if (Test-Path $exePath) {
        Write-Host "Ejecutando $exePath..."
        Start-Process -FilePath $exePath -Wait -WindowStyle Minimized -TimeoutSec 10
    }
    else {
        Write-Host "Ejecutable no encontrado en $exePath"
    }
}
catch {
    Write-Host "Error ejecutando aplicación: $($_.Exception.Message)"
}

Write-Host "Pruebas completadas."
Read-Host "Presiona Enter para continuar"