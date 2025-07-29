@echo off

echo:
echo =============================================
echo Building the .NET project of the crash handler
echo:

dotnet publish -c Release -r win-x64

echo Copying crash handler to fenUI lib folder
echo:

copy "..\crash-handler\fenUICrashHandler\bin\Release\net9.0-windows10.0.19041.0\win-x64\fenUICrashHandler.exe" "..\fenUI\lib"

echo:
echo:
echo ================ SUCCESS =================
echo Build and move of crash handler completed!