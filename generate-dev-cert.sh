#!/bin/bash

# Script to generate development HTTPS certificate for Docker

echo "Generating development HTTPS certificate..."

# Create directory for certificates if it doesn't exist
mkdir -p ./.docker/certs

# Clean up old certificate if it exists
dotnet dev-certs https --clean

# Generate new certificate
dotnet dev-certs https -ep ./.docker/certs/aspnetapp.pfx -p YourSecurePassword123!

# Set permissions so Docker container can read the certificate
# Make the certificate readable by all users (the app user in the container needs to read it)
chmod 644 ./.docker/certs/aspnetapp.pfx

# Trust the certificate (optional, for local development)
dotnet dev-certs https --trust

echo "✓ Certificate generated at ./.docker/certs/aspnetapp.pfx"
echo "✓ Password: YourSecurePassword123!"
echo "✓ Permissions set to 644 (readable by Docker container)"
echo ""
echo "You can now run: docker compose up --build"
