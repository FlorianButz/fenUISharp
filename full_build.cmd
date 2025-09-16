@echo off

echo Building fenUI...
echo:

echo Removing old crash handler...
echo:

cd "fenUI\lib"
del /q *
cd "..\.."

echo Building fenUI without crash handler...
echo:

cd "fenUI"
dotnet build -c Release
cd ".."

echo Copying new fenUI build to crash handler...
echo:

set "SOURCE=%~dp0\fenUI\bin\Release\net9.0-windows10.0.19041.0\"
set "DEST=%~dp0\crash-handler\fenUICrashHandler\lib"

for %%I in ("%SOURCE%") do set "SOURCE=%%~fI"
for %%I in ("%DEST%") do set "DEST=%%~fI"

echo SOURCE = %SOURCE%
echo:
echo DEST   = %DEST%
echo:

if not exist "%DEST%" (
    mkdir "%DEST%"
)

echo Copying from "%SOURCE%" to "%DEST%"
echo:

xcopy /e /v /y "%SOURCE%\\" "%DEST%\\"

echo Building crash handler...
echo:

call "crash-handler\build.cmd"

echo Build fenUI with new crash handler...
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