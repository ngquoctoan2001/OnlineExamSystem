#!/bin/bash
# Script khởi tạo Online Exam System cho Linux/macOS

echo "=== Online Exam System - Initialization Script ==="
echo ""

# Check .NET SDK
echo "Checking .NET SDK..."
dotnet_version=$(dotnet --version)
echo "✓ .NET version: $dotnet_version"

# Check if docker-compose.yml exists
if [ -f "docker-compose.yml" ]; then
    echo ""
    echo "Starting Docker containers..."
    docker-compose up -d
    echo "✓ Docker containers started"
    sleep 5
else
    echo "docker-compose.yml not found. Skipping Docker setup."
fi

# Restore packages
echo ""
echo "Restoring NuGet packages..."
dotnet restore
echo "✓ Packages restored"

# Apply migrations
echo ""
echo "Applying database migrations..."
dotnet ef database update --project src/OnlineExamSystem.Infrastructure --startup-project src/OnlineExamSystem.API
echo "✓ Database migration applied"

# Summary
echo ""
echo "=== Setup Complete! ==="
echo ""
echo "Next steps:"
echo "1. Run the application:"
echo "   cd src/OnlineExamSystem.API"
echo "   dotnet run"
echo ""
echo "2. Open API documentation:"
echo "   http://localhost:5000/swagger"
echo ""
echo "3. Test health endpoint:"
echo "   http://localhost:5000/api/health/status"
echo ""
