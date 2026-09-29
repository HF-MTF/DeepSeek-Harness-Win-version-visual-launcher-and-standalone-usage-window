$src = 'E:\DeepSeekHarness\launcher-src'
$lib = Join-Path $src 'lib'
$out = Join-Path $src 'build'
New-Item -ItemType Directory -Force -Path $out | Out-Null
Remove-Item "$out\DSH启动器.exe" -Force -ErrorAction SilentlyContinue
$csc = 'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
& $csc /nologo /target:winexe /optimize+ /out:"$out\DSH启动器.exe" /win32icon:"$src\app.ico" /win32manifest:"$src\app.manifest" "/resource:$src\logo160.png,WhaleIcon" /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll /r:"$lib\Microsoft.Web.WebView2.Core.dll" /r:"$lib\Microsoft.Web.WebView2.WinForms.dll" "$src\Launcher3.cs"
$ok = ($LASTEXITCODE -eq 0) -and (Test-Path "$out\DSH启动器.exe")
Write-Output ('编译成功: ' + $ok)
if (-not $ok) { exit 1 }
Get-Process -Name 'DSH启动器' -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 1200
Copy-Item "$out\DSH启动器.exe" 'E:\DeepSeekHarness\DSH启动器.exe' -Force
$r = (Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{CommandLine='"E:\DeepSeekHarness\DSH启动器.exe" --page'; CurrentDirectory='E:\DeepSeekHarness'})
Start-Sleep -Seconds 9
$pr2 = Get-Process -Id $r.ProcessId -ErrorAction SilentlyContinue
if (-not $pr2) { Write-Output '窗口没起来'; exit 1 }
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
Write-Output ('窗口:   ' + $rc.Left + ',' + $rc.Top + '  ' + ($rc.Right-$rc.Left) + 'x' + ($rc.Bottom-$rc.Top))
Write-Output ('工作区: ' + $wa.Left + ',' + $wa.Top + '  ' + $wa.Width + 'x' + $wa.Height)
$match = ($rc.Left -eq $wa.Left) -and ($rc.Top -eq $wa.Top) -and (($rc.Right-$rc.Left) -eq $wa.Width) -and (($rc.Bottom-$rc.Top) -eq $wa.Height)
Write-Output ('精确贴合(无缝隙): ' + $match)
$bar = New-Object System.Drawing.Bitmap 900, 200
Add-Type -AssemblyName System.Drawing
$g = [System.Drawing.Graphics]::FromImage($bar)
$g.CopyFromScreen($rc.Left, $rc.Top, 0, 0, (New-Object System.Drawing.Size(900, 200)))
$bar.Save('E:\_max_tl.png', [System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose(); $bar.Dispose()
Write-Output '左上角截图已保存'