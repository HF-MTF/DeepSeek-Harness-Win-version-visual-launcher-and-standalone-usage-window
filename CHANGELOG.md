# 更新记录

## v1.0.6 — 2026-09-30

### 变更

- **运行窗口改名「DSH 工作台」** —— 原先标题是 `DSH-HFRin调试版`，带着「调试版」三个字，
  和正式发布版的身份不符。改名只动一处常量 `PageForm.WindowCaption`：窗口自绘标题栏、
  任务栏显示、以及网页标题变化后的回填都会跟着变。
- **仓库更名为 `HFRin-DSH-Windows-Launcher`** —— GitHub 会自动 301 重定向旧链接，
  已发出的地址不会失效。

## v1.0.5 — 2026-09-30

### 新增

- **补齐开源规范文件** —— 之前仓库只有 README，GitHub 社区规范检查的完整度只有 28%。
  现在补上 `LICENSE`（MIT，© 2026 HF-MTF and HFRin）、`CONTRIBUTING.md`、`SECURITY.md`，
  以及 `.github/`（Issue 表单、PR 模板、CI 编译验证）和 `.gitattributes`。
- **README 增加对外入口** —— 顶部加许可证 / 发布 / 平台徽章与「下载最新成品包」直达链接；
  原先指向 `release\` 的说明改为指向 Releases（那个目录并不在版本库里，访客点进去是空的）；
  「目录结构」区分了「仓库内容」与「不纳入版本库的本地目录」。
- **配图改用语义化文件名** —— `docs/1.png` / `docs/bin.png` 改为
  `docs/screenshot-window.png` / `docs/screenshot-panel.png`，并在 README 里并排显示。
- **成品 exe 带上版本信息** —— 新增 `AssemblyInfo.cs`，由 `build.ps1` 一起编译。
  右键「属性 → 详细信息」现在能看到产品名、公司、版权与版本号（当前 1.0.5）；
  此前这些字段全是空的、`FileVersion` 显示 `0.0.0.0`。
- **成品包补齐内容** —— `dist\` 现在同时包含 `使用说明.txt` 与 `LICENSE`，成品包就是
  `dist\` 的全部文件；使用说明里补了版本号、项目地址与许可证说明。

### 修复

- **关掉窗口后进程不退出，导致再也打不开程序（严重）** —— 关闭面板后启动器变成一个没有窗口、
  却还占着单实例锁的僵尸进程：任务栏没有它，再双击只得到「已经在运行了」，从此进不去。
  根因在 `DshContext` 判断「窗口还在不在」用的是 `Form.IsDisposed`，而 `FormClosed` 事件是在
  `Dispose()` **之前**触发的 —— 那一刻它还是 `false`，于是永远认为窗口还开着，
  `ApplicationContext.ExitThread()` 一次都没被调用过，消息循环就一直空转。
  现在新增 `LauncherForm` 基类，用「真的关掉了」的 `IsGone` 标记（在 `OnFormClosed` 里、
  `base` 之前置位）做判断；另外补了一道不依赖任何事件的巡检兜底：只要面板与页面窗口都没了，
  直接结束进程。实测关窗口 6 ms 退出，立即重启可正常打开。
- **旧实例没有窗口时彻底死锁** —— 单实例保护只认 `Process.MainWindowHandle`，而它只会返回
  *可见* 的窗口：旧实例卡住（窗口已销毁）时它会返回 0，于是既激活不了、也不给别的出路。
  现在自己枚举对方进程的顶层窗口（隐藏的也能找到并叫出来）；若对方确实连窗口都没有了，
  会问一句「结束那个卡住的启动器，然后重新打开窗口」—— 只结束启动器本身，不碰正在跑的 DSH。
- **未处理异常弹窗会把启动器卡成僵尸** —— WinForms 默认的未处理异常对话框自带嵌套消息循环，
  会把 `ExitThread()` 投递的 `WM_QUIT` 吃掉，外层消息循环于是永远退不出来。现在统一改成写
  `launcher-error.log`，不再弹窗。
- **三个定时器不随窗体释放而停止** —— 面板关闭后 `_stateTimer` / `_balanceTimer` / `_logTimer`
  仍在 Tick 并去碰已经释放的控件。已覆写 `Dispose(bool)` 停掉并释放它们，每个 Tick 也加了
  存活检查。
- **关闭动画重复触发** —— 双击标题栏会在模态确认框的嵌套消息循环里重入 `OnFormClosing`，
  导致关闭动画与 `Close()` 被走两遍。现在加了 `_closeAnimBusy` 防重入，动画回调也包了
  `try/catch`。
- **`ObjectDisposedException: 无法访问已释放的对象 TextBox`（补完）** —— 除日志控件本身，
  状态刷新的 `ApplyRunning` / `RefreshState` 以及余额回调也会碰已释放的控件，一并加了防护。

## 2026-09-29

首个公开版本。

### 新增

- **自动检测 DSH 安装位置** —— 不再需要改源码填路径。按以下顺序探测，第一个同时具备
  `node\node.exe` 与 `dsh\node_modules\@deepseek-ai\dsh\lib\bin.js` 的目录胜出：
  `launcher-path.txt` → 环境变量 `DSH_ROOT` → `DSH_HOME` 及其上级 → exe 自身目录及向上 4 级
  → 常见安装位置 → 当前工作目录。所以 exe 放在 DSH 根目录或其任意子目录里都能认出来。
- **一键安装 DSH** —— 面板上的按钮，自动取 Node 运行时（npmmirror 镜像）并安装
  `@deepseek-ai/dsh`。命令行对应 `--install <目录>`，适合脚本化部署。
- **`--where` 诊断** —— 逐条打印候选目录的判定结果并落一份 `launcher-where.txt`，
  用来排查「为什么没找到 DSH」。
- **手动指定目录** —— 面板上的「指定已有目录」，路径记进 `launcher-path.txt`，下次直接命中。

### 修复

- **WebView2 数据目录跑到 `C:\Program Files`** —— `DSH_HOME` 若被设成相对路径，会被
  WebView2 按它自己的安装目录解析。现在所有路径统一规范成绝对路径（基准取 exe 所在目录，
  而不是会随启动方式漂移的当前目录）；目录建不出来时回退到临时目录，不再让运行窗口整个起不来。
- **`ObjectDisposedException: 无法访问已释放的对象 TextBox`** —— 面板关闭后，日志定时器与
  DSH 退出回调仍在往已释放的控件写。已加 `IsDisposed` 防护。
- **重复启动报 `HRESULT 0x800700AA`（资源在使用中）** —— 面板与运行窗口共用同一个 WebView2
  数据目录，而该目录是独占的。现在有单实例保护，重复双击只会把已有窗口叫到前台。
- **安装时下载握手失败** —— `ServicePointManager.SecurityProtocol` 原本设在 `Main` 的后半段，
  而 `--install` 分支在那之前就返回了，所有网络请求跑在没有 TLS 1.2 的默认协议上。
  已提前到 `Main` 的第一件事。
- **拿不到访问凭据时没有提示** —— DSH 的 token 只存在它自己的进程内存里（不写文件），
  仅在启动时打印一次。若这次运行的 DSH 不是本启动器拉起的，就无法获得。现在会明确说明
  原因并提供一键重启（成功打开一次之后，凭据以 cookie 形式长期保存，以后不再需要）。
- **一键安装的日志乱码、失败后无从排查** —— npm 输出是 UTF-8，而中文系统的 `cmd.exe`
  默认代码页是 936(GBK)，.NET 按后者解码会把整份日志变成乱码、真实报错被完全吃掉。
  现在直接让 node 执行 `npm-cli.js`、绕开 `cmd.exe`（顺带免掉引号嵌套），并显式按 UTF-8
  解码；失败时自动捞出 npm 调试日志的尾部。
- **全新机器上安装失败：`'node' 不是内部或外部命令`** —— 包内的 install script 用的是
  裸 `node` 命令（例如 koffi 的 `cmd.exe /d /s /c node ./cnoke.cjs ...`），靠 PATH 查找。
  启动器过去没有把 `<root>\node` 注入子进程 PATH，全新机器上脚本必然失败、整个安装以
  退出码 1 中断（开发机上因为 PATH 里恰好有 node，一直没暴露出来）。现在启动 npm 前会把
  `<root>\node` 前置进 PATH；若仍失败，会自动换官方源并跳过包内构建脚本再试一次
  （那些包的预编译产物本来就在包里，跳过不影响 DSH 运行）。

### 已知限制

- 仅 Windows（依赖 WinForms + WebView2 + DWM）
- 运行窗口不支持透明度动画：WebView2 在分层窗口（`Opacity<1`）下渲染异常
- 余额显示依赖 DSH 的凭据配置（`$DSH_HOME/.credentials.yaml`）
