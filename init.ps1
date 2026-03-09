# Script khởi tạo Online Exam System
# Chạy với: powershell -ExecutionPolicy Bypass -File init.ps1

Write-Host "=== Online Exam System - Initialization Script ===" -ForegroundColor Cyan
Write-Host ""

# Check .NET SDK
Write-Host "Checking .NET SDK..." -ForegroundColor Yellow
$dotnetVersion = dotnet --version
Write-Host "✓ .NET version: $dotnetVersion" -ForegroundColor Green

# Check if docker-compose.yml exists
if (Test-Path "docker-compose.yml") {
    Write-Host ""
    Write-Host "Starting Docker containers..." -ForegroundColor Yellow
    docker-compose up -d
    Write-Host "✓ Docker containers started" -ForegroundColor Green
    Start-Sleep -Seconds 5
} else {
    Write-Host "docker-compose.yml not found. Skipping Docker setup." -ForegroundColor Yellow
}

# Restore packages
Write-Host ""
Write-Host "Restoring NuGet packages..." -ForegroundColor Yellow
dotnet restore
Write-Host "✓ Packages restored" -ForegroundColor Green

# Create migrations
Write-Host ""
Write-Host "Creating database migrations..." -ForegroundColor Yellow
cd src/OnlineExamSystem.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../OnlineExamSystem.API
Write-Host "✓ Migration created" -ForegroundColor Green

# Apply migrations
Write-Host ""
Write-Host "Applying database migrations..." -ForegroundColor Yellow
dotnet ef database update --startup-project ../OnlineExamSystem.API
Write-Host "✓ Database migration applied" -ForegroundColor Green

cd ../..

# Summary
Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Run the application:"
Write-Host "   cd src/OnlineExamSystem.API"
Write-Host "   dotnet run"
Write-Host ""
Write-Host "2. Open API documentation:"
Write-Host "   http://localhost:5000/swagger"
Write-Host ""
Write-Host "3. Test health endpoint:"
Write-Host "   http://localhost:5000/api/health/status"
Write-Host ""
