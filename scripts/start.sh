#!/bin/bash
# ============================================================
# EconomIA - Script de inicio rápido
# ============================================================
set -e

echo "🚀 EconomIA - Iniciando entorno de desarrollo..."
echo ""

# ---- Verificar dependencias ----
echo "📋 Verificando dependencias..."

command -v docker >/dev/null 2>&1 || { echo "❌ Docker no instalado"; exit 1; }
command -v dotnet >/dev/null 2>&1 || { echo "❌ .NET SDK no instalado"; exit 1; }
command -v node >/dev/null 2>&1 || { echo "❌ Node.js no instalado"; exit 1; }

echo "✅ Docker: $(docker --version | head -1)"
echo "✅ .NET: $(dotnet --version)"
echo "✅ Node: $(node --version)"
echo ""

# ---- Modo Docker Compose ----
if [ "$1" == "docker" ] || [ "$1" == "" ]; then
    echo "🐳 Levantando infraestructura con Docker Compose..."
    cd docker
    docker compose up -d sqlserver redis kafka zookeeper vault otel-collector prometheus loki tempo grafana kafka-ui
    echo ""
    echo "⏳ Esperando a que SQL Server esté listo..."
    sleep 15
    echo ""
    echo "🔨 Aplicando migraciones..."
    cd ../src
    dotnet ef database update --project EconomIA.Infrastructure --startup-project EconomIA.API || echo "⚠️  Instala dotnet-ef: dotnet tool install --global dotnet-ef"
    echo ""
    echo "✅ Infraestructura lista!"
fi

# ---- Modo K3d ----
if [ "$1" == "k3d" ]; then
    command -v k3d >/dev/null 2>&1 || { echo "❌ k3d no instalado. Instala: curl -s https://raw.githubusercontent.com/k3d-io/k3d/main/install.sh | bash"; exit 1; }
    command -v kubectl >/dev/null 2>&1 || { echo "❌ kubectl no instalado"; exit 1; }

    echo "☸️  Creando cluster K3d..."
    k3d cluster create economia \
        --servers 1 \
        --agents 3 \
        --port "80:80@loadbalancer" \
        --port "443:443@loadbalancer" \
        --registry-create economia-registry:0.0.0.0:5050

    echo ""
    echo "🏗️  Construyendo imágenes..."
    docker build -t economia-registry:5050/economia-api:latest -f docker/Dockerfile.api .
    docker build -t economia-registry:5050/economia-frontend:latest -f docker/Dockerfile.frontend .
    docker push economia-registry:5050/economia-api:latest
    docker push economia-registry:5050/economia-frontend:latest

    echo ""
    echo "📦 Aplicando manifiestos K8s..."
    kubectl apply -k k8s/overlays/dev

    echo ""
    echo "✅ Cluster K3d listo!"
    echo "   Frontend: http://economia.localhost"
    echo "   Grafana:  http://grafana.economia.localhost"
fi

echo ""
echo "============================================================"
echo "  🌐 Accesos:"
echo "    API:       http://localhost:5000/swagger"
echo "    Frontend:  http://localhost:3000"
echo "    Grafana:   http://localhost:3001 (admin/economia)"
echo "    Kafka UI:  http://localhost:8080"
echo "    Vault:     http://localhost:8200 (token: economia-dev-token)"
echo "============================================================"
