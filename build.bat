@ECHO OFF

IF EXIST program.exe DEL /F program.exe
CALL dotnet publish -c App -r win10-x64
CALL mklink ".\\bin\\App\\netcoreapp2.2\\ubuntu.16.10-x64\\MCTOPP" ".\\program"