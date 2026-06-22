#!/bin/bash

# Kill process using port 7000
echo "Killing process on port 7000..."
lsof -ti:7000 | xargs kill -9 2>/dev/null || echo "No process found on port 7000"

echo "Port 7000 is now free. You can run 'dotnet run' again."
