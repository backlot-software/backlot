#!/bin/bash


# Build the solution
dotnet build --configuration Debug

# Pack all projects (excluding Backlot.Demo.*)

mkdir -p nupkgs
find . -name "*.csproj" -not -path "*Backlot.Demo.*" | while read -r project; do
  dotnet pack "$project" \
    --configuration "Debug" \
    --output nupkgs
done

# List generated .nupkg files
echo "Generated NuGet packages:"
ls -l nupkgs/
