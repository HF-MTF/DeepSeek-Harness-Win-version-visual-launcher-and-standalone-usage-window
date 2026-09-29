# 更新记录

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
