DSH 启动器 —— 源码说明
========================================

文件构成
  Launcher3.cs    启动器主体：面板窗口 + DSH 页面窗口（WebView2）+ 自绘深色边框 + 窗口动画
  app.manifest    DPI 感知（PerMonitorV2）/ 通用控件 v6 / Win10-11 兼容声明
  app.ico         程序图标
  logo160.png     标题栏鲸鱼头像，编译时作为嵌入资源 WhaleIcon
  whale64.png     备用素材
  build.ps1       一键编译 + 部署 + 最大化冒烟验证
  lib\            WebView2 SDK（编译引用，运行时也需要）
  README-源码说明.txt  本文件

编译
  用 .NET Framework 自带的 csc.exe（C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe）
  在本目录执行：
      powershell -ExecutionPolicy Bypass -File build.ps1
  build.ps1 会依次：编译到 build\ → 替换 E:\DeepSeekHarness\DSH启动器.exe
                    → 起一个实例最大化并核对窗口矩形是否贴合工作区、截图留证

运行环境
  Windows 10/11 + .NET Framework 4.8 + WebView2 Runtime（Win11 随 Edge 自带）
  lib\ 下这三个 dll 必须和 DSH启动器.exe 放在同一目录：
      Microsoft.Web.WebView2.Core.dll
      Microsoft.Web.WebView2.WinForms.dll
      WebView2Loader.dll

第三方组件
  Microsoft.Web.WebView2 1.0.4258.31（NuGet 包，取自 lib/net462 与
  runtimes/win-x64/native），版权归 Microsoft 所有。

关键实现备注（都是踩过的坑）
  · DSH 的 stdout/stderr 必须重定向到日志文件，不能接启动器进程的匿名管道；
    否则关掉启动器时管道读端断开，DSH 下次写日志会撞 EPIPE 直接崩溃。
  · 页面地址必须带 token（从 home\logs\dsh-web.out.log 解析），否则根路径返回 401。
  · WebView2 需要 --no-proxy-server，否则系统代理会拦掉对 127.0.0.1 的访问。
  · 窗口动画：FormBorderStyle.None 的窗口没有 WS_CAPTION，Windows 不会播过渡动画。
    因此窗口保持 FormBorderStyle.Sizable，再用 WM_NCCALCSIZE 返回 0 抹掉非客户区，
    外观仍是全自绘，打开/关闭/最小化/还原动画由 DWM 提供（另需
    DWMWA_TRANSITIONS_FORCEDISABLED=0 允许过渡）。
  · 带系统边框的窗口被"系统最大化"时，窗口矩形会比工作区大一圈（边框伸到屏幕外），
    底部会压住任务栏。所以最大化采用自定义实现（窗口矩形 = 工作区），并在 Resize 里
    把系统最大化（拖到顶部 / Win+Up）自动纠正过来。
  · 标题栏双击由自绘逻辑处理（避免走系统最大化）；拖动则发
    WM_NCLBUTTONDOWN + HTCAPTION 交给系统，保留拖到屏幕边缘分屏的行为。
  · 面板窗口与页面窗口用自定义 ApplicationContext 管理，各自独立关闭，
    两个都关掉才退出进程。
  · DPI：进程声明 PerMonitorV2，窗口尺寸按显示器缩放换算，否则 150% 缩放下会发虚。
