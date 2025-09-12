Add-Type -AssemblyName "PdfExtractor\bin\Debug\net8.0-windows\PdfExtractor.dll"
Add-Type -AssemblyName "PdfExtractor\bin\Debug\net8.0-windows\itext.kernel.dll"
Add-Type -AssemblyName "PdfExtractor\bin\Debug\net8.0-windows\itext.io.dll"

try {
    Write-Host "Cargando extractor de PDF..."
    
    # Usar reflection para cargar y ejecutar
    $assemblyPath = ".\PdfExtractor\bin\Debug\net8.0-windows\PdfExtractor.dll"
    $assembly = [System.Reflection.Assembly]::LoadFrom($assemblyPath)
    
    $extractorType = $assembly.GetType("PdfExtractor.Services.PdfDataExtractor")
    $extractor = [Activator]::CreateInstance($extractorType)
    
    $pdfPath = ".\getjobid484197.pdf"
    Write-Host "Extrayendo datos de: $pdfPath"
    
    $method = $extractorType.GetMethod("ExtractFromFile")
    $result = $method.Invoke($extractor, @($pdfPath))
    
    Write-Host "Datos extraídos:"
    Write-Host "Lote: $($result.Lote)"
    Write-Host "OSM: $($result.TotalOSM)"
    Write-Host "MUNI: $($result.TotalMunicipalidad)"
    
} catch {
    Write-Host "Error: $($_.Exception.Message)"
    Write-Host $_.Exception.StackTrace
}

Read-Host "Presiona Enter para continuar"