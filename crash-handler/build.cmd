echo Building the .NET project
dotnet publish -c Release -r win-x64

set "SOURCE=%~dp0..\crash-handler\fenUICrashHandler\bin\Release\net9.0-windows10.0.19041.0\win-x64\publish"
set "DEST=%~dp0..\fenUI\lib"

for %%I in ("%SOURCE%") do set "SOURCE=%%~fI"
for %%I in ("%DEST%") do set "DEST=%%~fI"

echo SOURCE = %SOURCE%
echo DEST   = %DEST%

if not exist "%DEST%" (
    mkdir "%DEST%"
)

echo Copying from "%SOURCE%" to "%DEST%"
xcopy /e /v /y "%SOURCE%\" "%DEST%\"

echo Build and move completed!