@echo off
chcp 65001 >nul
cd /d %~dp0
wsl --shell-type login bash bump-version.sh
pause
