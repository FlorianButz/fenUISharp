@echo off

echo:
echo =============================================
echo Building the .NET project of the crash handler
echo:

set CRASH_HANDLER_PROJECT=%~dp0..\crash-handler\fenUICrashHandler\fenUICrashHandler.csproj

dotnet build "%CRASH_HANDLER_PROJECT%" -c Release -r win-x64

echo Copying crash handler to fenUI lib folder
echo:

set CRASH_HANDLER_EXE=%~dp0..\crash-handler\fenUICrashHandler\bin\Release\net9.0-windows10.0.19041.0\win-x64\fenUICrashHandler.exe
set CRASH_HANDLER_DLL=%~dp0..\crash-handler\fenUICrashHandler\bin\Release\net9.0-windows10.0.19041.0\win-x64\fenUICrashHandler.dll
set CRASH_HANDLER_CFG=%~dp0..\crash-handler\fenUICrashHandler\bin\Release\net9.0-windows10.0.19041.0\win-x64\fenUICrashHandler.runtimeconfig.json

copy "%CRASH_HANDLER_EXE%" "%~dp0..\fenUI\lib"
copy "%CRASH_HANDLER_DLL%" "%~dp0..\fenUI\lib"
copy "%CRASH_HANDLER_CFG%" "%~dp0..\fenUI\lib"

echo:
echo:
echo ================ SUCCESS =================
echo Build and move of crash handler completed!
echo: