@echo off
chcp 65001 >nul
cd /d %~dp0
cd ..

echo インストーラーを作成します。実行するにはWiX Toolsetが必要です。

dotnet.exe publish -c Release --property:Platform="Any CPU" --self-contained true -p:PublishReadyToRun=true -o "WixToolset\publish"
wix.exe build -o "WixToolset\AudioTranscription.msi" "WixToolset\Package.wxs"
pause
