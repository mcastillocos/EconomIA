# ============================================================
# EconomIA - EXECUTE SQL SCRIPTS
# Verifica si los scripts SQL ya fueron ejecutados y, si no,
# los ejecuta contra SQL Server (Docker).
# Usa un archivo de control en .scripts_state/ para saber
# cuáles ya se han aplicado.
# ============================================================
param(
    [switch]$Force,
    [string]$Server = "localhost,1433",
    [string]$User = "sa",
    [string]$Password = "EconomIA_Dev2024!"
)

$ErrorActionPreference = "Continue"
$root = $PSScriptRoot | Split-Path -Parent
$sqlDir = Join-Path $root "sql"
$stateDir = Join-Path $root ".scripts_state"

Write-Host ""
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host "  EconomIA - Ejecutar Scripts SQL" -ForegroundColor Cyan
Write-Host "======================================================" -ForegroundColor Cyan
Write-Host ""

# ---- Crear carpeta de control si no existe ----
if (-not (Test-Path $stateDir)) {
    New-Item -ItemType Directory -Path $stateDir -Force | Out-Null
    Write-Host "  Creada carpeta de control: .scripts_state/" -ForegroundColor DarkGray
}

# ---- Verificar que sqlcmd está disponible ----
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) {
    # Intentar la ruta típica de SQL Server tools
    $sqlcmdPath = "C:\Program Files\Microsoft SQL Server\Client SDK\ODBC\170\Tools\Binn\sqlcmd.exe"
    if (Test-Path $sqlcmdPath) {
        $sqlcmd = $sqlcmdPath
    } else {
        Write-Host "  WARN: sqlcmd no encontrado. Intentando con docker exec..." -ForegroundColor Yellow
        $useSqlcmd = $false
    }
}
if ($sqlcmd) { $useSqlcmd = $true }

# ---- Obtener scripts SQL ordenados ----
$scripts = Get-ChildItem -Path $sqlDir -Filter "*.sql" | Sort-Object Name

if ($scripts.Count -eq 0) {
    Write-Host "  No hay scripts SQL en $sqlDir" -ForegroundColor DarkGray
    exit 0
}

Write-Host "  Scripts encontrados: $($scripts.Count)" -ForegroundColor White
Write-Host "  Servidor: $Server" -ForegroundColor DarkGray
Write-Host ""

$applied = 0
$skipped = 0

foreach ($script in $scripts) {
    $stateFile = Join-Path $stateDir "$($script.BaseName).done"

    if ((Test-Path $stateFile) -and (-not $Force)) {
        Write-Host "  [SKIP] $($script.Name) (ya aplicado)" -ForegroundColor DarkGray
        $skipped++
        continue
    }

    Write-Host "  [EXEC] $($script.Name)..." -ForegroundColor Yellow -NoNewline

    $exitCode = 1
    if ($useSqlcmd) {
        # Ejecutar con sqlcmd local
        $result = sqlcmd -S $Server -U $User -P $Password -i $script.FullName -b 2>&1
        $exitCode = $LASTEXITCODE
    } else {
        # Ejecutar vía docker exec dentro del contenedor sqlserver
        $containerName = docker ps --filter "name=economia" --filter "ancestor=mcr.microsoft.com/mssql/server" --format "{{.Names}}" 2>$null | Select-Object -First 1
        if (-not $containerName) {
            $containerName = docker ps --filter "expose=1433" --format "{{.Names}}" 2>$null | Select-Object -First 1
        }

        if (-not $containerName) {
            Write-Host " ERROR" -ForegroundColor Red
            Write-Host "    No se encontro contenedor SQL Server activo." -ForegroundColor Red
            Write-Host "    Ejecuta primero: .\scripts\start_app.ps1" -ForegroundColor Yellow
            exit 1
        }

        # Copiar script al contenedor y ejecutar
        docker cp $script.FullName "${containerName}:/tmp/$($script.Name)" 2>&1 | Out-Null
        $result = docker exec $containerName /opt/mssql-tools/bin/sqlcmd -S localhost -U $User -P $Password -i "/tmp/$($script.Name)" -b 2>&1
        $exitCode = $LASTEXITCODE

        # Intentar con la ruta alternativa de mssql-tools18
        if ($exitCode -ne 0) {
            $result = docker exec $containerName /opt/mssql-tools18/bin/sqlcmd -S localhost -U $User -P $Password -i "/tmp/$($script.Name)" -b -C 2>&1
            $exitCode = $LASTEXITCODE
        }
    }

    if ($exitCode -eq 0) {
        # Marcar como ejecutado
        Get-Date -Format "yyyy-MM-dd HH:mm:ss" | Out-File $stateFile -Force
        Write-Host " OK" -ForegroundColor Green
        $applied++
    } else {
        Write-Host " ERROR" -ForegroundColor Red
        Write-Host "    $result" -ForegroundColor Red
        Write-Host ""
        Write-Host "  Usa -Force para reintentar scripts ya marcados." -ForegroundColor Yellow
        exit 1
    }
}

Write-Host ""
Write-Host "------------------------------------------------------" -ForegroundColor DarkGray
Write-Host "  Resultado: $applied aplicados, $skipped ya estaban" -ForegroundColor White
Write-Host "------------------------------------------------------" -ForegroundColor DarkGray
Write-Host ""
