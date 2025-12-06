# Script de limpieza y rebuild para forzar recarga de analyzers
Write-Host "=== Fuxion Analyzer Rebuild Script ===" -ForegroundColor Cyan
Write-Host ""

# 1. Shutdown build servers
Write-Host "1. Cerrando build servers..." -ForegroundColor Yellow
dotnet build-server shutdown
Write-Host "   ? Build servers cerrados" -ForegroundColor Green
Write-Host ""

# 2. Clean solution
Write-Host "2. Limpiando solución..." -ForegroundColor Yellow
dotnet clean --configuration Debug
dotnet clean --configuration Release
Write-Host "   ? Solución limpiada" -ForegroundColor Green
Write-Host ""

# 3. Eliminar carpetas bin/obj de analyzers
Write-Host "3. Eliminando bin/obj de analyzers..." -ForegroundColor Yellow
$analyzerPaths = @(
    "src\Analyzers",
    "src\Analyzers.CodeFixes"
)

foreach ($path in $analyzerPaths) {
    if (Test-Path "$path\bin") {
        Remove-Item "$path\bin" -Recurse -Force
        Write-Host "   ? Eliminado $path\bin" -ForegroundColor Green
    }
    if (Test-Path "$path\obj") {
        Remove-Item "$path\obj" -Recurse -Force
        Write-Host "   ? Eliminado $path\obj" -ForegroundColor Green
    }
}
Write-Host ""

# 4. Rebuild analyzers
Write-Host "4. Recompilando analyzers..." -ForegroundColor Yellow
dotnet build src\Analyzers\Fuxion.Analyzers.csproj --configuration Debug
dotnet build src\Analyzers.CodeFixes\Fuxion.Analyzers.CodeFixes.csproj --configuration Debug
Write-Host "   ? Analyzers recompilados" -ForegroundColor Green
Write-Host ""

# 5. Rebuild Fuxion (contiene los atributos)
Write-Host "5. Recompilando Fuxion..." -ForegroundColor Yellow
dotnet build src\Fuxion\Fuxion.csproj --configuration Debug
Write-Host "   ? Fuxion recompilado" -ForegroundColor Green
Write-Host ""

# 6. Rebuild Pods (usa los analyzers)
Write-Host "6. Recompilando Pods..." -ForegroundColor Yellow
dotnet build src\Pods\Fuxion.Pods.csproj --configuration Debug
Write-Host "   ? Pods recompilado" -ForegroundColor Green
Write-Host ""

# 7. Build test project
Write-Host "7. Compilando proyecto de test..." -ForegroundColor Yellow
$buildOutput = dotnet build test\Pods\Fuxion.Pods.Test.csproj --configuration Debug 2>&1 | Out-String
Write-Host "   ? Test compilado" -ForegroundColor Green
Write-Host ""

# 8. Verificar errores FX001
Write-Host "8. Verificando errores del analyzer..." -ForegroundColor Yellow
if ($buildOutput -match "FX001") {
    Write-Host "   ? ANALYZER FUNCIONANDO - Se detectaron errores FX001" -ForegroundColor Green
    Write-Host ""
    Write-Host "Errores encontrados:" -ForegroundColor Cyan
    $buildOutput | Select-String "FX001" | ForEach-Object { Write-Host "   $_" -ForegroundColor White }
} else {
    Write-Host "   ??  No se detectaron errores FX001" -ForegroundColor Red
    Write-Host "   El analyzer puede no estar cargándose correctamente." -ForegroundColor Red
    Write-Host ""
    Write-Host "SOLUCIÓN:" -ForegroundColor Yellow
    Write-Host "1. Cierra Visual Studio COMPLETAMENTE" -ForegroundColor White
    Write-Host "2. Elimina la carpeta .vs (oculta en la raíz)" -ForegroundColor White
    Write-Host "3. Reabre Visual Studio" -ForegroundColor White
    Write-Host "4. Limpia y recompila la solución desde VS" -ForegroundColor White
}
Write-Host ""
Write-Host "=== Proceso completado ===" -ForegroundColor Cyan
