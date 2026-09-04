using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Threading;
using System.Windows.Forms;

namespace SJBuddy
{
    // The pet itself: a per-pixel-alpha window that never steals focus, plus all behaviour logic.
    public class PetForm : Form
    {
        const int W = 420, H = 440, GroundY = 420;      // logical canvas
        public Settings S; public Brain B; public Character C;
        NotifyIcon tray; ContextMenuStrip menu; IntPtr trayIconHandle = IntPtr.Zero;
        System.Windows.Forms.Timer frame;
        Bitmap canvas; Graphics cg; float scale = 1f; float dpiScale = 1f;
        readonly Random rnd = new Random();
        readonly Stopwatch clock = Stopwatch.StartNew();

        // ---- input tracking: no keyboard hook. Each frame we ask Windows whether typing keys are currently pressed
        //      (letters, digits, space, enter, backspace...) and count press edges. Nothing is stored or logged. ----
        static readonly int[] TypingKeys = BuildTypingKeys();
        readonly bool[] keyWasDown = new bool[256];
        readonly Queue<DateTime> recentKeys = new Queue<DateTime>();
        bool typingActive;
        DateTime lastMouseMoveAt = DateTime.MinValue, lastPollAt = DateTime.MinValue; float mouseSpeed; bool hovering;
        DateTime lastKeyAt = DateTime.MinValue;
        readonly int[] keysPerSec = new int[60]; int secIdx; DateTime lastSecStamp = DateTime.MinValue; int keysThisSec;
        DateTime typingSessionStart = DateTime.MinValue, lastTypingAt = DateTime.MinValue; bool typingSessionOpen; int continuousMinutes;
        int idleSec; bool wasIdleLong; bool lunchObservedToday; string lunchDay = "";
        DateTime lastMinuteTick = DateTime.MinValue, lastLogicTick = DateTime.MinValue;
        Point lastCursor;

        // ---- state ----
        enum Mode { Idle, Typing, Sleep, Held }
        Mode mode = Mode.Idle;
        Act act; float actStart; int walkDir = 1;
        float nextBlinkAt, blinkUntil, excitedUntil = -1, waterStartT = -1;
        DateTime breakSuggestedAt = DateTime.MinValue;

        // ---- bubble ----
        string bubbleText; float bubbleUntil, bubbleShownAt; string bubbleCat; bool bubbleWater, bubbleSpontaneous; DateTime bubbleShownClock;
        string queuedCat; float queuedAt;

        // ---- water ----
        DateTime lastSipAt; bool waterPending; DateTime waterShownAt; int nagCount; bool waterDeferred; DateTime returnedAt = DateTime.MinValue;

        // ---- mouse ----
        bool mouseDown, dragging; Point downScreen, downWinPos; DateTime downAt, heldSince, lastDragAt; Point lastDragPt; float dragSpeed; bool shakeSaid, heldLongSaid;

        readonly Dictionary<string, DateTime> cool = new Dictionary<string, DateTime>();
        bool petHidden, exiting, dialogOpen;
        float pendingGreetAt = -1;
        int lastLevel;
        IntPtr prevForeground = IntPtr.Zero;

        public PetForm()
        {
            S = new Settings(); B = new Brain();
            C = Characters.Get(S.Character);
            if (S.PetName == "") S.PetName = C.Name;
            lastLevel = B.Level;
            lastSipAt = DateTime.Now;

            FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
            TopMost = S.AlwaysOnTop; Text = "SJ Buddy";
            SetStyle(ControlStyles.Opaque | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint, true);
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            SetupTray();
            frame = new System.Windows.Forms.Timer(); frame.Interval = 40; frame.Tick += OnFrame;
            Load += OnLoad; FormClosing += OnClosing;
            MouseDown += OnMouseDown; MouseMove += OnMouseMove; MouseUp += OnMouseUp; MouseDoubleClick += OnMouseDoubleClick;
        }

        protected override bool ShowWithoutActivation { get { return true; } }
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= Native.WS_EX_LAYERED | Native.WS_EX_TOOLWINDOW | Native.WS_EX_NOACTIVATE;
                if (S == null || S.AlwaysOnTop) cp.ExStyle |= Native.WS_EX_TOPMOST;
                return cp;
            }
        }
        protected override void OnPaintBackground(PaintEventArgs e) { }
        protected override void OnPaint(PaintEventArgs e) { }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Native.WM_MOUSEACTIVATE) { m.Result = (IntPtr)Native.MA_NOACTIVATE; return; }
            if (m.Msg == Native.WM_HOTKEY) { TogglePet(); return; }
            base.WndProc(ref m);
        }

        // =====================================================================================
        // setup
        // =====================================================================================
        void OnLoad(object sender, EventArgs e)
        {
            using (var g = CreateGraphics()) dpiScale = g.DpiX / 96f;
            ApplySize(false);
            if (S.PosX == int.MinValue) DefaultPosition(); else { Left = S.PosX; Top = S.PosY; }
            ClampToScreen();
            lastCursor = Cursor.Position;
            if (S.Hotkey) Native.RegisterHotKey(Handle, 1, Native.MOD_CONTROL | Native.MOD_ALT, Native.VK_B);
            nextBlinkAt = 2;
            frame.Start();
            UpdateTray();
            var th = new Thread(WatchShowEvent); th.IsBackground = true; th.Start();
            if (!S.FirstRunDone) { BeginInvoke(new Action(() => ShowSetup(true))); }
            else
            {
                pendingGreetAt = (float)clock.Elapsed.TotalSeconds + (Program.AutoStarted ? 12 : 2.5f);
                if (S.StartHidden) HidePet(true);
                if (Program.StartCommand == "settings") BeginInvoke(new Action(() => ShowSetup(false)));
                else if (Program.StartCommand == "stats") BeginInvoke(new Action(() => ShowStats()));
            }
        }

        void WatchShowEvent()
        {
            var handles = new WaitHandle[] { Program.ShowEvent, Program.SettingsEvent, Program.StatsEvent };
            foreach (var h in handles) if (h == null) return;
            while (!exiting)
            {
                try
                {
                    int i = WaitHandle.WaitAny(handles, 1000);
                    if (i == 0) BeginInvoke(new Action(() => { ShowPet(); StartAct("wave"); }));
                    else if (i == 1) BeginInvoke(new Action(() => { ShowPet(); ShowSetup(false); }));
                    else if (i == 2) BeginInvoke(new Action(() => { ShowPet(); ShowStats(); }));
                }
                catch { break; }
            }
        }

        void ApplySize(bool keepFeet)
        {
            float oldScale = scale; int oldLeft = Left, oldTop = Top;
            scale = S.SizeFactor * dpiScale;
            int pw = (int)(W * scale), ph = (int)(H * scale);
            if (cg != null) cg.Dispose(); if (canvas != null) canvas.Dispose();
            canvas = new Bitmap(pw, ph, PixelFormat.Format32bppArgb);
            cg = Graphics.FromImage(canvas);
            cg.SmoothingMode = SmoothingMode.AntiAlias; cg.TextRenderingHint = TextRenderingHint.AntiAlias; cg.PixelOffsetMode = PixelOffsetMode.HighQuality;
            Size = new Size(pw, ph);
            if (keepFeet)
            {
                float fx = oldLeft + W / 2f * oldScale, fy = oldTop + GroundY * oldScale;
                Left = (int)(fx - W / 2f * scale); Top = (int)(fy - GroundY * scale);
                ClampToScreen();
            }
        }

        void DefaultPosition()
        {
            var wa = Screen.PrimaryScreen.WorkingArea;
            Left = (int)(wa.Right - 90 * dpiScale - (W / 2f + 100) * scale);
            Top = (int)(wa.Bottom - GroundY * scale - 2);
        }

        void ClampToScreen()
        {
            var vs = SystemInformation.VirtualScreen;
            float x0 = Left + (W / 2f - 100) * scale, x1 = Left + (W / 2f + 100) * scale;
            float y0 = Top + (GroundY - 240) * scale, y1 = Top + GroundY * scale;
            int dx = 0, dy = 0;
            if (x0 < vs.Left) dx = (int)(vs.Left - x0); else if (x1 > vs.Right) dx = (int)(vs.Right - x1);
            if (y0 < vs.Top) dy = (int)(vs.Top - y0); else if (y1 > vs.Bottom) dy = (int)(vs.Bottom - y1);
            if (dx != 0 || dy != 0) Location = new Point(Left + dx, Top + dy);
        }

        // =====================================================================================
        // tray + menu
        // =====================================================================================
        void SetupTray()
        {
            tray = new NotifyIcon();
            tray.Visible = true;
            tray.Text = "SJ Buddy";
            tray.MouseClick += (s, e) => { if (e.Button == MouseButtons.Left) { ShowPet(); StartAct("wave"); } };
            tray.DoubleClick += (s, e) => TogglePet();
            tray.BalloonTipClicked += (s, e) => { ShowPet(); };
            menu = new ContextMenuStrip();
            menu.Opening += (s, e) => BuildMenu();
            menu.Closed += (s, e) => { if (prevForeground != IntPtr.Zero) { try { Native.SetForegroundWindow(prevForeground); } catch { } prevForeground = IntPtr.Zero; } };
            tray.ContextMenuStrip = menu;
        }

        void UpdateTray()
        {
            string txt = "SJ Buddy - " + S.PetName + " (" + C.Name + ") Lv." + B.Level;
            if (txt.Length > 63) txt = txt.Substring(0, 63);
            tray.Text = txt;
            try
            {
                using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias; g.Clear(Color.Transparent);
                    float s = 46f / -C.HeadTop;
                    var p = new Pose(); p.Eyes = Eyes.Open; p.Mouth = Mouth.Smile;
                    Preview.DrawCharacter(g, C, p, 0, s, 16, 2 - C.HeadTop * s, false);
                    var old = trayIconHandle;
                    trayIconHandle = bmp.GetHicon();
                    tray.Icon = Icon.FromHandle(trayIconHandle);
                    if (old != IntPtr.Zero) Native.DestroyIcon(old);
                }
            }
            catch { try { tray.Icon = Icon; } catch { } }
        }

        ToolStripMenuItem MI(string text, EventHandler h, bool check) { var it = new ToolStripMenuItem(text); if (h != null) it.Click += h; it.Checked = check; return it; }
        ToolStripMenuItem MI(string text, EventHandler h) { return MI(text, h, false); }

        void BuildMenu()
        {
            menu.Items.Clear();
            var head = MI(S.PetName + "  -  " + C.Name + "  (Level " + B.Level + ", " + B.Title(S.Language) + ")", null); head.Enabled = false; head.Font = new Font(menu.Font, FontStyle.Bold);
            menu.Items.Add(head);
            menu.Items.Add(new ToolStripSeparator());
            if (petHidden) menu.Items.Add(MI("Show " + S.PetName, (s, e) => ShowPet()));
            menu.Items.Add(MI("Talk to me", (s, e) => { ShowPet(); SayRandomTalk(); }));
            menu.Items.Add(MI("Pet me", (s, e) => { ShowPet(); DoPet(); }));
            menu.Items.Add(MI(waterPending ? "I drank water (done!)" : "I just drank water", (s, e) => { ShowPet(); Sip(); }));
            if (waterPending) menu.Items.Add(MI("Snooze water reminder 5 min", (s, e) => SnoozeWater()));
            var fun = MI("Do something fun", null);
            foreach (var id in new[] { "dance", "sing", "eat", "exercise", "jump", "spin", "laugh", "think", "stretch", "peek", "walk", "phone" }) { string a = id; fun.DropDownItems.Add(MI(Cap(a), (s, e) => { ShowPet(); StartAct(a, true); })); }
            fun.DropDownItems.Add(new ToolStripSeparator());
            foreach (var id in C.Specials) { string a = id; fun.DropDownItems.Add(MI(C.Name + ": " + Cap(a), (s, e) => { ShowPet(); StartAct(a, true); })); }
            menu.Items.Add(fun);
            menu.Items.Add(new ToolStripSeparator());
            var ch = MI("Change character", null);
            foreach (var id in Characters.Ids) { string a = id; ch.DropDownItems.Add(MI(Characters.DisplayName(a), (s, e) => SwitchCharacter(a), a == C.Id)); }
            menu.Items.Add(ch);
            menu.Items.Add(MI("Rename...", (s, e) => Rename()));
            var lang = MI("Language", null);
            foreach (var pair in new[] { "en|English", "hi|Hindi (हिंदी)", "hing|Hinglish", "mix|Mix (surprise me)" })
            { var parts = pair.Split('|'); string code = parts[0]; lang.DropDownItems.Add(MI(parts[1], (s, e) => { S.Language = code; S.Save(); Say("switch_in"); }, S.Language == code)); }
            menu.Items.Add(lang);
            var water = MI("Water reminder", null);
            foreach (int m in new[] { 0, 15, 20, 30, 45, 60, 90 })
            { int mm = m; water.DropDownItems.Add(MI(m == 0 ? "Off" : "Every " + m + " minutes", (s, e) => { S.WaterMinutes = mm; S.Save(); lastSipAt = DateTime.Now; waterPending = false; Say(mm == 0 ? "hide" : "water_ack"); }, S.WaterMinutes == m)); }
            menu.Items.Add(water);
            var talk = MI("Talkativeness", null);
            foreach (var pair in new[] { "quiet|Quiet", "normal|Normal", "chatty|Chatty" })
            { var parts = pair.Split('|'); string code = parts[0]; talk.DropDownItems.Add(MI(parts[1], (s, e) => { S.Chattiness = code; S.Save(); }, S.Chattiness == code)); }
            menu.Items.Add(talk);
            var size = MI("Size", null);
            foreach (var pair in new[] { "small|Small", "medium|Medium", "large|Large", "xl|Extra large" })
            { var parts = pair.Split('|'); string code = parts[0]; size.DropDownItems.Add(MI(parts[1], (s, e) => { S.Size = code; S.Save(); ApplySize(true); }, S.Size == code)); }
            menu.Items.Add(size);
            menu.Items.Add(MI("Settings...", (s, e) => ShowSetup(false)));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MI("Start with Windows", (s, e) => { bool on = !AutoStart.IsEnabled(); AutoStart.Set(on); Say(on ? "switch_in" : "hide"); }, AutoStart.IsEnabled()));
            menu.Items.Add(MI("Always on top", (s, e) => { S.AlwaysOnTop = !S.AlwaysOnTop; S.Save(); TopMost = S.AlwaysOnTop; }, S.AlwaysOnTop));
            menu.Items.Add(MI("Sounds", (s, e) => { S.Sound = !S.Sound; S.Save(); if (S.Sound) Sounds.Pop(); }, S.Sound));
            menu.Items.Add(MI("Wander around", (s, e) => { S.Wander = !S.Wander; S.Save(); }, S.Wander));
            menu.Items.Add(MI("Break and eye-rest reminders", (s, e) => { S.BreakReminders = !S.BreakReminders; S.Save(); }, S.BreakReminders));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MI("Stats and what I learned...", (s, e) => ShowStats()));
            menu.Items.Add(MI("About SJ Buddy", (s, e) => ShowAbout()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(MI(petHidden ? "Show (Ctrl+Alt+B)" : "Hide (Ctrl+Alt+B)", (s, e) => TogglePet()));
            menu.Items.Add(MI("Exit", (s, e) => ExitApp()));
        }

        static string Cap(string s) { return s.Length == 0 ? s : char.ToUpperInvariant(s[0]) + s.Substring(1); }

        void ShowMenu()
        {
            prevForeground = Native.GetForegroundWindow();
            if (prevForeground == Handle) prevForeground = IntPtr.Zero;
            Native.SetForegroundWindow(Handle);
            menu.Show(Cursor.Position);
        }

        // =====================================================================================
        // show / hide / exit
        // =====================================================================================
        public void ShowPet()
        {
            if (!petHidden) return;
            petHidden = false; Show(); ClampToScreen();
        }
        void HidePet(bool silent)
        {
            if (petHidden) return;
            if (bubbleSpontaneous && (DateTime.Now - bubbleShownClock).TotalSeconds < 8) B.OnAnnoyed();
            petHidden = true; Hide();
            if (!silent) { try { tray.ShowBalloonTip(4000, "SJ Buddy", S.PetName + " is waiting in the tray. Double-click the icon or press Ctrl+Alt+B to bring " + S.PetName + " back.", ToolTipIcon.Info); } catch { } }
        }
        void TogglePet() { if (petHidden) ShowPet(); else HidePet(false); }
        void ExitApp() { exiting = true; Close(); }

        void OnClosing(object sender, FormClosingEventArgs e)
        {
            exiting = true;
            try { frame.Stop(); } catch { }
            try { Native.UnregisterHotKey(Handle, 1); } catch { }
            S.PosX = Left; S.PosY = Top; S.Save(); B.Save();
            try { tray.Visible = false; tray.Dispose(); } catch { }
            if (trayIconHandle != IntPtr.Zero) Native.DestroyIcon(trayIconHandle);
        }

        // =====================================================================================
        // dialogs
        // =====================================================================================
        void ShowSetup(bool firstRun)
        {
            if (dialogOpen) return; dialogOpen = true;
            string oldChar = S.Character, oldSize = S.Size;
            try
            {
                using (var d = new SetupDialog(S, firstRun))
                {
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        S.FirstRunDone = true; S.Save();
                        AutoStart.Set(d.AutoStartWanted);
                        TopMost = S.AlwaysOnTop;
                        if (S.Character != oldChar) SwitchCharacter(S.Character); else UpdateTray();
                        if (S.Size != oldSize) ApplySize(true);
                        if (firstRun) { ShowPet(); Say("welcome", 9); StartAct("wave"); pendingGreetAt = (float)clock.Elapsed.TotalSeconds + 14; }
                    }
                    else if (firstRun) { S.FirstRunDone = true; S.Save(); Say("welcome", 9); }
                }
            }
            finally { dialogOpen = false; }
        }

        void Rename()
        {
            if (dialogOpen) return; dialogOpen = true;
            try
            {
                string n = InputDialog.Ask("Rename " + S.PetName, "Give your buddy a new name:", S.PetName);
                if (!string.IsNullOrEmpty(n)) { S.PetName = n.Trim(); S.Save(); UpdateTray(); Say("rename", 6); StartAct("love"); }
            }
            finally { dialogOpen = false; }
        }

        void ShowStats()
        {
            if (dialogOpen) return; dialogOpen = true;
            try { using (var d = new StatsDialog(this)) d.ShowDialog(); }
            finally { dialogOpen = false; }
        }

        void ShowAbout()
        {
            if (dialogOpen) return; dialogOpen = true;
            try { using (var d = new AboutDialog()) d.ShowDialog(); }
            finally { dialogOpen = false; }
        }

        public void SwitchCharacter(string id)
        {
            string oldDefault = C.Name;
            C = Characters.Get(id); S.Character = id;
            if (S.PetName == "" || S.PetName == oldDefault) S.PetName = C.Name;
            S.Save(); UpdateTray(); act = null; waterStartT = -1;
            ShowPet(); Say("switch_in", 6); StartAct("wave");
        }

        public void ResetLearning() { B.Reset(); lastLevel = 1; UpdateTray(); }
        public void LogSipFromDialog() { Sip(); }

        // =====================================================================================
        // frame loop
        // =====================================================================================
        void OnFrame(object sender, EventArgs e)
        {
            if (exiting) return;
            float t = (float)clock.Elapsed.TotalSeconds; var now = DateTime.Now;
            try
            {
                PollInput(now);
                if ((now - lastLogicTick).TotalSeconds >= 1) { lastLogicTick = now; Logic(now, t); }
                if (act != null && act.MoveX != 0 && !dragging) MoveBy(act.MoveX * walkDir * scale * frame.Interval / 1000f);
                if (queuedCat != null && t >= queuedAt) { string q = queuedCat; queuedCat = null; Say(q, 6); }
                if (pendingGreetAt >= 0 && t >= pendingGreetAt) { pendingGreetAt = -1; Greet(now); }
                if (!petHidden)
                {
                    var p = BuildPose(t, now);
                    Render(p, t);
                }
                int wanted = (mode == Mode.Sleep && bubbleText == null && act == null) ? 90 : 40;
                if (frame.Interval != wanted) frame.Interval = wanted;
            }
            catch (Exception ex) { Paths.Log("frame: " + ex); }
        }

        static int[] BuildTypingKeys()
        {
            var l = new List<int> { 0x08, 0x09, 0x0D, 0x20, 0x2E };
            for (int v = 0x30; v <= 0x39; v++) l.Add(v);      // digits
            for (int v = 0x41; v <= 0x5A; v++) l.Add(v);      // letters
            for (int v = 0x60; v <= 0x69; v++) l.Add(v);      // numpad digits
            for (int v = 0xBA; v <= 0xC0; v++) l.Add(v);      // ; = , - . / `
            for (int v = 0xDB; v <= 0xDE; v++) l.Add(v);      // [ \ ] '
            return l.ToArray();
        }

        Rectangle CharacterBox()
        {
            return new Rectangle((int)(Left + (W / 2f - 95) * scale), (int)(Top + (GroundY - 235) * scale), (int)(190 * scale), (int)(235 * scale));
        }

        void PollInput(DateTime now)
        {
            idleSec = Native.IdleSeconds();
            var cur = Cursor.Position;
            double dt = lastPollAt == DateTime.MinValue ? 0.04 : Math.Max(0.01, (now - lastPollAt).TotalSeconds); lastPollAt = now;
            if (cur != lastCursor)
            {
                float d = (float)Math.Sqrt((cur.X - lastCursor.X) * (cur.X - lastCursor.X) + (cur.Y - lastCursor.Y) * (cur.Y - lastCursor.Y));
                mouseSpeed = mouseSpeed * 0.5f + (float)(d / dt) * 0.5f; lastMouseMoveAt = now;
            }
            else mouseSpeed *= 0.7f;
            lastCursor = cur;
            hovering = !petHidden && !dragging && CharacterBox().Contains(cur);

            int k = 0;
            for (int i = 0; i < TypingKeys.Length; i++)
            {
                int vk = TypingKeys[i];
                bool down = (Native.GetAsyncKeyState(vk) & 0x8000) != 0;
                if (down && !keyWasDown[vk]) k++;
                keyWasDown[vk] = down;
            }
            if (k > 0) { lastKeyAt = now; keysThisSec += k; B.OnKeystrokes(k, now); recentKeys.Enqueue(now); }
            while (recentKeys.Count > 0 && (now - recentKeys.Peek()).TotalSeconds > 2.5) recentKeys.Dequeue();
            // Typing mode needs a real burst (2+ keys in 2.5 s) and ends 2.5 s after the last key: one stray key does nothing.
            typingActive = recentKeys.Count >= 2 || (typingActive && (now - lastKeyAt).TotalSeconds < 2.5);
            if (lastSecStamp == DateTime.MinValue || (now - lastSecStamp).TotalSeconds >= 1)
            {
                keysPerSec[secIdx] = keysThisSec; secIdx = (secIdx + 1) % 60; keysThisSec = 0; lastSecStamp = now;
            }
        }

        int Kpm() { int s = 0; for (int i = 0; i < 60; i++) s += keysPerSec[i]; return s; }

        bool Cool(string key, double seconds)
        {
            DateTime last; var now = DateTime.Now;
            if (cool.TryGetValue(key, out last) && (now - last).TotalSeconds < seconds) return false;
            cool[key] = now; return true;
        }
        double Chat { get { return S.ChattinessFactor * B.Chattiness; } }
        bool Chance(double p) { return rnd.NextDouble() < p; }

        // =====================================================================================
        // once-per-second brain
        // =====================================================================================
        void Logic(DateTime now, float t)
        {
            bool typingNow = typingActive;
            bool typingRecent = (now - lastKeyAt).TotalSeconds < 45;

            // mouse-only reactions
            if (hovering && !waterPending && Cool("hover", 45) && Chance(0.6 * Chat)) Say("hover", 4);
            if (mouseSpeed > 2600 && !typingNow && Cool("mouse_fast", 180) && Chance(0.35 * Chat)) Say("mouse_fast", 4);

            // typing sessions
            if (typingNow)
            {
                if (!typingSessionOpen || (now - lastTypingAt).TotalSeconds > 180)
                {
                    typingSessionOpen = true; typingSessionStart = now;
                    if (Cool("typing_start", 600) && Chance(0.6 * Chat)) Say("typing_start");
                }
                lastTypingAt = now;
            }
            else if (typingSessionOpen && (now - lastTypingAt).TotalSeconds > 180)
            {
                typingSessionOpen = false;
                int mins = (int)(lastTypingAt - typingSessionStart).TotalMinutes;
                if (mins > B.LongestTypingMinutes) B.LongestTypingMinutes = mins;
            }
            continuousMinutes = typingSessionOpen ? (int)(now - typingSessionStart).TotalMinutes : 0;

            // modes
            if (mode != Mode.Held)
            {
                Mode nm = typingNow ? Mode.Typing : (idleSec >= Math.Max(2, S.IdleSleepMinutes) * 60 ? Mode.Sleep : Mode.Idle);
                if (nm != mode)
                {
                    if (mode == Mode.Sleep) { if (Cool("sleep_wake", 300)) Say("sleep_wake"); StartAct("stretch"); }
                    mode = nm;
                }
            }

            // away / back
            if (idleSec >= 1800) wasIdleLong = true;
            if (wasIdleLong && idleSec < 3) { wasIdleLong = false; if (Cool("greet_back", 1200)) Say("greet_back"); }

            // lunch learning: a 20+ minute pause starting between 11:30 and 15:00
            string today = now.ToString("yyyy-MM-dd");
            if (lunchDay != today) { lunchDay = today; lunchObservedToday = false; }
            if (!lunchObservedToday && idleSec >= 1200 && idleSec < 1260)
            {
                var start = now.AddSeconds(-idleSec); double h = start.Hour + start.Minute / 60.0;
                if (h >= 11.5 && h <= 15.0) { B.OnLunchObserved(h); lunchObservedToday = true; }
            }

            // break taken after a marathon suggestion?
            if (breakSuggestedAt != DateTime.MinValue)
            {
                if (idleSec >= 180) { B.OnBreakTaken(); breakSuggestedAt = DateTime.MinValue; queuedCat = "break_taken"; queuedAt = t + 1; }
                else if ((now - breakSuggestedAt).TotalMinutes > 12) { B.OnBreakIgnored(); breakSuggestedAt = DateTime.MinValue; }
            }

            // per minute bookkeeping
            if ((now - lastMinuteTick).TotalSeconds >= 60)
            {
                lastMinuteTick = now; int kpm = Kpm();
                B.OnMinute(now, kpm, typingRecent && kpm > 0, idleSec > 120, C.Id);
                if (now.Minute % 5 == 0) { B.Save(); }
            }

            // typing reactions
            if (typingNow)
            {
                int kpm = Kpm();
                if (kpm >= Math.Max(110, B.KpmBaseline * 1.35) && Cool("typing_fast", 480)) { Say("typing_fast"); excitedUntil = t + 4; }
                if (S.BreakReminders && continuousMinutes >= 20 && Cool("typing_long", 1200)) Say("typing_long", 9);
                if (S.BreakReminders && continuousMinutes >= 50 && Cool("typing_marathon", 3000)) { Say("typing_marathon", 9); breakSuggestedAt = now; }
                if (S.BreakReminders && Cool("posture", 2700) && Chance(0.5)) Say("posture", 7);
            }

            // idle chatter
            if (idleSec >= 150 && idleSec < 152 && Cool("idle_short", 900) && Chance(0.8 * Chat)) Say("idle_short");
            int sleepAt = Math.Max(2, S.IdleSleepMinutes) * 60;
            if (idleSec >= sleepAt - 40 && idleSec < sleepAt - 38 && Cool("idle_long", 1800)) Say("idle_long");

            // time-of-day
            double hour = now.Hour + now.Minute / 60.0;
            double lunchH = B.LunchHour > 0 ? B.LunchHour : 13.0;
            if (typingRecent && Math.Abs(hour - lunchH) < 0.2 && Cool("lunch", 72000)) Say("lunch", 7);
            if ((now.Hour >= 23 || now.Hour < 4) && typingRecent && Cool("late_night", 3600)) { Say("late_night", 8); B.OnLateNight(now); }

            // random quips (only while the user is around but not typing)
            if (!typingNow && idleSec < 600 && bubbleText == null && Cool("quip_gate", 240 / Math.Max(0.3, Chat)) && Chance(0.5))
            {
                double r = rnd.NextDouble();
                string cat = r < 0.62 ? "quip" : r < 0.78 ? "care" : r < 0.92 ? "wisdom" : "learned";
                if (cat == "learned" && (B.DaysUsed < 3 || !Cool("learned", 43200))) cat = "quip";
                if (B.Mood < -0.3 && r < 0.5) cat = "care";
                Say(cat);
            }

            // random actions
            if (act == null && mode == Mode.Idle && bubbleText == null && !waterPending && idleSec < 900 && Chance(0.02 * Math.Min(1.5, Chat)))
            {
                string id = Acts.PickRandom(C, B, S.Wander, rnd);
                StartAct(id, Chance(0.45 * Chat));
            }

            Water(now, t);

            // level up
            if (B.Level > lastLevel)
            {
                lastLevel = B.Level; B.LastLevelAnnounced = B.Level;
                Say("levelup", 8); StartAct("celebrate"); if (S.Sound) Sounds.Tada(); UpdateTray();
            }
        }

        // =====================================================================================
        // greetings
        // =====================================================================================
        void Greet(DateTime now)
        {
            if (petHidden) return;
            int h = now.Hour;
            string cat = h < 5 ? "greet_night" : h < 12 ? "greet_morning" : h < 17 ? "greet_afternoon" : h < 21 ? "greet_evening" : "greet_night";
            string today = now.ToString("yyyy-MM-dd");
            string special = null;
            if (S.Get("greeted_day", "") != today)
            {
                S.Set("greeted_day", today); S.Save();
                special = Festival(now);
                if (special == null && S.Birthday != "" && S.Birthday == now.ToString("dd-MM")) special = "birthday";
                if (special == null && now.DayOfWeek == DayOfWeek.Monday) special = "monday";
                if (special == null && now.DayOfWeek == DayOfWeek.Friday) special = "friday";
                if (special == null && (now.DayOfWeek == DayOfWeek.Saturday || now.DayOfWeek == DayOfWeek.Sunday)) special = "weekend";
                if (special == null && B.HasActivityProfile && B.UsualStart > 0)
                {
                    double hh = now.Hour + now.Minute / 60.0;
                    if (hh < B.UsualStart - 1.5) special = "unusual_early";
                }
            }
            Say(cat, 6);
            if (special != null) { queuedCat = special; queuedAt = (float)clock.Elapsed.TotalSeconds + 7; if (special == "birthday") StartAct("celebrate"); }
            StartAct("wave");
        }

        static string Festival(DateTime d)
        {
            string md = d.ToString("MM-dd"), ymd = d.ToString("yyyy-MM-dd");
            if (md == "01-01") return "new_year";
            if (md == "01-26") return "republic_day";
            if (md == "08-15") return "independence_day";
            if (md == "11-14") return "childrens_day";
            if (md == "12-25") return "christmas";
            if (ymd == "2026-03-04" || ymd == "2027-03-22" || ymd == "2028-03-11") return "holi";
            if (ymd == "2026-11-08" || ymd == "2027-10-29" || ymd == "2028-10-17") return "diwali";
            if (ymd == "2026-09-14" || ymd == "2027-09-04" || ymd == "2028-08-23") return "ganesh";
            return null;
        }

        // =====================================================================================
        // water
        // =====================================================================================
        void Water(DateTime now, float t)
        {
            if (S.WaterMinutes <= 0) { waterPending = false; return; }
            if (!waterPending)
            {
                if (now < lastSipAt.AddMinutes(S.WaterMinutes)) return;
                if (idleSec > 180) { waterDeferred = true; returnedAt = DateTime.MinValue; return; }   // user is away: wait for them
                if (waterDeferred)
                {
                    if (returnedAt == DateTime.MinValue) returnedAt = now;
                    if ((now - returnedAt).TotalSeconds < 20) return;
                    waterDeferred = false;
                }
                StartWaterReminder(now, t);
            }
            else
            {
                double el = (now - waterShownAt).TotalSeconds;
                if (nagCount < 3 && el > 150 * (nagCount + 1))
                {
                    nagCount++;
                    string cat = (nagCount >= 2 && B.WaterStyle != "gentle") ? "water_serious" : "water_nag";
                    Say(cat, 600, true, false);
                    if (S.Sound) Sounds.Chime();
                    if (petHidden) Balloon(bubbleText);
                }
                if (el > 720) { waterPending = false; B.OnReminderIgnored(); lastSipAt = now; ClearBubble(); }
            }
        }

        void StartWaterReminder(DateTime now, float t)
        {
            waterPending = true; waterShownAt = now; nagCount = 0; waterStartT = t;
            act = null;
            Say(B.WaterStyle == "serious" ? "water_serious" : "water", 600, true, false);
            if (S.Sound) Sounds.Chime();
            if (petHidden) Balloon(bubbleText);
        }

        void Balloon(string text)
        {
            try { tray.ShowBalloonTip(8000, "SJ Buddy - " + S.PetName, text ?? "Water time!", ToolTipIcon.Info); } catch { }
        }

        void Sip()
        {
            var now = DateTime.Now;
            double latency = waterPending ? (now - waterShownAt).TotalSeconds : 0;
            bool fromReminder = waterPending;
            B.OnSip(now, latency, fromReminder);
            waterPending = false; lastSipAt = now; nagCount = 0; waterDeferred = false;
            ClearBubble();
            Say("water_ack", 6, false, false);
            StartAct("drink");
            if (B.SipsToday == 4 && B.StreakDays >= 2) { queuedCat = "water_streak"; queuedAt = (float)clock.Elapsed.TotalSeconds + 4; }
            if (S.Sound) Sounds.Pop();
            B.Save(); UpdateTray();
        }

        void SnoozeWater()
        {
            waterPending = false; nagCount = 0;
            lastSipAt = DateTime.Now.AddMinutes(5 - S.WaterMinutes);
            ClearBubble(); Say("water_snooze", 5, false, false);
        }

        // =====================================================================================
        // speaking
        // =====================================================================================
        Dictionary<string, string> Vars()
        {
            var v = new Dictionary<string, string>();
            v["name"] = S.PetName; v["user"] = S.UserName; v["sips"] = B.SipsToday.ToString(); v["streak"] = B.StreakDays.ToString();
            v["level"] = B.Level.ToString(); v["title"] = B.Title(S.Language); v["mins"] = continuousMinutes.ToString(); v["kpm"] = Kpm().ToString();
            return v;
        }

        bool Say(string cat) { return Say(cat, 0, false, true); }
        bool Say(string cat, float seconds) { return Say(cat, seconds, false, true); }
        bool Say(string cat, float seconds, bool water, bool spontaneous)
        {
            string text = Lines.Pick(C.Id, cat, S.Language, rnd, Vars());
            if (text == null) return false;
            float dur = seconds > 0 ? seconds : Math.Max(3.5f, Math.Min(12f, 2.5f + text.Length * 0.055f));
            float t = (float)clock.Elapsed.TotalSeconds;
            bubbleText = text; bubbleShownAt = t; bubbleUntil = t + dur; bubbleCat = cat; bubbleWater = water; bubbleSpontaneous = spontaneous; bubbleShownClock = DateTime.Now;
            B.OnLineShown(cat);
            return true;
        }
        void ClearBubble() { bubbleText = null; }

        void SayRandomTalk()
        {
            double r = rnd.NextDouble();
            Say(r < 0.5 ? "quip" : r < 0.7 ? "care" : r < 0.85 ? "wisdom" : "click");
            StartAct(rnd.Next(2) == 0 ? "nod" : "wave");
        }

        void DoPet() { B.OnPet(); Say("pet"); StartAct("love"); }

        // =====================================================================================
        // actions
        // =====================================================================================
        void StartAct(string id) { StartAct(id, false); }
        void StartAct(string id, bool sayLine)
        {
            var a = Acts.Get(id, C, rnd);
            if (a == null) return;
            if (a.NeedsWander && !S.Wander && id != "walk") return;
            if (a.MoveX != 0) walkDir = rnd.Next(2) == 0 ? 1 : -1;
            act = a; actStart = (float)clock.Elapsed.TotalSeconds;
            B.OnActionWatched();
            if (sayLine && a.LineCat != null && Lines.Has(C.Id, a.LineCat)) Say(a.LineCat);
        }

        void MoveBy(float dx)
        {
            Left += (int)Math.Round(dx);
            var scr = Screen.FromPoint(new Point(Left + (int)(W / 2f * scale), Top + (int)(GroundY * scale))).WorkingArea;
            float x0 = Left + (W / 2f - 100) * scale, x1 = Left + (W / 2f + 100) * scale;
            if (x0 < scr.Left) { Left += (int)(scr.Left - x0); walkDir = 1; }
            if (x1 > scr.Right) { Left -= (int)(x1 - scr.Right); walkDir = -1; }
        }

        // =====================================================================================
        // pose per frame
        // =====================================================================================
        Pose BuildPose(float t, DateTime now)
        {
            var p = new Pose();
            p.SY = 1 + 0.012f * (float)Math.Sin(t * 2.1);
            p.Mouth = B.Mood > 0.1 ? Mouth.Smile : (B.Mood > -0.3 ? Mouth.Neutral : Mouth.Sad);

            if (mode == Mode.Held)
            {
                p.Eyes = dragSpeed > 2200 ? Eyes.Dizzy : Eyes.Wide; p.Mouth = Mouth.O;
                p.ArmL = p.ArmR = 150 + 15 * (float)Math.Sin(t * 12);
                p.Walking = true; p.LegPhase = t * 10; p.Tilt = DU.Clamp(dragSpeed / 200f, -12, 12) * (lastDragPt.X >= downScreen.X ? 1 : -1);
                if (dragSpeed > 2200) p.Fx = "sweat";
                return p;
            }

            if (act != null)
            {
                float u = (t - actStart) / act.Dur;
                if (u >= 1) act = null;
                else
                {
                    var q = act.Step(p, u, t);
                    if (act.MoveX != 0) q.Facing = walkDir;
                    ApplyBlink(q, t);
                    return q;
                }
            }

            if (mode == Mode.Sleep)
            {
                p.Eyes = Eyes.Closed; p.Mouth = Mouth.Neutral; p.Fx = "zzz"; p.SY = 1 + 0.02f * (float)Math.Sin(t * 1.5); p.Tilt = -5; p.ArmL = p.ArmR = 4;
                return p;
            }

            if (waterPending)
            {
                p.Prop = "water"; p.ArmR = 95 + 5 * (float)Math.Sin(t * 3); p.Eyes = Eyes.Open; p.Mouth = Mouth.Smile;
                float since = t - waterStartT;
                if (since < 3) { p.Fx = "exclaim"; p.DY = -4 * Math.Abs((float)Math.Sin(t * 4)); }
                else if (nagCount > 0) { p.Fx = "sweat"; p.Brow = -0.6f; }
                TrackCursor(p);
                ApplyBlink(p, t);
                return p;
            }

            if (mode == Mode.Typing)
            {
                p.Prop = "laptop"; p.Eyes = Eyes.Focus; p.PupilX = -0.15f; p.PupilY = 0.5f; p.HeadBob = (float)Math.Sin(t * 8) * 1.5f;
                p.Mouth = B.Mood > -0.3 ? Mouth.Smile : Mouth.Neutral;
                if (t < excitedUntil) { p.Eyes = Eyes.Stars; p.Mouth = Mouth.Laugh; p.DY = -6 * Math.Abs((float)Math.Sin(t * 10)); p.Fx = "sparkle"; }
                ApplyBlink(p, t);
                return p;
            }

            // idle / mouse-only activity
            bool mouseActive = (now - lastMouseMoveAt).TotalSeconds < 1.5;
            if (hovering)
            {
                // the cursor is on the buddy: happy, blushing, leaning into it
                p.Eyes = Eyes.Happy; p.Mouth = Mouth.Smile; p.Blush = 0.7f; p.Tilt = LeanToCursor(0.8f);
                p.ArmL = p.ArmR = 25 + 6 * (float)Math.Sin(t * 5);
            }
            else if (mouseActive)
            {
                // mouse is moving: follow it with the eyes, lean toward it, wide eyes when it zooms past
                p.Eyes = mouseSpeed > 1800 * dpiScale ? Eyes.Wide : Eyes.Open; TrackCursor(p); p.Tilt = LeanToCursor(0.5f);
                var cur = Cursor.Position; var box = CharacterBox();
                float cx = box.Left + box.Width / 2f, cy = box.Top + box.Height / 2f;
                float dist = (float)Math.Sqrt((cur.X - cx) * (cur.X - cx) + (cur.Y - cy) * (cur.Y - cy));
                if (dist < 170 * dpiScale && mouseSpeed > 900 * dpiScale) { p.Eyes = Eyes.Wide; p.Mouth = Mouth.O; p.DX = cur.X < cx ? 10 : -10; p.SY = 1.04f; }
            }
            else if (idleSec >= 20 && bubbleText == null) { p.Eyes = B.Mood >= -0.3 ? Eyes.Happy : Eyes.Sleepy; p.Mouth = Mouth.Smile; }
            else { p.Eyes = Eyes.Open; TrackCursor(p); }
            ApplyBlink(p, t);
            return p;
        }

        float LeanToCursor(float strength)
        {
            var cur = Cursor.Position;
            float hx = Left + (W / 2f) * scale;
            float dx = (cur.X - hx) / (110f * dpiScale);
            if (Math.Abs(cur.X - hx) > 900 * dpiScale) return 0;
            return DU.Clamp(dx, -1, 1) * 8f * strength;
        }

        void TrackCursor(Pose p)
        {
            var cur = Cursor.Position;
            float hx = Left + (W / 2f + p.DX) * scale, hy = Top + (GroundY + C.HeadTop * 0.8f) * scale;
            float dx = cur.X - hx, dy = cur.Y - hy;
            if (Math.Abs(dx) < 900 * dpiScale && Math.Abs(dy) < 900 * dpiScale)
            {
                p.PupilX = DU.Clamp(dx / (260f * dpiScale), -1, 1) * p.Facing;
                p.PupilY = DU.Clamp(dy / (260f * dpiScale), -1, 1);
            }
        }

        void ApplyBlink(Pose p, float t)
        {
            if (t > nextBlinkAt) { blinkUntil = t + 0.13f; nextBlinkAt = t + 2.2f + (float)rnd.NextDouble() * 4f; }
            if (t < blinkUntil && (p.Eyes == Eyes.Open || p.Eyes == Eyes.Focus || p.Eyes == Eyes.Wide)) p.Eyes = Eyes.Closed;
        }

        // =====================================================================================
        // rendering
        // =====================================================================================
        void Render(Pose p, float t)
        {
            cg.Clear(Color.Transparent);
            Preview.DrawCharacter(cg, C, p, t, scale, W / 2f * scale, GroundY * scale, true);
            if (bubbleText != null)
            {
                if (t >= bubbleUntil) bubbleText = null;
                else DrawBubble(p, t);
            }
            Push();
        }

        void DrawBubble(Pose p, float t)
        {
            float alpha = Math.Min(1f, Math.Min((t - bubbleShownAt) / 0.2f, (bubbleUntil - t) / 0.3f));
            if (alpha <= 0) return;
            bool devanagari = false; foreach (char ch in bubbleText) if (ch >= 0x0900 && ch <= 0x097F) { devanagari = true; break; }
            float fpx = 14f * dpiScale * (S.SizeFactor >= 1f ? 1.15f : 1f);
            using (var font = DU.MakeFont(devanagari ? "Nirmala UI" : "Segoe UI", fpx, false))
            using (var small = DU.MakeFont("Segoe UI", fpx * 0.6f, true))
            {
                float maxW = canvas.Width * 0.72f;
                float iconW = bubbleWater ? fpx * 1.6f : 0;
                var sz = cg.MeasureString(bubbleText, font, (int)(maxW - iconW));
                float padX = 12 * dpiScale, padY = 9 * dpiScale;
                float bw = sz.Width + padX * 2 + iconW, bh = sz.Height + padY * 2;
                float hx = (W / 2f + p.DX) * scale, hy = (GroundY + p.DY + C.HeadTop) * scale;
                float bx = hx - bw / 2, by = hy - 16 * dpiScale - bh;
                // keep inside the canvas and the visible screen
                var vs = SystemInformation.VirtualScreen;
                float minX = Math.Max(4, vs.Left - Left + 4), maxX = Math.Min(canvas.Width - 4, vs.Right - Left - 4);
                if (bx < minX) bx = minX; if (bx + bw > maxX) bx = maxX - bw;
                if (by < 4) by = 4;
                int a = (int)(255 * alpha);
                var back = bubbleWater ? Color.FromArgb(a, 232, 244, 255) : Color.FromArgb(a, 255, 255, 255);
                var border = Color.FromArgb(a, C.Theme);
                using (var path = DU.RR(bx, by, bw, bh, 12 * dpiScale))
                {
                    using (var sh = new SolidBrush(Color.FromArgb((int)(40 * alpha), 0, 0, 0))) { cg.TranslateTransform(2, 3); cg.FillPath(sh, path); cg.TranslateTransform(-2, -3); }
                    using (var b = new SolidBrush(back)) cg.FillPath(b, path);
                    using (var pen = new Pen(border, 2.5f * dpiScale)) cg.DrawPath(pen, path);
                }
                // tail
                float tx = DU.Clamp(hx, bx + 20, bx + bw - 20);
                var tail = new[] { new PointF(tx - 9 * dpiScale, by + bh - 1), new PointF(tx + 9 * dpiScale, by + bh - 1), new PointF(hx, by + bh + 12 * dpiScale) };
                using (var b = new SolidBrush(back)) cg.FillPolygon(b, tail);
                using (var pen = new Pen(border, 2.5f * dpiScale)) { cg.DrawLine(pen, tail[0], tail[2]); cg.DrawLine(pen, tail[1], tail[2]); }
                if (bubbleWater) DU.Drop(cg, bx + padX + iconW * 0.35f, by + bh / 2, fpx * 1.1f, Color.FromArgb(a, 90, 170, 250), Color.FromArgb(a, 40, 90, 160));
                using (var b = new SolidBrush(Color.FromArgb(a, DU.Ink))) cg.DrawString(bubbleText, font, b, new RectangleF(bx + padX + iconW, by + padY, sz.Width + 2, sz.Height + 2));
                using (var b = new SolidBrush(Color.FromArgb((int)(120 * alpha), C.Theme))) cg.DrawString("SJ", small, b, bx + bw - padX - fpx * 0.9f, by + bh - padY - fpx * 0.55f);
            }
        }

        void Push()
        {
            IntPtr screenDc = Native.GetDC(IntPtr.Zero);
            IntPtr memDc = Native.CreateCompatibleDC(screenDc);
            IntPtr hBmp = IntPtr.Zero, old = IntPtr.Zero;
            try
            {
                hBmp = canvas.GetHbitmap(Color.FromArgb(0));
                old = Native.SelectObject(memDc, hBmp);
                var size = new Native.SIZE(canvas.Width, canvas.Height);
                var src = new Native.POINT(0, 0);
                var top = new Native.POINT(Left, Top);
                var blend = new Native.BLENDFUNCTION { BlendOp = Native.AC_SRC_OVER, BlendFlags = 0, SourceConstantAlpha = 255, AlphaFormat = Native.AC_SRC_ALPHA };
                Native.UpdateLayeredWindow(Handle, screenDc, ref top, ref size, memDc, ref src, 0, ref blend, Native.ULW_ALPHA);
            }
            finally
            {
                if (old != IntPtr.Zero) Native.SelectObject(memDc, old);
                if (hBmp != IntPtr.Zero) Native.DeleteObject(hBmp);
                Native.DeleteDC(memDc);
                Native.ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        // =====================================================================================
        // mouse
        // =====================================================================================
        void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            mouseDown = true; dragging = false; downScreen = Cursor.Position; downWinPos = Location; downAt = DateTime.Now; lastDragPt = downScreen; lastDragAt = downAt; dragSpeed = 0; shakeSaid = false; heldLongSaid = false;
            Capture = true;
        }

        void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!mouseDown) return;
            var cur = Cursor.Position;
            if (!dragging)
            {
                if (Math.Abs(cur.X - downScreen.X) + Math.Abs(cur.Y - downScreen.Y) < 6 * dpiScale) return;
                dragging = true; mode = Mode.Held; act = null; heldSince = DateTime.Now;
                if (Chance(0.4)) Say("drag", 3);
            }
            Location = new Point(downWinPos.X + cur.X - downScreen.X, downWinPos.Y + cur.Y - downScreen.Y);
            var now = DateTime.Now; double dt = (now - lastDragAt).TotalSeconds;
            if (dt > 0.03)
            {
                float d = (float)Math.Sqrt((cur.X - lastDragPt.X) * (cur.X - lastDragPt.X) + (cur.Y - lastDragPt.Y) * (cur.Y - lastDragPt.Y));
                dragSpeed = dragSpeed * 0.6f + (float)(d / dt) * 0.4f; lastDragPt = cur; lastDragAt = now;
                if (dragSpeed > 2600 && !shakeSaid) { shakeSaid = true; Say("shake", 3); B.Mood = Math.Max(-1, B.Mood - 0.05); }
            }
            if (!heldLongSaid && (now - heldSince).TotalSeconds > 7) { heldLongSaid = true; Say("held_long", 3); }
        }

        void OnMouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right) { ShowMenu(); return; }
            if (e.Button != MouseButtons.Left) return;
            Capture = false;
            if (!mouseDown) return;
            mouseDown = false;
            if (dragging)
            {
                dragging = false; mode = Mode.Idle; dragSpeed = 0;
                ClampToScreen(); S.PosX = Left; S.PosY = Top; S.Save();
                if (Chance(0.35)) Say("drop", 3);
                B.Xp += 1;
                return;
            }
            OnPetClick();
        }

        void OnPetClick()
        {
            B.OnClick();
            if (waterPending) { Sip(); return; }
            if (bubbleSpontaneous && bubbleText != null && (DateTime.Now - bubbleShownClock).TotalSeconds < 5) B.OnLineLiked(bubbleCat);
            if (act != null && act.Id != "wave" && act.Id != "nod") { act = null; }
            string[] reacts = { "jump", "wave", "laugh", "nod", "spin", "love", "salute" };
            StartAct(reacts[rnd.Next(reacts.Length)]);
            if (Chance(0.75)) Say("click");
        }

        void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            mouseDown = false; dragging = false;
            DoPet();
        }
    }
}
