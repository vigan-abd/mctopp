@ECHO OFF

IF EXIST program.exe DEL /F program.exe
CALL dotnet publish -c App -r win10-x64
CALL cd ".\\bin\\App\\netcoreapp2.2\\win10-x64"
ECHO "Program is stored in .\\bin\\App\\netcoreapp2.2\\win10-x64\\MCTOPP.exe"