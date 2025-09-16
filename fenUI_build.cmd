@echo off

echo Building fenUI...
echo:

cd "fenUI"
dotnet build -c Release
cd ".."

echo Copy new fenUI build to out folder...
echo:

if not exist "out" (
    mkdir "out"
)

copy "fenUI\bin\Release\net9.0-windows10.0.19041.0\fenUI.dll" "out"

echo:
echo:
echo:

echo ============= SUCESS =============
echo Build and move of fenUI completed!

pause