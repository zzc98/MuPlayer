using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("MuPlayer")]
[assembly: AssemblyProduct("MuPlayer")]
[assembly: AssemblyVersion("2.1.0.0")]

namespace MuPlayer
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    static class Ui
    {
        public static float S = 1f;
        public static int Sc(int v) { return (int)Math.Round(v * S); }
    }

    static class Palette
    {
        public static readonly Color Bg = Color.FromArgb(26, 27, 33);
        public static readonly Color Bar = Color.FromArgb(34, 35, 42);
        public static readonly Color RowHover = Color.FromArgb(38, 39, 48);
        public static readonly Color RowCurrent = Color.FromArgb(44, 45, 56);
        public static readonly Color Hairline = Color.FromArgb(52, 53, 64);
        public static readonly Color Text = Color.FromArgb(245, 246, 248);
        public static readonly Color Muted = Color.FromArgb(152, 153, 165);
        public static readonly Color Dim = Color.FromArgb(110, 111, 123);
        public static readonly Color Accent = Color.FromArgb(30, 215, 96);
        public static readonly Color Track = Color.FromArgb(70, 71, 82);
        public static readonly Color HoverBg = Color.FromArgb(48, 49, 60);
        public static readonly Color PressBg = Color.FromArgb(58, 59, 72);
        public static readonly Color CloseRed = Color.FromArgb(232, 17, 35);

        public static Color Hsl(double h, double s, double l)
        {
            h = ((h % 360) + 360) % 360 / 360.0;
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            return Color.FromArgb(255, Hue(p, q, h + 1.0 / 3), Hue(p, q, h), Hue(p, q, h - 1.0 / 3));
        }

        static int Hue(double p, double q, double t)
        {
            if (t < 0) t += 1;
            if (t > 1) t -= 1;
            double v;
            if (t < 1.0 / 6) v = p + (q - p) * 6 * t;
            else if (t < 1.0 / 2) v = q;
            else if (t < 2.0 / 3) v = p + (q - p) * (2.0 / 3 - t) * 6;
            else v = p;
            return (int)Math.Round(v * 255);
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    static class Fonts
    {
        public static readonly Font Title = new Font("Microsoft YaHei UI", 15F, FontStyle.Bold);
        public static readonly Font Artist = new Font("Microsoft YaHei UI", 9.5F);
        public static readonly Font Head = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        public static readonly Font Small = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font Tiny = new Font("Microsoft YaHei UI", 8.5F);
        public static readonly Font Row = new Font("Microsoft YaHei UI", 10F);
        public static readonly Font RowArtist = new Font("Microsoft YaHei UI", 8.5F);
        public static readonly Font Index = new Font("Microsoft YaHei UI", 9F);
        public static readonly Font App = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold);
        public static readonly Font Logo = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold);
        public static readonly Font Art = new Font("Microsoft YaHei UI", 34F, FontStyle.Bold);
    }

    static class Gfx
    {
        static readonly Dictionary<int, SolidBrush> Brushes = new Dictionary<int, SolidBrush>();
        static readonly Dictionary<int, Font> Mdl2Fonts = new Dictionary<int, Font>();

        public static SolidBrush Brush(Color c)
        {
            int key = c.ToArgb();
            SolidBrush b;
            if (!Brushes.TryGetValue(key, out b))
            {
                b = new SolidBrush(c);
                Brushes[key] = b;
            }
            return b;
        }

        public static Font Mdl2(int size)
        {
            Font f;
            if (!Mdl2Fonts.TryGetValue(size, out f))
            {
                f = new Font("Segoe MDL2 Assets", size);
                Mdl2Fonts[size] = f;
            }
            return f;
        }

        public static readonly StringFormat Center = Make(StringAlignment.Center);
        public static readonly StringFormat Near = Make(StringAlignment.Near);
        public static readonly StringFormat Far = Make(StringAlignment.Far);

        static StringFormat Make(StringAlignment a)
        {
            StringFormat sf = new StringFormat();
            sf.Alignment = a;
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            return sf;
        }

        public static readonly Pen Hairline = new Pen(Palette.Hairline);
    }

    class IconButton : Control
    {
        public string Glyph = "";
        public Color GlyphColor = Palette.Text;
        public bool Circle;
        public bool AccentFill;
        public bool CloseStyle;
        public int GlyphSize = 14;
        bool hover, pressed;

        public IconButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Bg;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; pressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { pressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { pressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            Color back = Color.Empty;
            if (AccentFill) back = pressed ? ControlPaint.Dark(Palette.Accent, 0.1f) : (hover ? ControlPaint.Light(Palette.Accent, 0.1f) : Palette.Accent);
            else if (pressed) back = Palette.PressBg;
            else if (hover) back = CloseStyle ? Palette.CloseRed : Palette.HoverBg;
            if (back != Color.Empty)
            {
                using (GraphicsPath path = Circle ? Palette.Round(r, Height / 2) : Palette.Round(r, Ui.Sc(8)))
                {
                    g.FillPath(Gfx.Brush(back), path);
                }
            }
            Color glyphColor = GlyphColor;
            if (CloseStyle && hover) glyphColor = Color.White;
            if (AccentFill) glyphColor = Color.FromArgb(18, 18, 22);
            g.DrawString(Glyph, Gfx.Mdl2(GlyphSize), Gfx.Brush(glyphColor), new RectangleF(0, 1, Width, Height), Gfx.Center);
        }
    }

    class Slider : Control
    {
        int maximum = 100;
        int val;
        bool hover, dragging;

        public event EventHandler UserScroll;
        public event EventHandler UserScrollEnd;

        public Slider()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Bar;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public int Maximum { get { return maximum; } set { maximum = Math.Max(1, value); if (val > maximum) val = maximum; Invalidate(); } }
        public int Value
        {
            get { return val; }
            set { int v = Math.Max(0, Math.Min(maximum, value)); if (v != val) { val = v; Invalidate(); } }
        }

        int ValueFromX(int x)
        {
            int pad = Ui.Sc(7);
            double frac = (x - pad) / (double)Math.Max(1, Width - pad * 2);
            return (int)Math.Round(Math.Max(0, Math.Min(1, frac)) * maximum);
        }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            dragging = true;
            Value = ValueFromX(e.X);
            if (UserScroll != null) UserScroll(this, EventArgs.Empty);
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                Value = ValueFromX(e.X);
                if (UserScroll != null) UserScroll(this, EventArgs.Empty);
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging)
            {
                dragging = false;
                Invalidate();
                if (UserScrollEnd != null) UserScrollEnd(this, EventArgs.Empty);
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cy = Height / 2;
            int pad = Ui.Sc(7);
            int th = Ui.Sc(4);
            Rectangle track = new Rectangle(pad, cy - th / 2, Width - pad * 2, th);
            using (GraphicsPath path = Palette.Round(track, th / 2))
            {
                g.FillPath(Gfx.Brush(Palette.Track), path);
            }
            int fillW = (int)((Width - pad * 2) * (val / (double)maximum));
            if (fillW > 0)
            {
                Rectangle fill = new Rectangle(pad, cy - th / 2, fillW, th);
                using (GraphicsPath path = Palette.Round(fill, th / 2))
                {
                    g.FillPath(Gfx.Brush(hover || dragging ? Palette.Accent : Color.FromArgb(210, 211, 218)), path);
                }
            }
            int tx = pad + fillW;
            int radius = hover || dragging ? Ui.Sc(7) : Ui.Sc(5);
            g.FillEllipse(Gfx.Brush(Color.White), tx - radius, cy - radius, radius * 2, radius * 2);
        }
    }

    class SlimScrollBar : Control
    {
        int maximum, pageSize = 1, value;
        bool hover, dragging;
        int dragOffset;

        public event EventHandler UserScroll;

        public SlimScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.Bg;
            Cursor = Cursors.Hand;
            TabStop = false;
        }

        public int Maximum { get { return maximum; } set { maximum = Math.Max(0, value); Clamp(); Invalidate(); } }
        public int PageSize { get { return pageSize; } set { pageSize = Math.Max(1, value); Invalidate(); } }
        public int Value { get { return this.value; } set { int v = Math.Max(0, Math.Min(maximum, value)); if (v != this.value) { this.value = v; Invalidate(); } } }

        void Clamp() { if (value > maximum) value = maximum; }

        int Range { get { return maximum + pageSize; } }
        int ThumbH { get { return Math.Max(Ui.Sc(28), (int)(Height * (pageSize / (double)Range))); } }
        int ThumbY { get { return Range <= 0 ? 0 : (int)((Height - ThumbH) * (value / (double)Math.Max(1, maximum))); } }

        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e)
        {
            int ty = ThumbY;
            if (e.Y >= ty && e.Y <= ty + ThumbH) { dragging = true; dragOffset = e.Y - ty; }
            else { Value = (int)((e.Y / (double)Height) * Range - pageSize / 2.0); Fire(); }
            base.OnMouseDown(e);
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
            {
                double frac = (e.Y - dragOffset) / (double)Math.Max(1, Height - ThumbH);
                Value = (int)Math.Round(frac * maximum);
                Fire();
            }
            base.OnMouseMove(e);
        }
        protected override void OnMouseUp(MouseEventArgs e) { dragging = false; base.OnMouseUp(e); }

        void Fire() { if (UserScroll != null) UserScroll(this, EventArgs.Empty); }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (maximum <= 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, ThumbY, Width - 1, ThumbH);
            using (GraphicsPath path = Palette.Round(r, Width / 2))
            {
                g.FillPath(Gfx.Brush(hover || dragging ? Palette.Dim : Palette.Track), path);
            }
        }
    }

    class DarkListBox : ListBox
    {
        protected override void WndProc(ref Message m)
        {
            // 空白区点击会让原生 ListBox 重发选中事件导致误切歌，直接吞掉
            if (m.Msg == 0x0201)
            {
                int lp = m.LParam.ToInt32();
                Point p = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
                if (IndexFromPoint(p) < 0) return;
            }
            // 原生 ListBox 重绘前先擦背景会导致闪烁；背景改由每个项自绘覆盖
            if (m.Msg == 0x0014)
            {
                m.Result = (IntPtr)1;
                return;
            }
            if (m.Msg == 0x000F)
            {
                base.WndProc(ref m);
                int y = Items.Count * ItemHeight;
                if (y < ClientSize.Height)
                {
                    using (Graphics g = Graphics.FromHwnd(Handle))
                    using (SolidBrush b = new SolidBrush(Palette.Bg))
                    {
                        g.FillRectangle(b, 0, y, ClientSize.Width, ClientSize.Height - y);
                    }
                }
                return;
            }
            base.WndProc(ref m);
        }
    }

    class EqOverlay : Control
    {
        public bool Playing;

        public EqOverlay()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            BackColor = Palette.RowCurrent;
            TabStop = false;
            Visible = false;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084)
            {
                m.Result = (IntPtr)(-1);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (Playing)
            {
                double t = DateTime.Now.TimeOfDay.TotalSeconds;
                for (int i = 0; i < 4; i++)
                {
                    int barH = Ui.Sc(4 + (int)(Math.Abs(Math.Sin(t * 3.2 + i * 1.7)) * 12));
                    g.FillRectangle(Gfx.Brush(Palette.Accent), Ui.Sc(4 + i * 5), Height - Ui.Sc(12) - barH, Ui.Sc(3), barH);
                }
            }
            else
            {
                g.DrawString("\uE769", Gfx.Mdl2(10), Gfx.Brush(Palette.Accent), new RectangleF(0, 0, Width, Height), Gfx.Center);
            }
        }
    }

    interface IBackend : IDisposable
    {
        bool Open(string path);
        void Play();
        void Pause();
        void Stop();
        void SetVolume(double v);
        bool Seek(double seconds);
        double Position { get; }
        double Duration { get; }
    }

    class MciBackend : IBackend
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        static extern int mciSendString(string command, StringBuilder buffer, int bufferSize, IntPtr callback);

        const string Alias = "muplayer";
        bool open;
        readonly StringBuilder sb = new StringBuilder(256);

        string Send(string command)
        {
            sb.Length = 0;
            return mciSendString(command, sb, sb.Capacity, IntPtr.Zero) == 0 ? sb.ToString() : null;
        }

        public bool Open(string path)
        {
            Close();
            if (Send("open \"" + path + "\" alias " + Alias) == null) return false;
            open = true;
            Send("set " + Alias + " time format milliseconds");
            return true;
        }

        public void Play() { if (open) Send("play " + Alias); }
        public void Pause() { if (open) Send("pause " + Alias); }
        public void Stop() { if (open) Send("stop " + Alias); }
        public void SetVolume(double v) { if (open) Send("setaudio " + Alias + " volume to " + (int)(v * 1000)); }
        public bool Seek(double seconds) { return open && Send("play " + Alias + " from " + (int)(seconds * 1000)) != null; }

        public double Position
        {
            get
            {
                double ms;
                string r = open ? Send("status " + Alias + " position") : null;
                return r != null && double.TryParse(r, out ms) ? ms / 1000 : 0;
            }
        }

        public double Duration
        {
            get
            {
                double ms;
                string r = open ? Send("status " + Alias + " length") : null;
                return r != null && double.TryParse(r, out ms) && ms > 0 ? ms / 1000 : -1;
            }
        }

        public void Close()
        {
            if (open)
            {
                Send("close " + Alias);
                open = false;
            }
        }

        public void Dispose() { Close(); }
    }

    // WPF 解码栈较重，仅在 MCI 不支持该格式时才懒加载
    class WpfBackend : IBackend
    {
        readonly System.Windows.Media.MediaPlayer p = new System.Windows.Media.MediaPlayer();

        public bool Open(string path) { p.Open(new Uri(path)); return true; }
        public void Play() { p.Play(); }
        public void Pause() { p.Pause(); }
        public void Stop() { p.Stop(); }
        public void SetVolume(double v) { p.Volume = v; }
        public bool Seek(double seconds) { p.Position = TimeSpan.FromSeconds(seconds); return true; }
        public double Position { get { return p.Position.TotalSeconds; } }
        public double Duration { get { return p.NaturalDuration.HasTimeSpan ? p.NaturalDuration.TimeSpan.TotalSeconds : -1; } }
        public void Dispose() { p.Close(); }
    }

    public class MainForm : Form
    {
        static readonly string[] ModeNames = { "顺序播放", "单曲循环", "随机播放" };
        static readonly string[] ModeGlyphs = { "\uE8EE", "\uE8ED", "\uE8B1" };
        static readonly HashSet<string> SupportedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".mp3", ".wav", ".wma", ".m4a", ".aac", ".flac"
        };

        IBackend backend;
        readonly List<string> files = new List<string>();
        readonly List<int> history = new List<int>();
        readonly Random random = new Random();
        readonly ToolTip tips = new ToolTip();

        string musicFolder = "";
        string nowTitle = "MuPlayer";
        string nowArtist = "点击列表中的歌曲开始播放";
        int current = -1;
        int mode;
        bool playIntent;
        bool opened;
        bool seeking;
        bool muted;
        bool updatingSelection;
        DateTime openStarted;
        int hoverRow = -1;
        double artHue = 210;
        LinearGradientBrush glowBrush;
        int glowW = -1;
        LinearGradientBrush artBrush;
        double artBrushHue = -1;
        string countText = "0 首";
        float headW = -1;
        int lastCurSec = -1, lastTotSec = -1;
        string curText = "00:00", totText = "00:00";
        DateTime idleSince;
        bool trimmed;
        DateTime nextGc = DateTime.Now.AddSeconds(60);

        IconButton btnMin, btnClose, btnRefresh, btnMode, btnPrev, btnPlay, btnNext, btnVol;
        Slider seekSlider, volSlider;
        SlimScrollBar listScroll;
        ListBox playlist;
        EqOverlay eq;
        Panel clipPanel;
        Timer timer, animTimer;

        const int TitleH = 44;
        const int BottomH = 112;

        [DllImport("shell32.dll")]
        static extern int SHGetKnownFolderPath(ref Guid rfid, uint dwFlags, IntPtr hToken, out IntPtr ppszPath);

        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        [DllImport("kernel32.dll")]
        static extern bool SetProcessWorkingSetSize(IntPtr h, IntPtr min, IntPtr max);

        public MainForm()
        {
            Text = "MuPlayer";
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Palette.Bg;
            ForeColor = Palette.Text;
            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(900, 640);
            MinimumSize = new Size(720, 520);
            StartPosition = FormStartPosition.CenterScreen;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);

            BuildUi();
            BuildIcon();
            ScanFolder();

            timer = new Timer();
            timer.Interval = 400;
            timer.Tick += TimerTick;
            timer.Start();

            animTimer = new Timer();
            animTimer.Interval = 120;
            animTimer.Tick += AnimTick;
            animTimer.Start();

            FormClosed += delegate
            {
                timer.Stop();
                animTimer.Stop();
                if (backend != null) backend.Dispose();
                if (glowBrush != null) glowBrush.Dispose();
                if (artBrush != null) artBrush.Dispose();
            };
            SyncTimers();
        }

        void SyncTimers()
        {
            timer.Enabled = current >= 0;
            animTimer.Enabled = current >= 0 && playIntent;
        }

        void TrimWorkingSet()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, (IntPtr)(-1), (IntPtr)(-1));
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized) TrimWorkingSet();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            using (Graphics g = CreateGraphics())
            {
                Ui.S = g.DpiX / 96f;
            }
            ClientSize = new Size(Ui.Sc(900), Ui.Sc(640));
            MinimumSize = new Size(Ui.Sc(720), Ui.Sc(520));
            playlist.ItemHeight = Ui.Sc(46);
            int pref = 2;
            DwmSetWindowAttribute(Handle, 33, ref pref, 4);
        }

        int resizeDir;
        Point resizeStart;
        Rectangle resizeBounds;

        int EdgeDir(Point p)
        {
            int b = Ui.Sc(8);
            bool l = p.X < b, r = p.X >= ClientSize.Width - b, t = p.Y < b, bo = p.Y >= ClientSize.Height - b;
            if (t && l) return 13;
            if (t && r) return 14;
            if (bo && l) return 16;
            if (bo && r) return 17;
            if (l) return 10;
            if (r) return 11;
            if (t) return 12;
            if (bo) return 15;
            return 0;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0084)
            {
                int lp = m.LParam.ToInt32();
                Point p = PointToClient(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                int dir = EdgeDir(p);
                if (dir != 0)
                {
                    Cursor = dir == 10 || dir == 11 ? Cursors.SizeWE
                        : dir == 12 || dir == 15 ? Cursors.SizeNS
                        : dir == 13 || dir == 17 ? Cursors.SizeNWSE : Cursors.SizeNESW;
                    m.Result = (IntPtr)1;
                    return;
                }
                Cursor = Cursors.Default;
                m.Result = (IntPtr)(p.Y < Ui.Sc(TitleH) && !btnMin.Bounds.Contains(p) && !btnClose.Bounds.Contains(p) ? 2 : 1);
                return;
            }
            if (m.Msg == 0x0201)
            {
                int lp = m.LParam.ToInt32();
                Point p = new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF));
                int dir = EdgeDir(p);
                if (dir != 0)
                {
                    resizeDir = dir;
                    resizeStart = PointToScreen(p);
                    resizeBounds = Bounds;
                    Capture = true;
                    return;
                }
            }
            if (m.Msg == 0x0200 && resizeDir != 0)
            {
                int lp = m.LParam.ToInt32();
                Point cur = PointToScreen(new Point((short)(lp & 0xFFFF), (short)((lp >> 16) & 0xFFFF)));
                int dx = cur.X - resizeStart.X, dy = cur.Y - resizeStart.Y;
                Rectangle b = resizeBounds;
                if (resizeDir == 10 || resizeDir == 13 || resizeDir == 16) { b.X += dx; b.Width -= dx; }
                if (resizeDir == 11 || resizeDir == 14 || resizeDir == 17) { b.Width += dx; }
                if (resizeDir == 12 || resizeDir == 13 || resizeDir == 14) { b.Y += dy; b.Height -= dy; }
                if (resizeDir == 15 || resizeDir == 16 || resizeDir == 17) { b.Height += dy; }
                if (b.Width < MinimumSize.Width) b.Width = MinimumSize.Width;
                if (b.Height < MinimumSize.Height) b.Height = MinimumSize.Height;
                Bounds = b;
                return;
            }
            if (m.Msg == 0x0202 && resizeDir != 0)
            {
                resizeDir = 0;
                Capture = false;
                return;
            }
            base.WndProc(ref m);
        }

        void BuildUi()
        {
            btnMin = new IconButton();
            btnMin.Glyph = "\uE921";
            btnMin.GlyphSize = 10;
            btnMin.GlyphColor = Palette.Muted;
            btnMin.Click += delegate { WindowState = FormWindowState.Minimized; };
            tips.SetToolTip(btnMin, "最小化");

            btnClose = new IconButton();
            btnClose.Glyph = "\uE8BB";
            btnClose.GlyphSize = 10;
            btnClose.GlyphColor = Palette.Muted;
            btnClose.CloseStyle = true;
            btnClose.Click += delegate { Close(); };
            tips.SetToolTip(btnClose, "关闭");

            btnRefresh = new IconButton();
            btnRefresh.Glyph = "\uE72C";
            btnRefresh.GlyphSize = 12;
            btnRefresh.GlyphColor = Palette.Muted;
            btnRefresh.Click += delegate { RefreshLibrary(); };
            tips.SetToolTip(btnRefresh, "刷新列表");

            clipPanel = new Panel();
            clipPanel.BackColor = Palette.Bg;

            playlist = new DarkListBox();
            playlist.BackColor = Palette.Bg;
            playlist.ForeColor = Palette.Text;
            playlist.BorderStyle = BorderStyle.None;
            playlist.DrawMode = DrawMode.OwnerDrawFixed;
            playlist.ItemHeight = 46;
            playlist.Font = Fonts.Row;
            playlist.IntegralHeight = false;
            playlist.DrawItem += PlaylistDrawItem;
            playlist.SelectedIndexChanged += PlaylistSelected;
            playlist.MouseMove += PlaylistMouseMove;
            playlist.MouseLeave += delegate { SetHoverRow(-1); };
            clipPanel.Controls.Add(playlist);

            eq = new EqOverlay();
            clipPanel.Controls.Add(eq);
            eq.BringToFront();

            listScroll = new SlimScrollBar();
            listScroll.UserScroll += delegate { playlist.TopIndex = listScroll.Value; UpdateEq(); };

            seekSlider = new Slider();
            seekSlider.UserScroll += SeekScroll;
            seekSlider.UserScrollEnd += delegate { seeking = false; };

            btnMode = new IconButton();
            btnMode.Glyph = ModeGlyphs[0];
            btnMode.GlyphSize = 14;
            btnMode.GlyphColor = Palette.Accent;
            btnMode.BackColor = Palette.Bar;
            btnMode.Click += delegate
            {
                mode = (mode + 1) % ModeNames.Length;
                btnMode.Glyph = ModeGlyphs[mode];
                tips.SetToolTip(btnMode, ModeNames[mode]);
                btnMode.Invalidate();
            };
            tips.SetToolTip(btnMode, ModeNames[0]);

            btnPrev = new IconButton();
            btnPrev.Glyph = "\uE76B";
            btnPrev.GlyphSize = 16;
            btnPrev.BackColor = Palette.Bar;
            btnPrev.Click += delegate { PrevTrack(); };
            tips.SetToolTip(btnPrev, "上一首");

            btnPlay = new IconButton();
            btnPlay.Glyph = "\uE768";
            btnPlay.GlyphSize = 18;
            btnPlay.Circle = true;
            btnPlay.AccentFill = true;
            btnPlay.BackColor = Palette.Bar;
            btnPlay.Click += delegate { TogglePlay(); };
            tips.SetToolTip(btnPlay, "播放 / 暂停");

            btnNext = new IconButton();
            btnNext.Glyph = "\uE76C";
            btnNext.GlyphSize = 16;
            btnNext.BackColor = Palette.Bar;
            btnNext.Click += delegate { NextTrack(); };
            tips.SetToolTip(btnNext, "下一首");

            btnVol = new IconButton();
            btnVol.Glyph = "\uE767";
            btnVol.GlyphSize = 13;
            btnVol.GlyphColor = Palette.Muted;
            btnVol.BackColor = Palette.Bar;
            btnVol.Click += delegate { ToggleMute(); };
            tips.SetToolTip(btnVol, "静音");

            volSlider = new Slider();
            volSlider.Value = 80;
            volSlider.UserScroll += delegate { ApplyVolume(); };

            Controls.AddRange(new Control[]
            {
                btnMin, btnClose, btnRefresh, clipPanel, listScroll, seekSlider,
                btnMode, btnPrev, btnPlay, btnNext, btnVol, volSlider
            });
            LayoutUi();
        }

        void LayoutUi()
        {
            int w = (int)Math.Round(ClientSize.Width / Ui.S);
            int h = (int)Math.Round(ClientSize.Height / Ui.S);
            btnMin.SetBounds(Ui.Sc(w - 88), Ui.Sc(6), Ui.Sc(40), Ui.Sc(32));
            btnClose.SetBounds(Ui.Sc(w - 48), Ui.Sc(6), Ui.Sc(40), Ui.Sc(32));
            btnRefresh.SetBounds(Ui.Sc(w - 56), Ui.Sc(176), Ui.Sc(32), Ui.Sc(32));

            int listTop = 214;
            int listH = Math.Max(60, h - BottomH - listTop - 8);
            clipPanel.SetBounds(Ui.Sc(16), Ui.Sc(listTop), Ui.Sc(w - 32), Ui.Sc(listH));
            playlist.SetBounds(0, 0, clipPanel.Width + Ui.Sc(22), clipPanel.Height);
            listScroll.SetBounds(Ui.Sc(w - 13), Ui.Sc(listTop + 2), Ui.Sc(6), Ui.Sc(listH - 4));

            int y0 = h - BottomH;
            seekSlider.SetBounds(Ui.Sc(76), Ui.Sc(y0 + 16), Ui.Sc(w - 152), Ui.Sc(16));

            int cy = y0 + 66;
            int totalW = 36 + 40 + 52 + 40 + 3 * 18;
            int x = (w - totalW) / 2;
            btnMode.SetBounds(Ui.Sc(x), Ui.Sc(cy - 18), Ui.Sc(36), Ui.Sc(36)); x += 36 + 18;
            btnPrev.SetBounds(Ui.Sc(x), Ui.Sc(cy - 20), Ui.Sc(40), Ui.Sc(40)); x += 40 + 18;
            btnPlay.SetBounds(Ui.Sc(x), Ui.Sc(cy - 26), Ui.Sc(52), Ui.Sc(52)); x += 52 + 18;
            btnNext.SetBounds(Ui.Sc(x), Ui.Sc(cy - 20), Ui.Sc(40), Ui.Sc(40));

            btnVol.SetBounds(Ui.Sc(w - 160), Ui.Sc(cy - 16), Ui.Sc(32), Ui.Sc(32));
            volSlider.SetBounds(Ui.Sc(w - 124), Ui.Sc(cy - 8), Ui.Sc(100), Ui.Sc(16));
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (btnMin != null) LayoutUi();
        }

        // GDI+ 的 Trimming+NoWrap 组合对部分字符串会错误地整串画成省略号，改为手动测量截断
        static void DrawText(Graphics g, string text, Font font, Color color, Rectangle rect, StringAlignment align)
        {
            string s = text;
            if (g.MeasureString(s, font).Width > rect.Width)
            {
                while (s.Length > 1 && g.MeasureString(s + "…", font).Width > rect.Width)
                {
                    s = s.Substring(0, s.Length - 1);
                }
                s += "…";
            }
            float w = g.MeasureString(s, font).Width;
            float x = rect.X;
            if (align == StringAlignment.Far) x = rect.Right - w;
            else if (align == StringAlignment.Center) x = rect.X + (rect.Width - w) / 2;
            float y = rect.Y + (rect.Height - font.GetHeight(g)) / 2;
            g.DrawString(s, font, Gfx.Brush(color), x, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            int w = (int)Math.Round(ClientSize.Width / Ui.S);
            int h = (int)Math.Round(ClientSize.Height / Ui.S);

            if (glowBrush == null || glowW != w)
            {
                if (glowBrush != null) glowBrush.Dispose();
                glowBrush = new LinearGradientBrush(new Rectangle(0, 0, Ui.Sc(w), Ui.Sc(240)),
                    Color.FromArgb(26, Palette.Accent), Color.FromArgb(0, Palette.Accent), 90f);
                glowW = w;
            }
            g.FillRectangle(glowBrush, 0, 0, Ui.Sc(w), Ui.Sc(240));

            g.FillRectangle(Gfx.Brush(Palette.Bar), 0, Ui.Sc(h - BottomH), Ui.Sc(w), Ui.Sc(BottomH));
            g.DrawLine(Gfx.Hairline, 0, Ui.Sc(h - BottomH), Ui.Sc(w), Ui.Sc(h - BottomH));

            Rectangle art = new Rectangle(Ui.Sc(24), Ui.Sc(62), Ui.Sc(96), Ui.Sc(96));
            if (artBrush == null || artBrushHue != artHue)
            {
                if (artBrush != null) artBrush.Dispose();
                artBrush = new LinearGradientBrush(art, Palette.Hsl(artHue, 0.55, 0.34), Palette.Hsl(artHue + 50, 0.6, 0.2), 45f);
                artBrushHue = artHue;
            }
            using (GraphicsPath path = Palette.Round(art, Ui.Sc(12)))
            {
                g.FillPath(artBrush, path);
                g.SetClip(path);
                g.DrawString("\u266A", Fonts.Art, Gfx.Brush(Color.FromArgb(150, 255, 255, 255)), art, Gfx.Center);
                g.ResetClip();
            }

            DrawText(g, nowTitle, Fonts.Title, Palette.Text, new Rectangle(Ui.Sc(140), Ui.Sc(76), Ui.Sc(w - 164), Ui.Sc(32)), StringAlignment.Near);
            DrawText(g, nowArtist, Fonts.Artist, Palette.Muted, new Rectangle(Ui.Sc(140), Ui.Sc(112), Ui.Sc(w - 164), Ui.Sc(20)), StringAlignment.Near);

            Rectangle logo = new Rectangle(Ui.Sc(16), Ui.Sc(11), Ui.Sc(22), Ui.Sc(22));
            using (GraphicsPath path = Palette.Round(logo, Ui.Sc(6)))
            {
                g.FillPath(Gfx.Brush(Palette.Accent), path);
            }
            g.DrawString("\u266A", Fonts.Logo, Gfx.Brush(Color.FromArgb(18, 18, 22)), logo, Gfx.Center);
            g.DrawString("MuPlayer", Fonts.App, Gfx.Brush(Palette.Text), Ui.Sc(46), Ui.Sc(13));

            if (headW < 0) headW = g.MeasureString("播放列表", Fonts.Head).Width / Ui.S;
            g.DrawString("播放列表", Fonts.Head, Gfx.Brush(Palette.Text), Ui.Sc(24), Ui.Sc(180));
            g.DrawString(countText, Fonts.Small, Gfx.Brush(Palette.Muted), Ui.Sc(24 + (int)headW + 12), Ui.Sc(184));
            DrawText(g, musicFolder, Fonts.Tiny, Palette.Dim, new Rectangle(Ui.Sc(w - 320), Ui.Sc(184), Ui.Sc(256), Ui.Sc(18)), StringAlignment.Far);

            int cs = backend != null ? (int)backend.Position : 0;
            if (cs != lastCurSec)
            {
                lastCurSec = cs;
                curText = FormatTime(TimeSpan.FromSeconds(cs));
            }
            double dur = backend != null ? backend.Duration : -1;
            int ts = dur > 0 ? (int)dur : 0;
            if (ts != lastTotSec)
            {
                lastTotSec = ts;
                totText = dur > 0 ? FormatTime(TimeSpan.FromSeconds(ts)) : "00:00";
            }
            int y0 = h - BottomH;
            DrawText(g, curText, Fonts.Tiny, Palette.Muted, new Rectangle(Ui.Sc(24), Ui.Sc(y0 + 16), Ui.Sc(48), Ui.Sc(16)), StringAlignment.Near);
            DrawText(g, totText, Fonts.Tiny, Palette.Muted, new Rectangle(Ui.Sc(w - 72), Ui.Sc(y0 + 16), Ui.Sc(48), Ui.Sc(16)), StringAlignment.Far);
        }

        void BuildIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    using (GraphicsPath path = Palette.Round(new Rectangle(0, 0, 31, 31), 7))
                    using (SolidBrush b = new SolidBrush(Palette.Accent)) g.FillPath(b, path);
                    g.DrawString("\u266A", Fonts.Logo, Gfx.Brush(Color.FromArgb(18, 18, 22)), new RectangleF(0, 0, 32, 32), Gfx.Center);
                }
                Icon = Icon.FromHandle(bmp.GetHicon());
            }
        }

        static string GetMusicFolder()
        {
            Guid folderIdMusic = new Guid("4BD8D571-6D19-48D3-BE97-422220080E43");
            IntPtr ptr;
            if (SHGetKnownFolderPath(ref folderIdMusic, 0, IntPtr.Zero, out ptr) == 0 && ptr != IntPtr.Zero)
            {
                string path = Marshal.PtrToStringUni(ptr);
                Marshal.FreeCoTaskMem(ptr);
                if (Directory.Exists(path)) return path;
            }
            return Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
        }

        void ScanFolder()
        {
            files.Clear();
            musicFolder = GetMusicFolder();
            CollectFiles(musicFolder);
            files.Sort(StringComparer.OrdinalIgnoreCase);

            updatingSelection = true;
            playlist.Items.Clear();
            foreach (string file in files) playlist.Items.Add(RelativePath(file));
            updatingSelection = false;
            countText = files.Count + " 首";
            Invalidate();
        }

        void CollectFiles(string dir)
        {
            try
            {
                foreach (string file in Directory.GetFiles(dir))
                {
                    if (SupportedExtensions.Contains(Path.GetExtension(file))) files.Add(file);
                }
                foreach (string sub in Directory.GetDirectories(dir))
                {
                    if ((File.GetAttributes(sub) & FileAttributes.ReparsePoint) != 0) continue;
                    CollectFiles(sub);
                }
            }
            catch { }
        }

        string RelativePath(string file)
        {
            string prefix = musicFolder.TrimEnd('\\') + "\\";
            if (file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return file.Substring(prefix.Length);
            return Path.GetFileName(file);
        }

        static void SplitName(string file, out string title, out string artist)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            int i = name.LastIndexOf('-');
            if (i > 0)
            {
                title = name.Substring(0, i).Trim();
                artist = name.Substring(i + 1).Trim();
            }
            else
            {
                title = name;
                artist = "本地音乐";
            }
        }

        void RefreshLibrary()
        {
            string playing = current >= 0 && current < files.Count ? files[current] : null;
            ScanFolder();
            if (playing != null)
            {
                int idx = files.IndexOf(playing);
                if (idx >= 0)
                {
                    current = idx;
                    updatingSelection = true;
                    playlist.SelectedIndex = idx;
                    updatingSelection = false;
                }
                else
                {
                    StopPlayback();
                }
            }
            playlist.Invalidate();
        }

        void SetHoverRow(int index)
        {
            if (hoverRow == index) return;
            int old = hoverRow;
            hoverRow = index;
            if (old >= 0) playlist.Invalidate(playlist.GetItemRectangle(old));
            if (index >= 0) playlist.Invalidate(playlist.GetItemRectangle(index));
        }

        void PlaylistMouseMove(object sender, MouseEventArgs e)
        {
            SetHoverRow(playlist.IndexFromPoint(e.Location));
        }

        void PlaylistSelected(object sender, EventArgs e)
        {
            if (updatingSelection) return;
            if (playlist.SelectedIndex >= 0) PlayIndex(playlist.SelectedIndex);
        }

        void PlaylistDrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            Rectangle bounds = e.Bounds;
            int bx = (int)Math.Round(bounds.X / Ui.S);
            int by = (int)Math.Round(bounds.Y / Ui.S);
            int bw = (int)Math.Round(bounds.Width / Ui.S);
            bool isCurrent = e.Index == current;
            g.FillRectangle(Gfx.Brush(Palette.Bg), bounds);
            Color back = isCurrent ? Palette.RowCurrent : (e.Index == hoverRow ? Palette.RowHover : Color.Empty);
            if (back != Color.Empty)
            {
                Rectangle r = new Rectangle(bounds.X + Ui.Sc(4), bounds.Y + Ui.Sc(2), bounds.Width - Ui.Sc(12), bounds.Height - Ui.Sc(4));
                using (GraphicsPath path = Palette.Round(r, Ui.Sc(8)))
                {
                    g.FillPath(Gfx.Brush(back), path);
                }
            }

            Rectangle glyphZone = new Rectangle(Ui.Sc(bx + 14), bounds.Y, Ui.Sc(30), bounds.Height);
            if (!isCurrent)
            {
                DrawText(g, (e.Index + 1).ToString("00"), Fonts.Index, Palette.Dim, glyphZone, StringAlignment.Far);
            }

            string title, artist;
            SplitName(files[e.Index], out title, out artist);
            Rectangle titleRect = new Rectangle(Ui.Sc(bx + 52), Ui.Sc(by + 6), Ui.Sc(bw - 70), Ui.Sc(20));
            Rectangle artistRect = new Rectangle(Ui.Sc(bx + 52), Ui.Sc(by + 25), Ui.Sc(bw - 70), Ui.Sc(16));
            DrawText(g, title, Fonts.Row, isCurrent ? Palette.Accent : Palette.Text, titleRect, StringAlignment.Near);
            DrawText(g, artist, Fonts.RowArtist, Palette.Muted, artistRect, StringAlignment.Near);
        }

        void PlayIndex(int index)
        {
            if (index < 0 || index >= files.Count) return;
            if (current >= 0 && current != index)
            {
                history.Add(current);
                if (history.Count > 200) history.RemoveAt(0);
            }
            current = index;
            opened = false;
            playIntent = true;
            openStarted = DateTime.Now;
            nextGc = DateTime.Now.AddSeconds(10);
            seekSlider.Maximum = 100;
            seekSlider.Value = 0;

            IBackend next = new MciBackend();
            if (!next.Open(files[index]))
            {
                next.Dispose();
                next = new WpfBackend();
                next.Open(files[index]);
            }
            if (backend != null) backend.Dispose();
            backend = next;
            ApplyVolume();
            backend.Play();

            SplitName(files[index], out nowTitle, out nowArtist);
            artHue = StableHue(files[index]);

            updatingSelection = true;
            playlist.SelectedIndex = index;
            updatingSelection = false;

            int visible = clipPanel.Height / playlist.ItemHeight;
            if (index < playlist.TopIndex || index >= playlist.TopIndex + visible)
            {
                playlist.TopIndex = Math.Max(0, index - visible / 2);
            }
            playlist.Invalidate();
            UpdatePlayButton();
            UpdateEq();
            SyncTimers();
            Invalidate();
        }

        static double StableHue(string s)
        {
            int h = 17;
            foreach (char c in s) h = h * 31 + c;
            return Math.Abs(h % 360);
        }

        void StopPlayback()
        {
            if (backend != null) backend.Stop();
            current = -1;
            playIntent = false;
            opened = false;
            nowTitle = "MuPlayer";
            nowArtist = "点击列表中的歌曲开始播放";
            seekSlider.Value = 0;
            UpdatePlayButton();
            playlist.Invalidate();
            UpdateEq();
            SyncTimers();
            Invalidate();
        }

        void TogglePlay()
        {
            if (files.Count == 0) return;
            if (current < 0)
            {
                PlayIndex(playlist.SelectedIndex >= 0 ? playlist.SelectedIndex : 0);
                return;
            }
            if (playIntent)
            {
                backend.Pause();
                playIntent = false;
            }
            else
            {
                backend.Play();
                playIntent = true;
            }
            UpdatePlayButton();
            playlist.Invalidate();
            UpdateEq();
            SyncTimers();
        }

        void PrevTrack()
        {
            if (files.Count == 0) return;
            if (current < 0) { PlayIndex(0); return; }
            if (mode == 2 && history.Count > 0)
            {
                int prev = history[history.Count - 1];
                history.RemoveAt(history.Count - 1);
                PlayIndex(prev);
            }
            else
            {
                PlayIndex((current - 1 + files.Count) % files.Count);
            }
        }

        void NextTrack()
        {
            if (files.Count == 0) return;
            if (current < 0) PlayIndex(0);
            else if (mode == 2) PlayIndex(RandomOther());
            else PlayIndex((current + 1) % files.Count);
        }

        int RandomOther()
        {
            if (files.Count == 1) return 0;
            for (int i = 0; i < 50; i++)
            {
                int idx = random.Next(files.Count);
                if (idx != current) return idx;
            }
            return (current + 1) % files.Count;
        }

        void HandleTrackEnded()
        {
            if (mode == 1)
            {
                backend.Seek(0);
                backend.Play();
            }
            else if (mode == 2)
            {
                PlayIndex(RandomOther());
            }
            else
            {
                PlayIndex((current + 1) % files.Count);
            }
        }

        void UpdatePlayButton()
        {
            btnPlay.Glyph = playIntent ? "\uE769" : "\uE768";
            btnPlay.Invalidate();
        }

        void ToggleMute()
        {
            muted = !muted;
            btnVol.Glyph = muted ? "\uE74F" : "\uE767";
            btnVol.GlyphColor = muted ? Palette.Accent : Palette.Muted;
            btnVol.Invalidate();
            ApplyVolume();
        }

        void ApplyVolume()
        {
            if (backend != null) backend.SetVolume(muted ? 0 : volSlider.Value / 100.0);
        }

        void SeekScroll(object sender, EventArgs e)
        {
            seeking = true;
            if (opened && backend != null && backend.Duration > 0)
            {
                bool wasPlaying = playIntent;
                backend.Seek(seekSlider.Value);
                if (!wasPlaying) backend.Pause();
                InvalidateTimeRow();
            }
        }

        // WPF MediaPlayer 的事件依赖 Dispatcher 消息循环，在 WinForms 线程上不会触发，改用轮询
        void TimerTick(object sender, EventArgs e)
        {
            if (current < 0) return;
            if (!playIntent)
            {
                if (idleSince == DateTime.MinValue) idleSince = DateTime.Now;
                else if (!trimmed && (DateTime.Now - idleSince).TotalSeconds > 20)
                {
                    TrimWorkingSet();
                    trimmed = true;
                }
            }
            else
            {
                idleSince = DateTime.MinValue;
                trimmed = false;
            }
            if (!opened)
            {
                double d0 = backend.Duration;
                if (d0 > 0)
                {
                    opened = true;
                    seekSlider.Maximum = Math.Max(1, (int)Math.Ceiling(d0));
                }
                else if ((DateTime.Now - openStarted).TotalSeconds > 8)
                {
                    opened = true;
                    playIntent = false;
                    nowTitle = "无法播放";
                    nowArtist = Path.GetFileName(files[current]);
                    UpdatePlayButton();
                    SyncTimers();
                    Invalidate();
                }
                return;
            }
            double pos = backend.Position;
            double dur = backend.Duration;
            if (!seeking)
            {
                seekSlider.Value = (int)pos;
            }
            if (playIntent && dur > 0 && pos >= dur - 0.3)
            {
                HandleTrackEnded();
                return;
            }
            if ((int)pos != lastCurSec) InvalidateTimeRow();
            if (DateTime.Now > nextGc)
            {
                nextGc = DateTime.Now.AddSeconds(60);
                TrimWorkingSet();
            }
        }

        void InvalidateTimeRow()
        {
            int y0 = ClientSize.Height - Ui.Sc(BottomH);
            Invalidate(new Rectangle(0, y0 + Ui.Sc(12), ClientSize.Width, Ui.Sc(24)));
        }

        void AnimTick(object sender, EventArgs e)
        {
            UpdateEq();
            int visible = Math.Max(1, clipPanel.Height / playlist.ItemHeight);
            int maxTop = Math.Max(0, files.Count - visible);
            listScroll.PageSize = visible;
            listScroll.Maximum = maxTop;
            listScroll.Value = Math.Min(playlist.TopIndex, maxTop);
            listScroll.Visible = files.Count > visible;
        }

        void UpdateEq()
        {
            if (current < 0)
            {
                eq.Visible = false;
                return;
            }
            Rectangle row = playlist.GetItemRectangle(current);
            if (row.Y < 0 || row.Bottom > clipPanel.Height)
            {
                eq.Visible = false;
                return;
            }
            eq.SetBounds(row.X + Ui.Sc(16), row.Y + Ui.Sc(4), Ui.Sc(36), row.Height - Ui.Sc(8));
            bool was = eq.Playing;
            eq.Playing = playIntent;
            eq.Visible = true;
            if (playIntent || was != playIntent) eq.Invalidate();
        }

        static string FormatTime(TimeSpan t)
        {
            if (t < TimeSpan.Zero) t = TimeSpan.Zero;
            return t.ToString(t.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss");
        }
    }
}