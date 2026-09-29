using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DshLauncher
{
    static class Cfg
    {
        public const int Port = 3080;
        public const string Home = @"E:\DeepSeekHarness\home";
        public const string NodeExe = @"E:\DeepSeekHarness\node\node.exe";
        public const string NodeDir = @"E:\DeepSeekHarness\node";
        public const string DshBin = @"E:\DeepSeekHarness\dsh\node_modules\@deepseek-ai\dsh\lib\bin.js";
        public const string Url = "http://127.0.0.1:3080";
    }

    /// 统一的 DPI 缩放层：设计稿按 96 DPI 写，运行时按实际 DPI 放大。
    static class UI
    {
        public static float S = 1f;

        public static int P(double v) { return (int)Math.Round(v * S); }
        public static float F(double pt) { return (float)(pt * S); }
        public static Font Fnt(double pt, FontStyle st) { return new Font("Microsoft YaHei UI", F(pt), st); }
        public static Font Mono(double pt) { return new Font("Consolas", F(pt)); }
    }

    static class Th
    {
        public static readonly Color BgTop = Color.FromArgb(18, 26, 42);
        public static readonly Color BgBot = Color.FromArgb(10, 15, 27);
        public static readonly Color Card = Color.FromArgb(30, 41, 59);
        public static readonly Color CardEdge = Color.FromArgb(46, 60, 82);
        public static readonly Color Inner = Color.FromArgb(12, 18, 30);
        public static readonly Color InnerEdge = Color.FromArgb(38, 50, 68);
        public static readonly Color Text = Color.FromArgb(241, 245, 249);
        public static readonly Color Dim = Color.FromArgb(150, 165, 186);
        public static readonly Color Primary = Color.FromArgb(59, 130, 246);
        public static readonly Color Primary2 = Color.FromArgb(129, 96, 250);
        public static readonly Color Green = Color.FromArgb(52, 211, 153);
        public static readonly Color Gray = Color.FromArgb(100, 116, 139);
        public static readonly Color BtnBg = Color.FromArgb(34, 46, 63);
        public static readonly Color BtnHi = Color.FromArgb(49, 64, 88);
        public static readonly Color LogText = Color.FromArgb(126, 212, 168);

        public static GraphicsPath Round(Rectangle r, int rad)
        {
            GraphicsPath p = new GraphicsPath();
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }
    }

    class RoundedButton : Control
    {
        public bool Primary;
        private bool _hover, _down;

        public RoundedButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
            Font = UI.Fnt(11, FontStyle.Regular);
        }

        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; _down = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _down = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _down = false; Invalidate(); base.OnMouseUp(e); }
        protected override void OnEnabledChanged(EventArgs e) { Invalidate(); base.OnEnabledChanged(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Rectangle ri = new Rectangle(0, 0, Width, Height);
            int rad = UI.P(12);
            TextFormatFlags flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding;

            if (!Enabled)
            {
                using (GraphicsPath p = Th.Round(r, rad))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(24, 32, 46)))
                    g.FillPath(b, p);
                using (GraphicsPath p2 = Th.Round(r, rad))
                using (Pen pen = new Pen(Color.FromArgb(36, 47, 65)))
                    g.DrawPath(pen, p2);
                TextRenderer.DrawText(g, Text, Font, ri, Color.FromArgb(84, 98, 120), flags);
                return;
            }

            if (Primary)
            {
                Color c1 = _down ? Color.FromArgb(37, 99, 235) : (_hover ? Color.FromArgb(96, 165, 250) : Th.Primary);
                Color c2 = _down ? Color.FromArgb(91, 33, 182) : (_hover ? Color.FromArgb(139, 110, 250) : Th.Primary2);
                using (GraphicsPath p = Th.Round(r, rad))
                using (LinearGradientBrush br = new LinearGradientBrush(r, c1, c2, 0f))
                    g.FillPath(br, p);
                TextRenderer.DrawText(g, Text, Font, ri, Color.White, flags);
            }
            else
            {
                Color bg = _down ? Color.FromArgb(27, 37, 52) : (_hover ? Th.BtnHi : Th.BtnBg);
                using (GraphicsPath p = Th.Round(r, rad))
                using (SolidBrush b = new SolidBrush(bg))
                    g.FillPath(b, p);
                using (GraphicsPath p2 = Th.Round(r, rad))
                using (Pen pen = new Pen(_hover ? Color.FromArgb(74, 90, 112) : Th.CardEdge))
                    g.DrawPath(pen, p2);
                TextRenderer.DrawText(g, Text, Font, ri, _hover ? Color.White : Color.FromArgb(205, 215, 228), flags);
            }
        }
    }

    class CloseButton : Control
    {
        private bool _hover;
        public CloseButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Cursor = Cursors.Hand;
        }
        protected override void OnMouseEnter(EventArgs e) { _hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (_hover)
            {
                using (GraphicsPath p = Th.Round(new Rectangle(0, 0, Width - 1, Height - 1), UI.P(9)))
                using (SolidBrush b = new SolidBrush(Color.FromArgb(239, 68, 68)))
                    g.FillPath(b, p);
            }
            using (Pen pen = new Pen(_hover ? Color.White : Th.Dim, UI.F(1.7)))
            {
                int m = UI.P(11);
                g.DrawLine(pen, m, m, Width - m - 1, Height - m - 1);
                g.DrawLine(pen, Width - m - 1, m, m, Height - m - 1);
            }
        }
    }

    class MainForm : Form
    {
        // ---- 设计稿尺寸（96 DPI 基准）----
        private const int DW = 448;
        private const int DH = 560;
        private const int MARGIN = 20;
        private const int CARD_Y = 88;
        private const int CARD_H = 118;
        private const int BTN_W = 194;
        private const int BTN_H = 56;
        private const int BTN_GAP = 16;
        private const int BTN_Y = 226;
        private const int LOG_Y = 394;
        private const int LOG_H = 146;

        private RoundedButton _bStart, _bRestart, _bOpen, _bStop;
        private CloseButton _bClose;
        private TextBox _log;
        private System.Windows.Forms.Timer _stateTimer;
        private System.Windows.Forms.Timer _balanceTimer;
        private Process _proc;
        private System.Windows.Forms.Timer _logTimer;
        private string _dshLogPath;
        private long _dshLogOffset;
        private bool _lastRunning;
        private int _pid;
        private Image _whale;
        private bool _balanceLogged;
        private string _balanceText = "";
        private bool _dragging;
        private Point _dragOffset;
        private bool _firstShow = true;
        private bool _closeAnimDone;

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public MainForm()
        {
            // 先确定 DPI 缩放
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) UI.S = g.DpiX / 96f;

            this.Text = "DSH 启动器";
            this.FormBorderStyle = FormBorderStyle.None;
            this.ClientSize = new Size(UI.P(DW), UI.P(DH));
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Th.BgTop;
            this.ForeColor = Th.Text;
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            try { this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            LoadWhale();

            _bClose = new CloseButton();
            _bClose.SetBounds(UI.P(DW - 50), UI.P(18), UI.P(32), UI.P(32));
            _bClose.Click += delegate { Close(); };
            this.Controls.Add(_bClose);

            int bx2 = MARGIN + BTN_W + BTN_GAP;
            _bStart = MakeButton("启动 DSH", MARGIN, BTN_Y, true);
            _bRestart = MakeButton("重启 DSH", bx2, BTN_Y, false);
            _bOpen = MakeButton("打开页面", MARGIN, BTN_Y + BTN_H + 14, false);
            _bStop = MakeButton("停止 DSH", bx2, BTN_Y + BTN_H + 14, false);

            _bStart.Click += delegate { DoStart(); };
            _bRestart.Click += delegate { DoRestart(); };
            _bOpen.Click += delegate { DoOpen(); };
            _bStop.Click += delegate { DoStop(); };

            _log = new TextBox();
            _log.SetBounds(UI.P(MARGIN + 12), UI.P(LOG_Y + 10), UI.P(DW - MARGIN * 2 - 24), UI.P(LOG_H - 20));
            _log.Multiline = true;
            _log.ReadOnly = true;
            _log.ScrollBars = ScrollBars.Vertical;
            _log.HideSelection = false;
            _log.BorderStyle = BorderStyle.None;
            _log.BackColor = Th.Inner;
            _log.ForeColor = Th.LogText;
            _log.Font = UI.Mono(9);
            _log.WordWrap = true;
            _log.TabStop = false;
            this.Controls.Add(_log);

            this.MouseDown += delegate(object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left && e.Y < UI.P(74))
                {
                    _dragging = true;
                    _dragOffset = new Point(e.X, e.Y);
                }
            };
            this.MouseMove += delegate(object s, MouseEventArgs e)
            {
                if (_dragging)
                {
                    Point p = PointToScreen(new Point(e.X, e.Y));
                    this.Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
                }
            };
            this.MouseUp += delegate { _dragging = false; };

            _lastRunning = IsListening();
            if (_lastRunning) _pid = FindOwner();
            ApplyRunning(_lastRunning);
            RefreshState();
            Log("DSH 启动器已就绪");

            _stateTimer = new System.Windows.Forms.Timer();
            _stateTimer.Interval = 1200;
            _stateTimer.Tick += delegate { RefreshState(); };
            _stateTimer.Start();

            _balanceTimer = new System.Windows.Forms.Timer();
            _balanceTimer.Interval = 60000;
            _balanceTimer.Tick += delegate { RefreshBalance(); };
            _balanceTimer.Start();

            _logTimer = new System.Windows.Forms.Timer();
            _logTimer.Interval = 500;
            _logTimer.Tick += delegate { PumpDshLog(); };
            _logTimer.Start();

            RefreshBalance();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ClassStyle |= 0x20000;
                return cp;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int pref = 2;
                DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
            }
            catch { }
        }

        private void LoadWhale()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                Stream st = asm.GetManifestResourceStream("WhaleIcon");
                if (st != null) _whale = Image.FromStream(st);
            }
            catch { }
        }

        private RoundedButton MakeButton(string text, int dx, int dy, bool primary)
        {
            RoundedButton b = new RoundedButton();
            b.Text = text;
            b.Primary = primary;
            b.SetBounds(UI.P(dx), UI.P(dy), UI.P(BTN_W), UI.P(BTN_H));
            this.Controls.Add(b);
            return b;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            int W = UI.P(DW), H = UI.P(DH);

            using (LinearGradientBrush br = new LinearGradientBrush(new Rectangle(0, 0, W, H), Th.BgTop, Th.BgBot, 90f))
                g.FillRectangle(br, 0, 0, W, H);
            using (LinearGradientBrush gl = new LinearGradientBrush(new Rectangle(0, 0, W, UI.P(180)),
                       Color.FromArgb(28, 59, 130, 246), Color.FromArgb(0, 59, 130, 246), 90f))
                g.FillRectangle(gl, 0, 0, W, UI.P(180));

            // ---- 头部：logo 放大到 50，与大标题视觉平衡 ----
            int logoSize = UI.P(56);
            int logoX = UI.P(22);
            int logoY = UI.P(12);
            if (_whale != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                GraphicsPath clip = new GraphicsPath();
                clip.AddEllipse(logoX, logoY, logoSize, logoSize);
                g.SetClip(clip);
                g.DrawImage(_whale, logoX, logoY, logoSize, logoSize);
                g.ResetClip();
                using (Pen ring = new Pen(Color.FromArgb(72, 94, 122)))
                    g.DrawEllipse(ring, logoX, logoY, logoSize - 1, logoSize - 1);
                clip.Dispose();
            }

            int textX = logoX + logoSize + UI.P(16);
            using (Font f = UI.Fnt(16, FontStyle.Bold))
                TextRenderer.DrawText(g, "DSH 启动器", f, new Point(textX, UI.P(18)), Th.Text, TextFormatFlags.NoPadding);
            using (Font f2 = UI.Fnt(9, FontStyle.Regular))
                TextRenderer.DrawText(g, "DeepSeek Harness", f2, new Point(textX + 1, UI.P(54)), Th.Dim, TextFormatFlags.NoPadding);

            // ---- 状态卡片 ----
            Rectangle card = new Rectangle(UI.P(MARGIN), UI.P(CARD_Y), UI.P(DW - MARGIN * 2), UI.P(CARD_H));
            using (GraphicsPath p = Th.Round(card, UI.P(16)))
            using (SolidBrush b = new SolidBrush(Th.Card))
                g.FillPath(b, p);
            using (GraphicsPath p2 = Th.Round(card, UI.P(16)))
            using (Pen pen = new Pen(Th.CardEdge))
                g.DrawPath(pen, p2);

            Color dot = _lastRunning ? Th.Green : Th.Gray;
            int ds = UI.P(14);
            int dx = card.X + UI.P(26);
            int dy = card.Y + UI.P(36);
            using (SolidBrush glow = new SolidBrush(Color.FromArgb(58, dot)))
                g.FillEllipse(glow, dx - UI.P(6), dy - UI.P(6), ds + UI.P(12), ds + UI.P(12));
            using (SolidBrush core = new SolidBrush(dot))
                g.FillEllipse(core, dx, dy, ds, ds);

            using (Font f = UI.Fnt(16, FontStyle.Bold))
                TextRenderer.DrawText(g, _lastRunning ? "运行中" : "未运行", f,
                    new Point(card.X + UI.P(56), card.Y + UI.P(24)),
                    _lastRunning ? Th.Green : Th.Dim, TextFormatFlags.NoPadding);

            using (Font f = UI.Fnt(9, FontStyle.Regular))
            {
                TextRenderer.DrawText(g, _lastRunning ? Cfg.Url : "点下方按钮把它拉起来", f,
                    new Point(card.X + UI.P(58), card.Y + UI.P(66)), Th.Dim, TextFormatFlags.NoPadding);
                if (_lastRunning && _pid > 0)
                    TextRenderer.DrawText(g, "PID " + _pid, f,
                        new Point(card.X + UI.P(58), card.Y + UI.P(88)),
                        Color.FromArgb(112, 130, 155), TextFormatFlags.NoPadding);
            }

            if (_balanceText.Length > 0)
            {
                using (Font f = UI.Fnt(9, FontStyle.Regular))
                    TextRenderer.DrawText(g, "账户余额", f,
                        new Rectangle(card.Right - UI.P(210), card.Y + UI.P(26), UI.P(184), UI.P(20)),
                        Th.Dim, TextFormatFlags.Right | TextFormatFlags.NoPadding);
                using (Font f = UI.Fnt(17, FontStyle.Bold))
                    TextRenderer.DrawText(g, _balanceText, f,
                        new Rectangle(card.Right - UI.P(230), card.Y + UI.P(50), UI.P(204), UI.P(34)),
                        Th.Green, TextFormatFlags.Right | TextFormatFlags.NoPadding);
            }

            // ---- 日志 ----
            using (Font f = UI.Fnt(10, FontStyle.Bold))
                TextRenderer.DrawText(g, "运行日志", f, new Point(UI.P(23), UI.P(368)),
                    Color.FromArgb(122, 144, 172), TextFormatFlags.NoPadding);

            Rectangle logRect = new Rectangle(UI.P(MARGIN), UI.P(LOG_Y), UI.P(DW - MARGIN * 2), UI.P(LOG_H));
            using (GraphicsPath p = Th.Round(logRect, UI.P(14)))
            using (SolidBrush b = new SolidBrush(Th.Inner))
                g.FillPath(b, p);
            using (GraphicsPath p2 = Th.Round(logRect, UI.P(14)))
            using (Pen pen = new Pen(Th.InnerEdge))
                g.DrawPath(pen, p2);
        }

        /// <summary>DSH 的 stdout/stderr 落盘位置（与启动器进程解耦，关启动器不会波及 DSH）。</summary>
        private string DshLogPath
        {
            get
            {
                if (_dshLogPath == null)
                {
                    string dir = Path.Combine(Cfg.Home, "logs");
                    try { Directory.CreateDirectory(dir); } catch { }
                    _dshLogPath = Path.Combine(dir, "dsh-web.out.log");
                }
                return _dshLogPath;
            }
        }

        /// <summary>轮询日志文件的新增完整行，替代原来的 stdout 管道读取。</summary>
        private void PumpDshLog()
        {
            if (_proc == null) return;
            try
            {
                string path = DshLogPath;
                if (!File.Exists(path)) return;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    long avail = fs.Length - _dshLogOffset;
                    if (avail <= 0) return;
                    int want = (int)Math.Min(avail, 262144);
                    fs.Seek(_dshLogOffset, SeekOrigin.Begin);
                    byte[] buf = new byte[want];
                    int n = fs.Read(buf, 0, want);
                    if (n <= 0) return;
                    int cut = -1;
                    for (int i = n - 1; i >= 0; i--) { if (buf[i] == (byte)'\n') { cut = i; break; } }
                    if (cut < 0) return; // 还没有完整的一行，等下一轮
                    _dshLogOffset += cut + 1;
                    string[] lines = Encoding.UTF8.GetString(buf, 0, cut + 1).Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].TrimEnd('\r');
                        if (line.Length == 0) continue;
                        Log(line);
                        CaptureTokenLine(line);
                    }
                }
            }
            catch { }
        }

        private void Log(string msg)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action<string>(Log), new object[] { msg });
                return;
            }
            _log.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine);
            _log.SelectionStart = _log.TextLength;
            _log.ScrollToCaret();
        }

        private static bool IsListening()
        {
            try
            {
                IPGlobalProperties ip = IPGlobalProperties.GetIPGlobalProperties();
                IPEndPoint[] eps = ip.GetActiveTcpListeners();
                for (int i = 0; i < eps.Length; i++)
                    if (eps[i].Port == Cfg.Port) return true;
            }
            catch { }
            return false;
        }

        private static int FindOwner()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("netstat", "-ano");
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    string outp = p.StandardOutput.ReadToEnd();
                    p.WaitForExit(5000);
                    string[] lines = outp.Split('\n');
                    for (int i = 0; i < lines.Length; i++)
                    {
                        string line = lines[i].Trim();
                        if (line.Length == 0) continue;
                        if (line.IndexOf("LISTENING", StringComparison.OrdinalIgnoreCase) < 0) continue;
                        string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 5) continue;
                        string local = parts[1];
                        int colon = local.LastIndexOf(':');
                        if (colon < 0) continue;
                        int port;
                        if (!int.TryParse(local.Substring(colon + 1), out port)) continue;
                        if (port != Cfg.Port) continue;
                        int pid;
                        if (int.TryParse(parts[parts.Length - 1], out pid)) return pid;
                    }
                }
            }
            catch { }
            return 0;
        }

        private void ApplyRunning(bool running)
        {
            _bStart.Enabled = !running;
            _bRestart.Enabled = running;
            _bOpen.Enabled = running;
            _bStop.Enabled = running;
        }

        private void RefreshState()
        {
            bool running = IsListening();
            bool changed = (running != _lastRunning);
            if (changed)
            {
                _lastRunning = running;
                _pid = running ? FindOwner() : 0;
                if (running)
                {
                    Log("检测到 DSH 正在运行" + (_pid > 0 ? "（PID " + _pid + "）" : ""));
                    RefreshBalance();
                }
                else Log("DSH 已停止");
            }
            if (changed)
            {
                ApplyRunning(running);
                this.Invalidate();
            }
        }

        private static string ReadApiKey()
        {
            try
            {
                string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string path = Path.Combine(Cfg.Home, ".credentials.yaml");
                if (!File.Exists(path)) return null;
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string t = lines[i].Trim();
                    if (t.StartsWith("DEEPSEEK_API_KEY:"))
                    {
                        string v = t.Substring("DEEPSEEK_API_KEY:".Length).Trim();
                        if (v.Length > 0) return v;
                    }
                }
            }
            catch { }
            return null;
        }

        private static string FetchBalance(out string err)
        {
            err = null;
            string key = ReadApiKey();
            if (key == null) { err = "凭据里没找到 DEEPSEEK_API_KEY"; return null; }
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.deepseek.com/user/balance");
                req.Method = "GET";
                req.Headers.Add("Authorization", "Bearer " + key);
                req.Accept = "application/json";
                req.Timeout = 15000;
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string json = sr.ReadToEnd();
                    int i = json.IndexOf("\"total_balance\"");
                    if (i < 0) { err = "响应里没有 total_balance"; return null; }
                    int q1 = json.IndexOf('"', i + 15);
                    int q2 = q1 >= 0 ? json.IndexOf('"', q1 + 1) : -1;
                    if (q1 < 0 || q2 < 0) { err = "解析失败"; return null; }
                    string total = json.Substring(q1 + 1, q2 - q1 - 1);
                    string cur = "CNY";
                    int c = json.IndexOf("\"currency\"");
                    if (c >= 0)
                    {
                        int c1 = json.IndexOf('"', c + 10);
                        int c2 = c1 >= 0 ? json.IndexOf('"', c1 + 1) : -1;
                        if (c1 >= 0 && c2 > c1) cur = json.Substring(c1 + 1, c2 - c1 - 1);
                    }
                    return (cur == "CNY" ? "\u00A5" : cur + " ") + total;
                }
            }
            catch (Exception ex) { err = ex.Message; return null; }
        }

        private void RefreshBalance()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                string err;
                string v = FetchBalance(out err);
                try
                {
                    this.BeginInvoke(new Action(delegate
                    {
                        _balanceText = v != null ? v : "";
                        if (!_balanceLogged)
                        {
                            _balanceLogged = true;
                            Log(v != null ? ("余额查询成功：" + v) : ("余额查询失败：" + (err == null ? "未知原因" : err)));
                        }
                        this.Invalidate();
                    }));
                }
                catch { }
            });
        }

        private void DoStart()
        {
            if (IsListening()) { Log("DSH 已经在运行，直接打开页面"); Log("（上面的日志只在由本启动器拉起 DSH 时刷新；DSH 输出文件：" + DshLogPath + "）"); DoOpen(); return; }
            if (!File.Exists(Cfg.NodeExe)) { Log("[X] 找不到 node.exe：" + Cfg.NodeExe); return; }

            try
            {
                string exe, args;
                // --no-open：不让 DSH 自己弹浏览器，改由本启动器在独立窗口里打开
                if (File.Exists(Cfg.DshBin)) { exe = Cfg.NodeExe; args = "\"" + Cfg.DshBin + "\" web --no-open"; }
                else { exe = Path.Combine(Cfg.NodeDir, "npx.cmd"); args = "--yes \"@deepseek-ai/dsh\" web --no-open"; }

                // 关键：DSH 的 stdout/stderr 直接写日志文件，绝不能接启动器进程的匿名管道。
                // 一旦接管道，启动器一退出（点“否”只关窗口也算），管道读端就关闭，
                // DSH 下次写日志会撞上断管（EPIPE）而崩溃 —— 现象就是“关了启动器，DSH 也没了”。
                _dshLogOffset = 0;

                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe");
                psi.Arguments = "/s /c \"\"" + exe + "\" " + args + " > \"" + DshLogPath + "\" 2>&1\"";
                psi.WorkingDirectory = Cfg.NodeDir;
                psi.EnvironmentVariables["DSH_HOME"] = Cfg.Home;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;

                _proc = new Process();
                _proc.StartInfo = psi;
                _proc.EnableRaisingEvents = true;
                _proc.Exited += delegate
                {
                    try
                    {
                        this.BeginInvoke(new Action(delegate
                        {
                            PumpDshLog();
                            Log("DSH 进程已退出");
                            _proc = null;
                            RefreshState();
                        }));
                    }
                    catch { }
                };

                _proc.Start();
                Log("已启动 DSH（PID " + _proc.Id + "），就绪后会在 DSH 窗口中打开…");
                Log("DSH 输出写入日志文件：" + DshLogPath);
                ThreadPool.QueueUserWorkItem(delegate
                {
                    for (int i = 0; i < 90; i++)
                    {
                        if (IsListening()) break;
                        Thread.Sleep(500);
                    }
                    if (!IsListening()) Log("[!] 等了 45 秒仍没监听到 " + Cfg.Port + "，可在窗口里点「重试」");
                    try { this.BeginInvoke(new Action(delegate { DoOpen(); })); } catch { }
                });
            }
            catch (Exception ex) { Log("[X] 启动失败：" + ex.Message); }
            RefreshState();
        }

        private void DoStop()
        {
            int pid = (_proc != null && !_proc.HasExited) ? _proc.Id : FindOwner();
            if (pid <= 0) { Log("没有找到正在运行的 DSH"); RefreshState(); return; }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/PID " + pid + " /T /F");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi)) { p.WaitForExit(8000); }
                Log("已停止 DSH（PID " + pid + "）");
                _proc = null;
                _dshLogOffset = 0;
            }
            catch (Exception ex) { Log("[X] 停止失败：" + ex.Message); }

            for (int i = 0; i < 20; i++)
            {
                if (!IsListening()) break;
                Thread.Sleep(200);
                Application.DoEvents();
            }
            RefreshState();
        }

        private void DoRestart()
        {
            Log("—— 重启 DSH ——");
            DoStop();
            Thread.Sleep(800);
            Application.DoEvents();
            DoStart();
        }


        /// <summary>从 DSH 输出里抓 launch token，落一份到 $DSH_HOME/guard/logs（供 qq-bridge 自动同步）。</summary>
        private void CaptureTokenLine(string line)
        {
            try
            {
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(line, @"[?&]token=([A-Za-z0-9_\-]+)");
                if (!m.Success) return;
                string dir = Path.Combine(Cfg.Home, "guard", "logs");
                Directory.CreateDirectory(dir);
                string file = Path.Combine(dir, "server-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".out.log");
                string content = "dsh web: " + Cfg.Url + "/?token=" + m.Groups[1].Value + Environment.NewLine
                    + "dsh web: opening the default browser; pass --no-open to disable" + Environment.NewLine;
                File.WriteAllText(file, content, new UTF8Encoding(false));
                Log("[token] " + m.Groups[1].Value + "   ← 已写入 guard 日志，可复制");
                FileInfo[] olds = new DirectoryInfo(dir).GetFiles("server-*.out.log");
                Array.Sort(olds, delegate(FileInfo a, FileInfo b) { return b.LastWriteTime.CompareTo(a.LastWriteTime); });
                for (int i = 5; i < olds.Length; i++) { try { olds[i].Delete(); } catch { } }
            }
            catch { }
        }

        private void DoOpen()
        {
            try
            {
                PageForm.ShowPage(Cfg.Url);
                Log("已在 DSH 窗口中打开界面");
            }
            catch (Exception ex)
            {
                try { Process.Start(Cfg.Url); Log("窗口组件不可用（" + ex.Message + "），已交给默认浏览器"); }
                catch (Exception ex2) { Log("[X] 打不开界面：" + ex2.Message); }
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (!_firstShow) return;
            _firstShow = false;
            Rectangle target = Bounds;
            Opacity = 0;
            Bounds = Anim.Shrink(target, 0.96);
            Anim.Run(this, Bounds, target, 0, 1, 190, null);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_closeAnimDone)
            {
                if (IsListening())
                {
                    DialogResult r = MessageBox.Show(
                        "DSH 还在运行。\n\n· 是  —— 停掉 DSH 再关闭\n· 否  —— 只关这个窗口，DSH 继续跑\n· 取消 —— 什么都不做",
                        "DSH 启动器", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question,
                        MessageBoxDefaultButton.Button1);
                    if (r == DialogResult.Cancel) { e.Cancel = true; return; }
                    if (r == DialogResult.Yes) DoStop();
                }
                e.Cancel = true;
                _closeAnimDone = true;
                Anim.Run(this, Bounds, Anim.Shrink(Bounds, 0.96), 1, 0, 150, delegate { Close(); });
                return;
            }
            base.OnFormClosing(e);
        }

        [STAThread]
        static void Main(string[] args)
        {
            try { SetProcessDPIAware(); } catch { }
            bool pageOnly = false;
            foreach (string a in args) { if (a == "--page") pageOnly = true; }
            try
            {
                ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (pageOnly) { Application.Run(new PageForm(DshPageUrl.Resolve(Cfg.Url))); return; }
            Application.Run(new DshContext());
        }

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
    }

    /// <summary>极简窗口动画：矩形缓动（ease-out）+ 可选透明度。</summary>
    static class Anim
    {
        /// <summary>按比例向中心收缩后的矩形。</summary>
        public static Rectangle Shrink(Rectangle r, double f)
        {
            int w = (int)Math.Round(r.Width * f), h = (int)Math.Round(r.Height * f);
            return new Rectangle(r.X + (r.Width - w) / 2, r.Y + (r.Height - h) / 2, w, h);
        }

        public static void Run(Form f, Rectangle from, Rectangle to, double opFrom, double opTo, int ms, Action done)
        {
            DateTime start = DateTime.Now;
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 15;
            t.Tick += delegate
            {
                double p = Math.Min(1.0, (DateTime.Now - start).TotalMilliseconds / ms);
                double e = 1.0 - Math.Pow(1.0 - p, 3.0);
                try
                {
                    f.Bounds = new Rectangle(
                        (int)Math.Round(from.X + (to.X - from.X) * e),
                        (int)Math.Round(from.Y + (to.Y - from.Y) * e),
                        (int)Math.Round(from.Width + (to.Width - from.Width) * e),
                        (int)Math.Round(from.Height + (to.Height - from.Height) * e));
                }
                catch { }
                if (opFrom >= 0 && opTo >= 0)
                {
                    try { f.Opacity = opFrom + (opTo - opFrom) * e; } catch { }
                }
                if (p >= 1.0) { t.Stop(); t.Dispose(); if (done != null) done(); }
            };
            t.Start();
        }
    }

    /// <summary>应用生命周期：面板窗口与页面窗口各自独立关闭，两个都关掉才退出进程。</summary>
    class DshContext : ApplicationContext
    {
        public static DshContext Current;
        private readonly MainForm _panel;

        public DshContext()
        {
            Current = this;
            _panel = new MainForm();
            _panel.FormClosed += delegate { Tick(); };
            _panel.Show();
        }

        public void Tick()
        {
            bool panelOpen = (_panel != null) && !_panel.IsDisposed;
            bool pageOpen = PageForm.IsOpen;
            if (!panelOpen && !pageOpen) ExitThread();
        }
    }

    /// <summary>把 DSH 地址补上 token（从 DSH 日志里抓当前 token）。</summary>
    static class DshPageUrl
    {
        public const string Default = "http://127.0.0.1:3080";

        private static string HomeDir()
        {
            if (!string.IsNullOrEmpty(Cfg.Home) && Directory.Exists(Cfg.Home)) return Cfg.Home;
            string env = Environment.GetEnvironmentVariable("DSH_HOME");
            if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) return env;
            return null;
        }

        private static string ReadLastToken(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                string text;
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    long len = fs.Length;
                    int take = (int)Math.Min(len, 65536L);
                    fs.Seek(len - take, SeekOrigin.Begin);
                    byte[] buf = new byte[take];
                    int n = fs.Read(buf, 0, take);
                    text = Encoding.UTF8.GetString(buf, 0, n);
                }
                System.Text.RegularExpressions.MatchCollection ms =
                    System.Text.RegularExpressions.Regex.Matches(text, "token=([A-Za-z0-9_-]+)");
                if (ms.Count == 0) return null;
                return ms[ms.Count - 1].Groups[1].Value;
            }
            catch { return null; }
        }

        private static string FindToken()
        {
            string home = HomeDir();
            if (home == null) return null;
            string t = ReadLastToken(Path.Combine(Path.Combine(home, "logs"), "dsh-web.out.log"));
            if (!string.IsNullOrEmpty(t)) return t;
            try
            {
                string g = Path.Combine(Path.Combine(home, "guard"), "logs");
                if (Directory.Exists(g))
                {
                    FileInfo newest = null;
                    foreach (string f in Directory.GetFiles(g, "server-*.out.log"))
                    {
                        FileInfo fi = new FileInfo(f);
                        if (newest == null || fi.LastWriteTimeUtc > newest.LastWriteTimeUtc) newest = fi;
                    }
                    if (newest != null) return ReadLastToken(newest.FullName);
                }
            }
            catch { }
            return null;
        }

        public static string Resolve(string raw)
        {
            string url = string.IsNullOrEmpty(raw) ? Default : raw;
            if (url.IndexOf("token=", StringComparison.OrdinalIgnoreCase) >= 0) return url;
            string token = FindToken();
            if (string.IsNullOrEmpty(token)) return url;
            return url + (url.IndexOf('?') >= 0 ? "&" : "/?") + "token=" + token;
        }
    }

    /// <summary>启动器自带的 DSH 页面窗口（WebView2 + 自绘深色边框），同进程内只开一个。</summary>
    class PageForm : Form
    {
        private const string WindowCaption = "DSH-HFRin调试版";
        private static PageForm _open;
        private WebView2 _web;
        private readonly string _url;
        private readonly int _scale;
        private readonly int _borderWide;
        private readonly int _borderThin;
        private int _border;
        private bool _maxed;
        private bool _converting;
        private Rectangle _preMaxBounds;
        private readonly int _titleH;
        private readonly Font _titleFont;
        private readonly Image _appIcon;
        private int _hoverBtn = -1;
        private bool _dragging;

        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr h, int msg, IntPtr w, IntPtr l);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        public static bool IsOpen { get { return _open != null && !_open.IsDisposed; } }

        public static void ShowPage(string rawUrl)
        {
            string url = DshPageUrl.Resolve(rawUrl);
            if (_open == null || _open.IsDisposed) _open = new PageForm(url);
            if (_open.WindowState == FormWindowState.Minimized) _open.WindowState = FormWindowState.Normal;
            _open.Show();
            _open.Activate();
            _open.BringToFront();
        }

        /// <summary>标题栏字体：优先圆体（幼圆），没有就退回雅黑。</summary>
        private static Font MakeTitleFont(float size)
        {
            string[] candidates = new string[] { "Microsoft YaHei UI", "微软雅黑", "Segoe UI" };
            foreach (string name in candidates)
            {
                try { return new Font(new FontFamily(name), size, FontStyle.Bold); }
                catch { }
            }
            return new Font(FontFamily.GenericSansSerif, size, FontStyle.Bold);
        }

        public PageForm(string url)
        {
            _url = url;
            float sc = 1f;
            try { using (Graphics gg = Graphics.FromHwnd(IntPtr.Zero)) sc = gg.DpiX / 96f; } catch { }
            _scale = Math.Max(1, (int)Math.Round(sc));
            _borderWide = 8 * _scale;
            _borderThin = 3 * _scale;
            _border = _borderWide;
            _titleH = 32 * _scale;
            _titleFont = MakeTitleFont(8.5f * sc);
            try { _appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath).ToBitmap(); } catch { }

            Text = WindowCaption;
            // 保留系统边框样式（WS_CAPTION），Win11 才会给这个窗口播原生过渡动画；
            // 视觉上的无边框由 WM_NCCALCSIZE 抹掉非客户区来实现。
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(9, 14, 26);
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);

            int w = (int)Math.Round(1400 * sc), h = (int)Math.Round(900 * sc);
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            if (w > wa.Width - 80) w = wa.Width - 80;
            if (h > wa.Height - 80) h = wa.Height - 80;
            ClientSize = new Size(w, h);

            _web = new WebView2();
            Controls.Add(_web);
            LayoutWeb();
            Load += async delegate { await InitAsync(); };
            FormClosed += delegate { if (DshContext.Current != null) DshContext.Current.Tick(); };
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int allow = 0; DwmSetWindowAttribute(this.Handle, 3, ref allow, sizeof(int)); } catch { }  // 允许系统窗口过渡
            ApplyCornerPreference();
        }

        private void LayoutWeb()
        {
            if (_web == null) return;
            _web.SetBounds(_border, _titleH,
                Math.Max(0, ClientSize.Width - _border * 2),
                Math.Max(0, ClientSize.Height - _titleH - _border));
        }

        /// <summary>最大化时把边框自动收细，还原时恢复原宽度。</summary>
        private void UpdateBorderForState()
        {
            int want = _maxed ? _borderThin : _borderWide;
            if (want != _border) _border = want;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 系统最大化（拖到屏幕顶部 / Win+Up）：转成精确贴合的自定义最大化，
            // 否则带边框的窗口会比工作区大一圈，底部压住任务栏
            if (!_converting && !_maxed && WindowState == FormWindowState.Maximized)
            {
                _converting = true;
                WindowState = FormWindowState.Normal;
                BeginInvoke(new Action(delegate
                {
                    _maxed = true;
                    Bounds = Screen.FromControl(this).WorkingArea;
                    UpdateBorderForState();
                    LayoutWeb();
                    Invalidate();
                    _converting = false;
                }));
            }
            UpdateBorderForState();
            LayoutWeb();
            ApplyCornerPreference();
            Invalidate();
        }

        private Rectangle BtnRect(int i)
        {
            int bw = 46 * _scale;
            return new Rectangle(ClientSize.Width - bw * (i + 1), 0, bw, _titleH);
        }

        private int HitButton(Point p)
        {
            for (int i = 0; i < 3; i++) if (BtnRect(i).Contains(p)) return i;
            return -1;
        }

        /// <summary>自定义最大化：窗口矩形精确等于工作区，避免系统最大化多出的一圈边框压到任务栏。</summary>
        private void ToggleMax()
        {
            if (!_maxed)
            {
                _preMaxBounds = Bounds;
                _maxed = true;
                Bounds = Screen.FromControl(this).WorkingArea;
            }
            else
            {
                _maxed = false;
                if (_preMaxBounds.Width > 0) Bounds = _preMaxBounds;
            }
            UpdateBorderForState();
            LayoutWeb();
            Invalidate();
        }

        /// <summary>最大化时去掉窗口圆角，否则四角会露出底下的画面；还原时恢复。</summary>
        private void ApplyCornerPreference()
        {
            try
            {
                int pref = _maxed ? 1 : 2;  // 1=不做圆角 2=圆角
                DwmSetWindowAttribute(this.Handle, 33, ref pref, sizeof(int));
            }
            catch { }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int W = ClientSize.Width, H = ClientSize.Height;

            using (SolidBrush b = new SolidBrush(Color.FromArgb(9, 14, 26)))
                g.FillRectangle(b, 0, 0, W, H);

            Rectangle tr = new Rectangle(0, 0, W, _titleH);
            if (tr.Height > 0 && tr.Width > 0)
                using (System.Drawing.Drawing2D.LinearGradientBrush br =
                    new System.Drawing.Drawing2D.LinearGradientBrush(tr,
                        Color.FromArgb(19, 28, 47), Color.FromArgb(12, 19, 33), 90f))
                    g.FillRectangle(br, tr);

            int pad = 13 * _scale, iconSize = 19 * _scale;
            if (_appIcon != null) g.DrawImage(_appIcon, pad, (_titleH - iconSize) / 2, iconSize, iconSize);
            TextRenderer.DrawText(g, WindowCaption, _titleFont,
                new Rectangle(pad + iconSize + 10 * _scale, 0, Math.Max(10, W - 300 * _scale), _titleH),
                Color.FromArgb(222, 232, 246),
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPadding);

            for (int i = 0; i < 3; i++) DrawButton(g, i);

            using (Pen pen = new Pen(Color.FromArgb(38, 52, 74)))
                g.DrawLine(pen, _border, _titleH, W - _border, _titleH);

            // 外圈：左上亮、右下暗的渐变描边，做出受光感
            using (System.Drawing.Drawing2D.GraphicsPath p = RoundPath(new Rectangle(0, 0, W - 1, H - 1), 13 * _scale))
            using (System.Drawing.Drawing2D.LinearGradientBrush lb = new System.Drawing.Drawing2D.LinearGradientBrush(
                new Rectangle(0, 0, W, H), Color.FromArgb(138, 180, 236), Color.FromArgb(46, 66, 98), 42f))
            using (Pen pen = new Pen(lb, Math.Max(1, _scale)))
                g.DrawPath(pen, p);

            // 内圈：一层暗描边，让边框有厚度
            using (System.Drawing.Drawing2D.GraphicsPath p2 = RoundPath(
                new Rectangle(_scale, _scale, W - 1 - 2 * _scale, H - 1 - 2 * _scale), 12 * _scale))
            using (Pen pen2 = new Pen(Color.FromArgb(20, 28, 44), Math.Max(1, _scale)))
                g.DrawPath(pen2, p2);
        }

        private static System.Drawing.Drawing2D.GraphicsPath RoundPath(Rectangle r, int rad)
        {
            System.Drawing.Drawing2D.GraphicsPath p = new System.Drawing.Drawing2D.GraphicsPath();
            int d = rad * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { p.AddRectangle(r); return p; }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void DrawButton(Graphics g, int i)
        {
            Rectangle r = BtnRect(i);
            bool hover = (_hoverBtn == i);
            if (hover)
            {
                Color c = (i == 0) ? Color.FromArgb(220, 38, 38) : Color.FromArgb(35, 48, 68);
                using (SolidBrush b = new SolidBrush(c)) g.FillRectangle(b, r);
            }
            Color stroke = (hover && i == 0) ? Color.White : Color.FromArgb(186, 202, 224);
            using (Pen pen = new Pen(stroke, Math.Max(1, _scale)))
            {
                int cx = r.X + r.Width / 2, cy = r.Y + r.Height / 2, s = 5 * _scale;
                if (i == 0)
                {
                    g.DrawLine(pen, cx - s, cy - s, cx + s, cy + s);
                    g.DrawLine(pen, cx - s, cy + s, cx + s, cy - s);
                }
                else if (i == 1) g.DrawRectangle(pen, cx - s, cy - s, s * 2, s * 2);
                else g.DrawLine(pen, cx - s, cy, cx + s, cy);
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int h = HitButton(e.Location);
            if (h != _hoverBtn) { _hoverBtn = h; Invalidate(); }
            if (_dragging && e.Button == MouseButtons.Left)
            {
                _dragging = false;
                ReleaseCapture();
                SendMessage(Handle, 0xA1, (IntPtr)0x2, IntPtr.Zero);
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (_hoverBtn != -1) { _hoverBtn = -1; Invalidate(); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left) return;
            int b = HitButton(e.Location);
            if (b == 0) { Close(); return; }
            if (b == 1) { ToggleMax(); return; }
            if (b == 2) { WindowState = FormWindowState.Minimized; return; }
            if (e.Y < _titleH) _dragging = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _dragging = false;
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            base.OnMouseDoubleClick(e);
            if (e.Button == MouseButtons.Left && e.Y < _titleH && HitButton(e.Location) < 0) ToggleMax();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        [StructLayout(LayoutKind.Sequential)]
        private struct NCCALCSIZE_PARAMS { public RECT rgrc0; public RECT rgrc1; public RECT rgrc2; public IntPtr lppos; }
        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO { public int cbSize; public RECT rcMonitor; public RECT rcWork; public int dwFlags; }
        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);
        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved, ptMaxSize, ptMaxPosition, ptMinTrackSize, ptMaxTrackSize;
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCHITTEST = 0x84;
            const int WM_GETMINMAXINFO = 0x24;
            const int WM_NCCALCSIZE = 0x83;
            if (m.Msg == WM_NCCALCSIZE && m.WParam != IntPtr.Zero)
            {
                // 客户区 = 整个窗口矩形（边框全部由自绘）；自定义最大化下窗口矩形正好等于工作区
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == WM_GETMINMAXINFO)
            {
                // 自己定最大化边界：精确贴合工作区，不留缝也不压任务栏
                Rectangle wa = Screen.FromControl(this).WorkingArea;
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(m.LParam, typeof(MINMAXINFO));
                mmi.ptMaxPosition.X = wa.Left;
                mmi.ptMaxPosition.Y = wa.Top;
                mmi.ptMaxSize.X = wa.Width;
                mmi.ptMaxSize.Y = wa.Height;
                Marshal.StructureToPtr(mmi, m.LParam, false);
                m.Result = IntPtr.Zero;
                return;
            }
            if (m.Msg == WM_NCHITTEST)
            {
                base.WndProc(ref m);
                if ((int)m.Result != 1) return;
                int sx = unchecked((short)(long)m.LParam);
                int sy = unchecked((short)(((long)m.LParam) >> 16));
                Point p = PointToClient(new Point(sx, sy));
                if (!_maxed)
                {
                    int grab = _border + 2 * _scale;
                    bool left = p.X <= grab, right = p.X >= ClientSize.Width - grab;
                    bool top = p.Y <= grab, bottom = p.Y >= ClientSize.Height - grab;
                    if (top && left) { m.Result = (IntPtr)13; return; }
                    if (top && right) { m.Result = (IntPtr)14; return; }
                    if (bottom && left) { m.Result = (IntPtr)16; return; }
                    if (bottom && right) { m.Result = (IntPtr)17; return; }
                    if (left) { m.Result = (IntPtr)10; return; }
                    if (right) { m.Result = (IntPtr)11; return; }
                    if (top) { m.Result = (IntPtr)12; return; }
                    if (bottom) { m.Result = (IntPtr)15; return; }
                }
                // 标题栏交给 WinForms 自己收事件：拖动走系统的 HTCAPTION（保留分屏），
                // 双击则走自定义最大化（系统最大化会多出一圈边框压住任务栏）
                return;
            }
            base.WndProc(ref m);
        }

        private static string DataDir()
        {
            string d = Path.Combine(Cfg.Home, "window-data");
            try { Directory.CreateDirectory(d); } catch { }
            return d;
        }

        private static void Trace(string msg)
        {
            try
            {
                File.AppendAllText(Path.Combine(DataDir(), "window.log"),
                    DateTime.Now.ToString("HH:mm:ss") + "  " + msg + Environment.NewLine, Encoding.UTF8);
            }
            catch { }
        }

        private async System.Threading.Tasks.Task InitAsync()
        {
            try
            {
                CoreWebView2EnvironmentOptions opts = new CoreWebView2EnvironmentOptions();
                opts.AdditionalBrowserArguments = "--no-proxy-server";
                CoreWebView2Environment env = await CoreWebView2Environment.CreateAsync(null, DataDir(), opts);
                await _web.EnsureCoreWebView2Async(env);
                CoreWebView2 core = _web.CoreWebView2;

                core.Settings.AreDevToolsEnabled = true;
                core.Settings.IsStatusBarEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = true;
                core.Settings.IsZoomControlEnabled = true;
                core.Settings.AreBrowserAcceleratorKeysEnabled = true;
                core.DocumentTitleChanged += delegate { try { this.Text = WindowCaption; } catch { } };

                core.NewWindowRequested += delegate(object s, CoreWebView2NewWindowRequestedEventArgs e)
                {
                    e.Handled = true;
                    try { Process.Start(e.Uri); } catch { }
                };
                core.NavigationCompleted += delegate(object s, CoreWebView2NavigationCompletedEventArgs e)
                {
                    Trace("导航 ok=" + e.IsSuccess + " err=" + e.WebErrorStatus);
                    if (!e.IsSuccess) ShowOffline(e.WebErrorStatus.ToString());
                };
                Trace("开始导航 " + _url);
                core.Navigate(_url);
            }
            catch (Exception ex)
            {
                Trace("初始化失败: " + ex.Message);
                MessageBox.Show("页面窗口初始化失败：" + Environment.NewLine + ex.Message, "DSH",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void ShowOffline(string reason)
        {
            string html =
                "<!doctype html><meta charset=\"utf-8\"><body style=\"margin:0;height:100vh;display:flex;align-items:center;justify-content:center;background:#0b1220;color:#cbd5e1;font:14px 'Microsoft YaHei UI',sans-serif\">"
                + "<div style=\"text-align:center;line-height:2\">"
                + "<div style=\"font-size:19px;color:#e2e8f0\">连不上 DSH</div>"
                + "<div style=\"color:#7d8ca3\">地址 " + _url + "</div>"
                + "<div style=\"color:#64748b;font-size:12px\">" + reason + "</div>"
                + "<div style=\"margin-top:16px\"><a href=\"" + _url + "\" style=\"color:#60a5fa;text-decoration:none;border:1px solid #334155;padding:8px 20px;border-radius:8px\">重试</a></div>"
                + "</div></body>";
            _web.CoreWebView2.NavigateToString(html);
        }
    }
}
