@echo off
title Password Manager
setlocal

set "EXE=%~dp0src\PasswordManager\bin\Release\net8.0\pm.exe"

if not exist "%EXE%" (
    echo Building Password Manager...
    dotnet build -c Release src\PasswordManager\PasswordManager.csproj || goto :error
)

"%EXE%" %*
goto :eof

:error
echo Build failed.
pause
