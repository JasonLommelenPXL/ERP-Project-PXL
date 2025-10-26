# PSEUDOCODE / PLAN (detailed):
# 1. Locate the project (.csproj) file in the repository root or immediate subfolders.
# 2. If no .csproj is found, print an error and exit.
# 3. Run `dotnet add <project> package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore`.
# 4. If the add command fails, try `dotnet restore` then retry the add command.
# 5. Once package is added, run `dotnet build` to ensure compilation succeeds.
# 6. If user prefers Visual Studio, instruct to use "Manage NuGet Packages" and install the package.
# 7. Alternative: If you do not want the package, comment out or remove the
#    `builder.Services.AddDatabaseDeveloperPageExceptionFilter();` line in `Program.cs`.
#
# This script implements steps 1-5 automatically.

set -e

# Find a csproj file (prefer the one in current directory, fallback to first match)
project_file=""
if ls *.csproj 1> /dev/null 2>&1; then
  project_file="$(ls *.csproj | head -n1)"
else
  project_file="$(find . -maxdepth 2 -name '*.csproj' -print -quit || true)"
fi

if [ -z "$project_file" ]; then
  echo "Error: No .csproj file found. Run this script from the project directory or specify the project manually."
  echo "Manual command example:"
  echo "  dotnet add <path-to-project>.csproj package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"
  exit 1
fi

echo "Using project: $project_file"
echo "Adding package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore..."
if dotnet add "$project_file" package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore; then
  echo "Package added successfully."
else
  echo "Failed to add package. Attempting restore and retry..."
  dotnet restore "$project_file" || true
  if dotnet add "$project_file" package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore; then
    echo "Package added successfully on retry."
  else
    echo "Failed again. Please install the package manually or use Visual Studio NuGet package manager."
    exit 1
  fi
fi

echo "Restoring and building project..."
dotnet restore "$project_file"
dotnet build "$project_file" --no-restore

echo "Done. If you still see the console message suggesting the command, it's informational — it shows how to install the package."
echo ""
echo "If you prefer to avoid adding the package, open `Program.cs` and comment out the line:"
echo "  // builder.Services.AddDatabaseDeveloperPageExceptionFilter();"
echo ""
echo "Windows PowerShell manual command (if needed):"
echo "  dotnet add \"$project_file\" package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore"