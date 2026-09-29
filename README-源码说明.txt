DSH 启动器 —— 源码说明
========================================

文件构成
  Launcher3.cs    启动器主体：面板窗口 + DSH 页面窗口（WebView2）+ 自绘深色边框 + 窗口动画
                  另含 DshLocator（DSH 安装位置探测）与一键安装流程
  app.manifest    DPI 感知（PerMonitorV2）/ 通用控件 v6 / Win10-11 兼容声明
  app.ico         程序图标
  logo160.png     标题栏鲸鱼头像，编译时作为嵌入资源 WhaleIcon
  whale64.png     备用素材
  build.ps1       一键编译 + 部署 + 最大化冒烟验证（自动检测部署目标）
  lib\            WebView2 SDK（编译引用，运行时也需要）
  README-源码说明.txt  本文件

运行时生成（不在源码里）
  launcher-path.txt    用户指定的 DSH 根目录；有它就不再自动探测
  launcher-where.txt   --where 的诊断输出
  launcher-install.log --install 的安装日志
  build\               编译产物目录

编译
  用 .NET Framework 自带的 csc.exe（C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe）
  在本目录执行：
      powershell -ExecutionPolicy Bypass -File build.ps1
  可选参数：-NoDeploy 只编译；-DeployTo <目录> 指定部署位置；-NoSmoke 跳过冒烟
  build.ps1 会依次：编译到 build\（同时把三个 WebView2 dll 一并复制进去，使 build\
                    成为可直接运行的自包含目录）→ 用 --where 自动确定 DSH 安装位置
                    → 部署 exe 与三个 WebView2 dll 到该位置
                    → 起一个实例最大化并核对窗口矩形是否贴合工作区、截图留证

  注意：build.ps1 含中文，必须存成 UTF-8 with BOM。
        Windows PowerShell 5.1 读取无 BOM 的 .ps1 时按 ANSI/GBK 解码，中文字符串
        会被拆坏，报出一堆 "Unexpected token" / "Missing closing ')'" 之类的语法错误。
        改完这个文件后记得确认 BOM 还在 —— 不少编辑器保存时会把它丢掉。

  csc 引用除 System / System.Drawing / System.Windows.Forms 外，还需要：
      System.IO.Compression.dll
      System.IO.Compression.FileSystem.dll      （ZipArchive，解压 Node 包用）

运行环境
  Windows 10/11 + .NET Framework 4.8 + WebView2 Runtime（Win11 随 Edge 自带）
  lib\ 下这三个 dll 必须和 DSH启动器.exe 放在同一目录：
      Microsoft.Web.WebView2.Core.dll
      Microsoft.Web.WebView2.WinForms.dll
      WebView2Loader.dll

DSH 安装位置
  由 static class DshLocator 解析，Main 入口第一件事就是 DshLocator.Resolve()。
  Cfg 里的 Home / NodeExe / NodeDir / DshBin 已从 const 改成静态属性、转读 DshLocator，
  这样十余处调用方一行都不用改，但路径变成运行时决定。

  判定一个目录是 DSH 根，要求同时存在：
      node\node.exe
      dsh\node_modules\@deepseek-ai\dsh\lib\bin.js

  探测优先级：launcher-path.txt → DSH_ROOT → DSH_HOME 及其上级 → exe 自身目录及
  向上 4 级 → 常见安装位置（%LOCALAPPDATA% / %USERPROFILE% / %ProgramFiles% /
  各盘根下的 DeepSeekHarness 与 DSH）→ 当前工作目录。

  DSH_HOME：环境变量 DSH_HOME → <root>\home → %USERPROFILE%\.dsh。
  Cfg.Home 必须非 null（多处 Path.Combine 直接吃它），所以最后一级是兜底。

一键安装（面板「一键安装 DSH」，或命令行 --install <目录>）
  两个入口共用同一套 InstallWorker：面板把日志切回 UI 线程，--install 逐行写
  launcher-install.log（本程序是 winexe，没有控制台，不能只靠 stdout）。

  1. 选目标目录（--install 直接给定）
  2. 从 npmmirror 取最新 Node LTS 版本号（正则解析 index.json），
     镜像不可达时回退到内置的 FallbackNodeVersion
  3. 下载 node-vX-win-x64.zip，解压时剥掉公共顶层目录（Node 的包都带一层）
  4. 用 node\npm.cmd 安装 @deepseek-ai/dsh（走 registry.npmmirror.com）
  5. 建 home\，把路径写进 launcher-path.txt，再重新 Resolve

  实测（本机，装机时）：Node v24.21.0 共 35 MB 下载约 3 秒，npm 装 518 个包
  约 44 秒，全程 0.8 分钟；装出来的 DSH 能正常启动 web 并监听端口。

  ！TLS 必须在任何网络请求之前设置。ServicePointManager.SecurityProtocol
    原先写在 Main 的后半段，而 --install 分支在那之前就 return 了，导致下载
    与版本查询全部握手失败（"基础连接已经关闭: 接收时发生错误"，而且版本号
    会静默退化成兜底值）。现已提到 Main 的第一件事。

  npm 会警告若干包（node-pty / koffi 等）的 install scripts 未执行。实测无害：
  node-pty 的预编译二进制随包发布，两种安装的 .node 文件数量逐项一致。

第三方组件
  Microsoft.Web.WebView2 1.0.4258.31（NuGet 包，取自 lib/net462 与
  runtimes/win-x64/native），版权归 Microsoft 所有。

关键实现备注（都是踩过的坑）
  · DSH 的 stdout/stderr 必须重定向到日志文件，不能接启动器进程的匿名管道；
    否则关掉启动器时管道读端断开，DSH 下次写日志会撞 EPIPE 直接崩溃。
    （安装流程跑 npm 时用了管道 + 异步读取，但那是短命进程且会 WaitForExit，
      不会出现同样的问题。）
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
  · 面板高度：加了第三行按钮后 DH 560→630、LOG_Y 394→464，这几处尺寸是联动的，
    改一个要一起看（BTN_ROW = BTN_H + 14 已抽出常量）。
  · 本程序是 winexe，没有控制台。--where 先 AttachConsole(-1) 尝试输出，再无条件
    落一份 launcher-where.txt，保证命令行和双击两种用法都拿得到结果。
