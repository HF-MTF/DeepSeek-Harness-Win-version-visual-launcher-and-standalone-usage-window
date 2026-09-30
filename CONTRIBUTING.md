# 参与贡献

感谢你愿意花时间改进 DSH 启动器。这个项目很小，流程也就很简单。

> 参与本项目即表示你同意遵守 [贡献者公约（CODE_OF_CONDUCT.md）](CODE_OF_CONDUCT.md)：
> 友善、尊重、对事不对人。欢迎任何水平的贡献者。

## 报告问题

请走 [Issues](../../issues/new/choose) —— 那里有两个表单，按提示填就行。
如果能附上下面这些，基本可以省掉一轮来回：

- Windows 版本（Win11 23H2 / Win10 22H2 …）
- DSH 装在哪里、什么版本（面板日志里有）
- exe 同目录下的 `launcher-where.txt`（检测结果）和 `launcher-error.log`（如果有）

> 安全相关的问题**不要**开公开 Issue，见 [SECURITY.md](SECURITY.md)。

## 开发环境

| 需要 | 说明 |
|---|---|
| Windows 10 / 11 x64 | 项目依赖 WinForms + WebView2 + DWM |
| .NET Framework 4.8 | Win10 1803+ 自带，编译用的是它自带的 `csc.exe`，**不需要装 .NET SDK** |
| WebView2 Runtime | Win11 随 Edge 自带 |
| DeepSeek Harness | 想实际跑起来才需要；只编译的话不用 |

## 编译

```powershell
# 只编译到 build\，不动任何已安装的启动器
powershell -ExecutionPolicy Bypass -File build.ps1 -NoDeploy -NoSmoke
```

完整流程（编译 → 自动检测 DSH 位置 → 部署 → 冒烟验证 → 同步 dist）直接双击
`一键编译并部署.bat`。

## 改代码前请先看

[README-源码说明.txt](README-源码说明.txt) 记着这个项目踩过的坑，尤其是：

- **`FormClosed` 在 `Dispose()` 之前触发** —— 判断「窗口还在不在」必须用
  `LauncherForm.IsGone`，用 `IsDisposed` 会让进程变成没有窗口却占着单实例锁的僵尸
- **未处理异常绝不弹模态框** —— WinForms 自带的异常对话框会吃掉 `WM_QUIT`，
  统一写 `launcher-error.log`
- **DSH 的 stdout/stderr 必须重定向到文件** —— 接管道会在启动器退出时把 DSH 一起带走

## 编码约定（容易踩）

- `build.ps1` 必须存成 **UTF-8 with BOM**，否则 Windows PowerShell 5.1 按 GBK 解码直接报语法错
- `一键编译并部署.bat` 反过来**不能**有 BOM
- 代码注释用中文，和现有风格保持一致
- 主体都在单文件 `Launcher3.cs` 里，按 `// ==================== 区块名 ====================` 分段

## 提交 PR

1. Fork 后从 `main` 切分支
2. 改完先本地编译通过（`build.ps1 -NoDeploy -NoSmoke`）
3. 提 PR，按模板填；**行为有变化的话记得同步更新 `README.md` 与 `CHANGELOG.md`**
4. CI 会跑一次编译验证，绿了就会有人看

不需要为了小改动开 Issue 先讨论——直接提 PR 更快。
