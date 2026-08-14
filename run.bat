@echo off
title Password Manager
setlocal

set "EXE=%~dp0src\PasswordManager.App\bin\Release\net8.0-windows\PasswordManager.exe"

if not exist "%EXE%" (
    echo Building Password Manager...
    dotnet build -c Release src\PasswordManager.App\PasswordManager.App.csproj || goto :error
)

start "" "%EXE%" %*
goto :eof

:error
echo Build failed.
pause
