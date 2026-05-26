@echo off
echo Publishing OMS Deployment Assistant...
dotnet publish OmsDeployer.App -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o dist
echo.
echo Done! Executable is at: dist\OmsDeployer.App.exe
pause
