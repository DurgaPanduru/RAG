# RAG Application - Development Environment Setup Script
# PowerShell script for Windows

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "RAG Application - Development Setup" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check Docker
try {
    $dockerVersion = docker --version
    Write-Host "[OK] Docker: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "[OK] .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "[WARN] .NET SDK not found (needed for local development)" -ForegroundColor Yellow
}

# Check Node.js
try {
    $nodeVersion = node --version
    Write-Host "[OK] Node.js: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Node.js not found (needed for local development)" -ForegroundColor Yellow
}

# Check Python
try {
    $pythonVersion = python --version
    Write-Host "[OK] Python: $pythonVersion" -ForegroundColor Green
} catch {
    Write-Host "[WARN] Python not found (needed for local development)" -ForegroundColor Yellow
}

Write-Host "`n========================================" -ForegroundColor Cyan

# Check for .env file
if (!(Test-Path ".env")) {
    Write-Host "Creating .env file from .env.example..." -ForegroundColor Yellow
    Copy-Item ".env.example" ".env"
    Write-Host "[IMPORTANT] Please edit .env and add your ANTHROPIC_API_KEY" -ForegroundColor Red
    Write-Host "Get your API key from: https://console.anthropic.com/`n" -ForegroundColor Yellow

    # Pause for user to add API key
    Write-Host "Press any key after you've added your API key to .env..." -ForegroundColor Yellow
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
} else {
    Write-Host "[OK] .env file exists" -ForegroundColor Green
}

# Pull Docker images
Write-Host "`nPulling Docker images (this may take a while)..." -ForegroundColor Yellow
docker-compose pull

# Build services
Write-Host "`nBuilding services..." -ForegroundColor Yellow
docker-compose build

# Start services
Write-Host "`nStarting all services..." -ForegroundColor Yellow
docker-compose up -d

# Wait for services to be ready
Write-Host "`nWaiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 20

# Check service health
Write-Host "`nChecking service health..." -ForegroundColor Yellow

$services = @{
    "API Gateway" = "http://localhost:5000"
    "Backend API" = "http://localhost:5001"
    "LangChain Service" = "http://localhost:8000/v1/health"
    "Grafana" = "http://localhost:3000"
    "Prometheus" = "http://localhost:9090"
    "MinIO Console" = "http://localhost:9001"
    "Seq Logs" = "http://localhost:5341"
}

foreach ($service in $services.GetEnumerator()) {
    try {
        $response = Invoke-WebRequest -Uri $service.Value -UseBasicParsing -TimeoutSec 5
        Write-Host "[OK] $($service.Key): $($service.Value)" -ForegroundColor Green
    } catch {
        Write-Host "[WARN] $($service.Key): Not ready yet" -ForegroundColor Yellow
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================`n" -ForegroundColor Cyan

Write-Host "Access the application:" -ForegroundColor Cyan
Write-Host "  Frontend:        http://localhost:4200" -ForegroundColor White
Write-Host "  API Gateway:     http://localhost:5000" -ForegroundColor White
Write-Host "  API Docs:        http://localhost:5001/swagger" -ForegroundColor White
Write-Host "  Grafana:         http://localhost:3000 (admin/admin)" -ForegroundColor White
Write-Host "  Prometheus:      http://localhost:9090" -ForegroundColor White
Write-Host "  MinIO Console:   http://localhost:9001 (minioadmin/minioadmin)" -ForegroundColor White
Write-Host "  Seq Logs:        http://localhost:5341`n" -ForegroundColor White

Write-Host "View logs:" -ForegroundColor Cyan
Write-Host "  docker-compose logs -f`n" -ForegroundColor White

Write-Host "Stop services:" -ForegroundColor Cyan
Write-Host "  docker-compose down`n" -ForegroundColor White
