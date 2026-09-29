@echo off
chcp 65001 >nul
cd /d "%~dp0"
echo ============================================
echo  DSH 启动器 - 编译并部署
echo ============================================
echo  1. 编译到 build\
echo  2. 自动检测 DSH 安装位置
echo  3. 部署 exe 与 3 个 WebView2 组件到该位置
echo  4. 冒烟验证：最大化是否精确贴合工作区
echo  5. 同步一份到 dist\（解压即用的发布成品）
echo.
echo  只想编译、不部署不动正在运行的启动器：
echo    powershell -ExecutionPolicy Bypass -File build.ps1 -NoDeploy
echo.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
echo.
echo 成品: dist\DSH启动器.exe（连同 3 个 WebView2 组件）
pause
