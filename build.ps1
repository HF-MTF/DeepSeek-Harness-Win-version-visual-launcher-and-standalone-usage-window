<#
  DSH 启动器 —— 编译 / 部署 / 冒烟验证

  用法（在本目录执行）：
    powershell -ExecutionPolicy Bypass -File build.ps1
        编译到 build\ → 自动检测 DSH 安装位置 → 部署 exe 与 WebView2 运行库
        → 起一个实例最大化，核对窗口矩形是否贴合工作区并截图留证
    powershell -ExecutionPolicy Bypass -File build.ps1 -NoDeploy
        只编译，不碰正在运行的启动器
    powershell -ExecutionPolicy Bypass -File build.ps1 -DeployTo 'D:\MyDSH'
        部署到指定目录（跳过自动检测）
    powershell -ExecutionPolicy Bypass -File build.ps1 -NoSmoke
        部署但不做最大化冒烟

  依赖：.NET Framework 自带的 csc.exe，以及 lib\ 下的 WebView2 程序集。
#>
param(
  [string]$DeployTo,
  [switch]$NoDeploy,
  [switch]$NoSmoke
)

$ErrorActionPreference = 'Continue'

# 源码目录 = 本脚本所在目录，不再依赖任何写死的路径
$src = $PSScriptRoot
if (-not $src) { $src = Split-Path -Parent $MyInvocation.MyCommand.Path }
$lib = Join-Path $src 'lib'
$out = Join-Path $src 'build'
$exe = Join-Path $out 'DSH启动器.exe'

New-Item -ItemType Directory -Force -Path $out | Out-Null
Remove-Item $exe -Force -ErrorAction SilentlyContinue

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
  Write-Host ('[X] 找不到 csc.exe：' + $csc) -ForegroundColor Red
  exit 1
}

# ---------- 1. 编译 ----------
Write-Host '== 编译 ==' -ForegroundColor Cyan
& $csc /nologo /target:winexe /optimize+ /out:"$exe" `
  /win32icon:"$src\app.ico" /win32manifest:"$src\app.manifest" `
  "/resource:$src\logo160.png,WhaleIcon" `
  /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll `
  /r:System.IO.Compression.dll /r:System.IO.Compression.FileSystem.dll `
  /r:"$lib\Microsoft.Web.WebView2.Core.dll" /r:"$lib\Microsoft.Web.WebView2.WinForms.dll" `
  "$src\Launcher3.cs" "$src\AssemblyInfo.cs"

if ($LASTEXITCODE -ne 0 -or -not (Test-Path $exe)) {
  Write-Host '[X] 编译失败' -ForegroundColor Red
  exit 1
}
Write-Host ('编译成功: ' + $exe + '  (' + [math]::Round((Get-Item $exe).Length / 1KB, 1) + ' KB)') -ForegroundColor Green

# 让 build\ 自包含：WebView2Loader.dll 是原生 DLL，必须和 exe 同目录，
# 否则 PageForm 初始化 WebView2 时会抛 FileNotFoundException（表现成一跑就退出）。
foreach ($dll in @('Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll')) {
  $s = Join-Path $lib $dll
  if (Test-Path $s) { Copy-Item $s (Join-Path $out $dll) -Force }
}

if ($NoDeploy) { Write-Host '已跳过部署（-NoDeploy）' -ForegroundColor Yellow; exit 0 }

# ---------- 2. 确定部署目标 ----------
# 没有显式指定时，让刚编译出来的 exe 自己报告 —— 与运行时共用同一套检测逻辑，
# 避免 build 脚本和程序里各写一份、日后漂移。
Write-Host ''
Write-Host '== 确定 DSH 安装位置 ==' -ForegroundColor Cyan

if (-not $DeployTo) {
  $wf = Join-Path $out 'launcher-where.txt'
  Remove-Item $wf -Force -ErrorAction SilentlyContinue
  & $exe --where | Out-Null
  Start-Sleep -Milliseconds 1200
  if (Test-Path $wf) {
    $m = Select-String -Path $wf -Pattern '^DSH 根目录\s*:\s*(.+?)\s*$' | Select-Object -First 1
    if ($m) {
      $cand = $m.Matches[0].Groups[1].Value.Trim()
      if ($cand -ne '(未找到)' -and (Test-Path $cand)) { $DeployTo = $cand }
    }
  }
}

if (-not $DeployTo) {
  Write-Host '[!] 没有自动找到 DSH 安装位置。' -ForegroundColor Yellow
  Write-Host '    用 -DeployTo 指定，例如：' -ForegroundColor Yellow
  Write-Host '      powershell -ExecutionPolicy Bypass -File build.ps1 -DeployTo "E:\DeepSeekHarness"' -ForegroundColor Yellow
  exit 1
}
Write-Host ('部署目标: ' + $DeployTo) -ForegroundColor Green

# ---------- 3. 部署 ----------
Write-Host ''
Write-Host '== 部署 ==' -ForegroundColor Cyan
Get-Process -Name 'DSH启动器' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 1200

$target = Join-Path $DeployTo 'DSH启动器.exe'
Copy-Item $exe $target -Force
foreach ($dll in @('Microsoft.Web.WebView2.Core.dll', 'Microsoft.Web.WebView2.WinForms.dll', 'WebView2Loader.dll')) {
  $s = Join-Path $lib $dll
  if (Test-Path $s) { Copy-Item $s (Join-Path $DeployTo $dll) -Force }
}
Write-Host ('已部署: ' + $target) -ForegroundColor Green

if ($NoSmoke) { Write-Host '已跳过冒烟（-NoSmoke）' -ForegroundColor Yellow; exit 0 }

# ---------- 4. 冒烟验证 ----------
Write-Host ''
Write-Host '== 冒烟验证（最大化是否精确贴合工作区）==' -ForegroundColor Cyan
$r = (Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{
  CommandLine      = '"' + $target + '" --page'
  CurrentDirectory = $DeployTo
})
Start-Sleep -Seconds 9
$pr2 = Get-Process -Id $r.ProcessId -ErrorAction SilentlyContinue
if (-not $pr2) { Write-Host '窗口没起来' -ForegroundColor Red; exit 1 }

Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public class MaxTest {
  [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int cmd);
  [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
  [DllImport("shcore.dll")] public static extern int SetProcessDpiAwareness(int v);
  [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
}
'@
[void][MaxTest]::SetProcessDpiAwareness(2)
[void][MaxTest]::ShowWindow($pr2.MainWindowHandle, 3)
Start-Sleep -Milliseconds 1500
$rc = New-Object MaxTest+RECT
[void][MaxTest]::GetWindowRect($pr2.MainWindowHandle, [ref]$rc)
Add-Type -AssemblyName System.Windows.Forms
$wa = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
Write-Output ('窗口:   ' + $rc.Left + ',' + $rc.Top + '  ' + ($rc.Right - $rc.Left) + 'x' + ($rc.Bottom - $rc.Top))
Write-Output ('工作区: ' + $wa.Left + ',' + $wa.Top + '  ' + $wa.Width + 'x' + $wa.Height)
$match = ($rc.Left -eq $wa.Left) -and ($rc.Top -eq $wa.Top) -and (($rc.Right - $rc.Left) -eq $wa.Width) -and (($rc.Bottom - $rc.Top) -eq $wa.Height)
Write-Output ('精确贴合(无缝隙): ' + $match)

$shot = Join-Path $out '_max_tl.png'
$bar = New-Object System.Drawing.Bitmap 900, 200
Add-Type -AssemblyName System.Drawing
$g = [System.Drawing.Graphics]::FromImage($bar)
$g.CopyFromScreen($rc.Left, $rc.Top, 0, 0, (New-Object System.Drawing.Size(900, 200)))
$bar.Save($shot, [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bar.Dispose()
Write-Output ('左上角截图: ' + $shot)

# 冒烟用的实例不能留着：--page 同样持有单实例锁，留着会让下一次双击只得到
# 「DSH 启动器已经在运行了」，而那个被最大化的窗口使用者根本看不见。
Get-Process -Id $r.ProcessId -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue

# 同步一份到 dist\：dist\ 里的全部文件就是成品包的内容，解压即可运行
$dist = Join-Path $src 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
Copy-Item "$out\DSH启动器.exe" (Join-Path $dist 'DSH启动器.exe') -Force
foreach ($n in 'Microsoft.Web.WebView2.Core.dll','Microsoft.Web.WebView2.WinForms.dll','WebView2Loader.dll') {
  $f = Join-Path $lib $n
  if (Test-Path $f) { Copy-Item $f (Join-Path $dist $n) -Force }
}
# 成品包也带一份许可证；dist\使用说明.txt 是直接维护的源文件，不在这里生成
$lic = Join-Path $src 'LICENSE'
if (Test-Path $lic) { Copy-Item $lic (Join-Path $dist 'LICENSE') -Force }
Write-Output ('已同步 dist: ' + ((Get-ChildItem $dist -File | ForEach-Object { $_.Name }) -join ', '))
