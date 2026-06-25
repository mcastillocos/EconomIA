<#
.SYNOPSIS
    Despliega EconomIA en la VM Azure remota (Standard_B1s, 1GB RAM).

.DESCRIPTION
    Este script:
    1. Configura Docker context para apuntar al servidor Azure
    2. Copia los archivos necesarios al servidor remoto
    3. Construye y levanta los contenedores del perfil seleccionado

.PARAMETER Profile
    Perfil a desplegar: core (default), db, monitor
    - core: API + Redis + Frontend (~400 MB)
    - db: SQL Server (~500 MB) - NO combinar con core en 1GB
    - monitor: Prometheus + Grafana (~300 MB)

.PARAMETER Action
    Acción: up (default), down, logs, status

.EXAMPLE
    .\deploy_azure.ps1 -Profile core -Action up
    .\deploy_azure.ps1 -Action logs
    .\deploy_azure.ps1 -Action down
#>

param(
    [ValidateSet("core", "db", "monitor")]
    [string]$Profile = "core",

    [ValidateSet("up", "down", "logs", "status", "build")]
    [string]$Action = "up"
)

$ErrorActionPreference = "Stop"

$REMOTE_HOST = "20.203.185.54"
$REMOTE_USER = "azureuser"
$SSH_KEY = "$env:USERPROFILE\.ssh\docker-lab_key.pem"
$REMOTE_DIR = "/home/azureuser/economia"
$COMPOSE_FILE = "docker/docker-compose.azure.yml"
$CONTEXT_NAME = "azure-docker"

function Write-Step($msg) {
    Write-Host "`n==> $msg" -ForegroundColor Cyan
}

function Test-SSHConnection {
    Write-Step "Verificando conexion SSH..."
    $result = ssh -i $SSH_KEY -o StrictHostKeyChecking=no -o ConnectTimeout=5 "${REMOTE_USER}@${REMOTE_HOST}" "echo OK" 2>&1
    if ($result -ne "OK") {
        Write-Host "ERROR: No se pudo conectar al servidor. Verifica:" -ForegroundColor Red
        Write-Host "  - IP: $REMOTE_HOST" -ForegroundColor Yellow
        Write-Host "  - Clave: $SSH_KEY" -ForegroundColor Yellow
        Write-Host "  - Puerto 22 abierto en NSG" -ForegroundColor Yellow
        exit 1
    }
    Write-Host "Conexion SSH OK" -ForegroundColor Green
}

function Set-DockerContext {
    Write-Step "Configurando Docker context remoto..."
    $contexts = docker context ls --format "{{.Name}}" 2>$null
    if ($contexts -notcontains $CONTEXT_NAME) {
        docker context create $CONTEXT_NAME --docker "host=ssh://docker-azure"
    }
    docker context use $CONTEXT_NAME
    Write-Host "Docker context: $CONTEXT_NAME (remoto)" -ForegroundColor Green
}

function Copy-ProjectFiles {
    Write-Step "Copiando archivos al servidor remoto..."

    # Crear directorio remoto
    ssh -i $SSH_KEY "${REMOTE_USER}@${REMOTE_HOST}" "mkdir -p $REMOTE_DIR"

    # Copiar archivos necesarios (solo lo mínimo para build)
    $filesToCopy = @(
        "docker/docker-compose.azure.yml",
        "docker/Dockerfile.api",
        "docker/Dockerfile.frontend",
        "frontend/",
        "src/",
        "observability/prometheus.yml"
    )

    foreach ($item in $filesToCopy) {
        $localPath = Join-Path $PSScriptRoot ".." $item
        if (Test-Path $localPath) {
            $remotePath = "$REMOTE_DIR/$item"
            $remoteParent = Split-Path $remotePath -Parent
            ssh -i $SSH_KEY "${REMOTE_USER}@${REMOTE_HOST}" "mkdir -p $remoteParent"

            if ((Get-Item $localPath).PSIsContainer) {
                # Es directorio - usar scp recursivo
                scp -i $SSH_KEY -r $localPath "${REMOTE_USER}@${REMOTE_HOST}:${remotePath}" 2>$null
            } else {
                scp -i $SSH_KEY $localPath "${REMOTE_USER}@${REMOTE_HOST}:${remotePath}" 2>$null
            }
            Write-Host "  Copiado: $item" -ForegroundColor DarkGray
        }
    }
    Write-Host "Archivos sincronizados" -ForegroundColor Green
}

function Invoke-DockerAction {
    param([string]$act, [string]$prof)

    switch ($act) {
        "up" {
            Write-Step "Levantando perfil '$prof'..."
            docker compose -f $COMPOSE_FILE --profile $prof up -d --build
            Write-Host "`nServicios levantados:" -ForegroundColor Green
            docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
        }
        "down" {
            Write-Step "Parando todos los servicios..."
            docker compose -f $COMPOSE_FILE --profile core --profile db --profile monitor down
        }
        "logs" {
            docker compose -f $COMPOSE_FILE --profile $prof logs -f --tail 50
        }
        "status" {
            Write-Step "Estado de contenedores:"
            docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}"
            Write-Host "`nUso de memoria:" -ForegroundColor Cyan
            docker stats --no-stream --format "table {{.Name}}\t{{.MemUsage}}\t{{.CPUPerc}}"
        }
        "build" {
            Write-Step "Construyendo imagenes del perfil '$prof'..."
            docker compose -f $COMPOSE_FILE --profile $prof build
        }
    }
}

# ==== MAIN ====
Write-Host "========================================" -ForegroundColor Magenta
Write-Host "  EconomIA - Deploy Azure VM" -ForegroundColor Magenta
Write-Host "  Perfil: $Profile | Accion: $Action" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

Test-SSHConnection
Set-DockerContext

if ($Action -eq "up" -or $Action -eq "build") {
    Copy-ProjectFiles
}

# Cambiar al directorio remoto para el compose
Push-Location (Join-Path $PSScriptRoot "..")
try {
    Invoke-DockerAction -act $Action -prof $Profile
} finally {
    Pop-Location
}

Write-Host "`nHecho." -ForegroundColor Green
