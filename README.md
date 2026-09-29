DSH 启动器
Windows 上的 DeepSeek Harness 启动器：一块面板管启停与日志，一个内嵌窗口看界面 —— 不再往浏览器里塞标签页。

启动器面板运行窗口
<img width="560" height="700" alt="image" src="https://github.com/user-attachments/assets/66f7c613-80b7-4071-ac3d-6d835a3db408" />
<img width="430" height="560" alt="image" src="https://github.com/user-attachments/assets/6ad57aec-a861-4383-b9f4-b018924e0540" />

它解决什么问题
原生 dsh web 每次都弹浏览器，DSH 混在几十个标签页里找不着
命令行启停麻烦，看不到实时日志和账户余额
关掉终端/启动器窗口会把 DSH 一起带走（stdout 管道断开 → EPIPE → 进程崩溃）
高 DPI 屏上界面发虚、窗口标题栏是系统白条，跟深色界面割裂
功能
面板窗口
一键 启动 / 重启 / 停止 DSH
实时状态：监听地址、PID、账户余额（60 秒自动刷新）
运行日志：DSH 的 stdout/stderr 实时回显到面板
系统托盘式的精简尺寸，DPI 感知（PerMonitorV2）
运行窗口（独立窗口，不是浏览器）
WebView2 内核，没有地址栏、标签栏、书签栏
自动带 token：从 $DSH_HOME/logs/dsh-web.out.log 解析当前 token，避免根路径 401
--no-proxy-server：系统开着代理时也能正常访问 127.0.0.1
自绘边框：圆角、渐变描边（左上受光）、鲸鱼头像 + 标题的深色标题栏、三个自绘按钮
最大化自动收细边框（8px → 3px），窗口矩形精确贴合工作区，不压任务栏
原生窗口动画：打开 / 关闭 / 最小化 / 还原由 DWM 播放（缩放淡入、飞向任务栏）
拖动标题栏移动，拖到屏幕边缘可分屏；双击标题栏最大化 / 还原
F12 开发者工具、右键菜单、Ctrl+R 刷新
页面里的外链（target=_blank）交给系统默认浏览器
两者关系
面板和运行窗口是同一个 exe 的两个窗口，各自独立关闭：

关掉面板 → 运行窗口继续开着，进程不退
两个都关掉 → 进程才退出
面板与页面窗口用自定义 ApplicationContext 管理生命周期
环境要求
依赖	说明
Windows 10 / 11 x64	开发和验证环境是 Windows 11
.NET Framework 4.8	Win10 1803+ 自带
WebView2 Runtime	Win11 随 Edge 自带；Win10 可能需单独安装
DeepSeek Harness	已安装的 @deepseek-ai/dsh（Node 运行时 + 主程序）
快速开始
直接用编译好的
取 DSH启动器.exe 和这三个文件放在同一目录：

代码块


DSH启动器.exe
Microsoft.Web.WebView2.Core.dll
Microsoft.Web.WebView2.WinForms.dll
WebView2Loader.dll
打开 Launcher3.cs 顶部的 Cfg 类，把路径改成你自己的安装位置（默认写的是 E:\DeepSeekHarness）

双击运行

自己编译
powershell


# 在源码目录执行（需要 .NET Framework 自带的 csc.exe）
powershell -ExecutionPolicy Bypass -File build.ps1
build.ps1 会：编译到 build\ → 替换部署目录的 exe → 起一个实例检查最大化是否贴合工作区并截图留证。

配置
配置集中在 Launcher3.cs 顶部：

csharp


static class Cfg
{
    public const int    Port    = 3080;
    public const string Home    = @"E:\DeepSeekHarness\home";                 // DSH_HOME
    public const string NodeExe = @"E:\DeepSeekHarness\node\node.exe";
    public const string NodeDir = @"E:\DeepSeekHarness\node";
    public const string DshBin  = @"E:\DeepSeekHarness\dsh\node_modules\@deepseek-ai\dsh\lib\bin.js";
    public const string Url     = "http://127.0.0.1:3080";
}

命令行参数
参数	行为
无	打开启动器面板
--page	只打开运行窗口，不显示面板（适合做"直接进界面"的快捷方式）
目录结构
代码块


launcher-src/
├─ Launcher3.cs              启动器主体（面板 + 运行窗口 + 自绘边框）
├─ app.manifest              DPI 感知 / 通用控件 v6 / Win10-11 兼容
├─ app.ico                   程序图标
├─ logo160.png               标题栏头像（编译为嵌入资源 WhaleIcon）
├─ whale64.png               备用素材
├─ build.ps1                 一键编译 + 部署 + 冒烟验证
├─ lib/                      WebView2 SDK（编译引用，运行时也要）
└─ docs/                     README 配图

实现要点
都是踩过的坑，改代码前建议看一眼 README-源码说明.txt：

管道 EPIPE：DSH 的 stdout/stderr 必须重定向到文件，不能接启动器进程的匿名管道，否则关掉启动器时管道读端断开，DSH 写日志直接崩
token 401：DSH 根路径不带 token 会 401，所以运行窗口启动时从日志里解析当前 token
系统代理：WebView2 默认走系统代理，本地回环也被拦，需要 --no-proxy-server
原生动画：FormBorderStyle.None 的窗口没有 WS_CAPTION，Windows 不给播过渡动画 —— 保留 Sizable 样式再用 WM_NCCALCSIZE 抹掉非客户区
最大化留缝：带边框窗口被系统最大化时窗口矩形比工作区大一圈，底部压任务栏 → 自定义最大化并在 Resize 里自动纠正
DPI 发虚：进程声明 PerMonitorV2，窗口尺寸按显示器缩放换算
已知限制
路径写死在 Cfg 里，换机器要改源码重编（暂未做成配置文件）
仅 Windows（依赖 WinForms + WebView2 + DWM）
运行窗口不支持透明度动画：WebView2 在分层窗口（Opacity<1）下渲染异常，所以只有面板用了淡入淡出
余额显示依赖 DSH 的凭据配置（$DSH_HOME/.credentials.yaml）
第三方组件
Microsoft.Web.WebView2 1.0.4258.31（NuGet 包中的 lib/net462 与 runtimes/win-x64/native），版权归 Microsoft，遵循其许可条款
DeepSeek Harness 由 deepseek-ai 开发，本项目只是它的 Windows 外壳
License
MIT
