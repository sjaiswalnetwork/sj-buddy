using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SJBuddy
{
    // Live animated preview of one character, used in the setup dialog.
    class PreviewPanel : Panel
    {
        public Character Ch; public bool Selected; float t; Timer timer;
        public PreviewPanel(Character c)
        {
            Ch = c; DoubleBuffered = true; Width = 118; Height = 150; Cursor = Cursors.Hand; BackColor = Color.White;
            timer = new Timer(); timer.Interval = 60; timer.Tick += (s, e) => { t += 0.06f; Invalidate(); }; timer.Start();
            Disposed += (s, e) => timer.Dispose();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Selected ? Color.FromArgb(232, 242, 255) : Color.White);
            using (var pen = new Pen(Selected ? Color.FromArgb(40, 120, 240) : Color.FromArgb(215, 220, 230), Selected ? 3 : 1.5f)) g.DrawRectangle(pen, 1, 1, Width - 3, Height - 3);
            var p = new Pose(); p.SY = 1 + 0.02f * (float)Math.Sin(t * 2); p.ArmR = 20 + 10 * (float)Math.Sin(t * 3);
            p.Eyes = ((int)(t * 2) % 9 == 0) ? Eyes.Closed : Eyes.Open; p.Mouth = Mouth.Smile;
            Preview.DrawCharacter(g, Ch, p, t, (Height - 34) / 300f, Width / 2f, Height - 28, true);
            using (var f = new Font("Segoe UI", 9, Selected ? FontStyle.Bold : FontStyle.Regular))
            using (var b = new SolidBrush(Color.FromArgb(50, 50, 60)))
            {
                var sz = g.MeasureString(Ch.Name, f);
                g.DrawString(Ch.Name, f, b, (Width - sz.Width) / 2, Height - 22);
            }
        }
    }

    // First-run wizard and the Settings window (same form).
    public class SetupDialog : Form
    {
        Settings S; List<PreviewPanel> panels = new List<PreviewPanel>();
        TextBox tbPet, tbUser, tbBday; ComboBox cbWater, cbLang, cbChat, cbSize, cbSleep;
        CheckBox ckAuto, ckSound, ckWander, ckBreaks, ckTop, ckHotkey;
        public bool AutoStartWanted;
        string selected;

        float k = 1f;   // DPI layout factor
        int Px(float v) { return (int)Math.Round(v * k); }

        public SetupDialog(Settings s, bool firstRun)
        {
            S = s; selected = S.Character;
            Text = firstRun ? "Welcome to SJ Buddy" : "SJ Buddy - Settings";
            Font = new Font("Segoe UI", 9.5f);
            AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; ShowInTaskbar = true; TopMost = true;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            using (var g = CreateGraphics()) k = g.DpiX / 96f;
            ClientSize = new Size(Px(700), Px(588));
            BackColor = Color.White;

            var title = new Label { Text = firstRun ? "Pick your desk buddy" : "Settings", Font = new Font("Segoe UI", 16, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 120), Left = Px(20), Top = Px(14), AutoSize = true };
            Controls.Add(title);
            var sub = new Label { Text = "SJ Buddy sits on your screen, reacts when you type, reminds you to drink water and learns your routine.   Made by SJ", Left = Px(20), Top = Px(50), Width = Px(660), Height = Px(22), ForeColor = Color.FromArgb(90, 90, 110) };
            Controls.Add(sub);

            int x = Px(20);
            foreach (var id in Characters.Ids)
            {
                var pp = new PreviewPanel(Characters.Get(id)) { Left = x, Top = Px(78), Width = Px(126), Height = Px(156) };
                pp.Selected = id == selected;
                string cid = id;
                pp.Click += (a, b) => Select(cid);
                pp.DoubleClick += (a, b) => { Select(cid); };
                Controls.Add(pp); panels.Add(pp); x += Px(132);
            }

            int y = Px(246), col2 = Px(360), w = Px(320);
            tbPet = AddText("Buddy's name", S.PetName == "" ? Characters.DisplayName(selected) : S.PetName, Px(20), y, w);
            tbUser = AddText("Your name (how it should address you)", S.UserName, col2, y, w);
            y += Px(54);
            tbBday = AddText("Your birthday (dd-MM, optional)", S.Birthday, Px(20), y, w);
            cbWater = AddCombo("Water reminder", new[] { "Off", "Every 15 minutes", "Every 20 minutes", "Every 30 minutes", "Every 45 minutes", "Every 60 minutes", "Every 90 minutes" }, WaterIndex(S.WaterMinutes), col2, y, w);
            y += Px(54);
            cbLang = AddCombo("Language of the lines", new[] { "English", "Hindi (हिंदी)", "Hinglish", "Mix (surprise me)" }, Idx(new[] { "en", "hi", "hing", "mix" }, S.Language), Px(20), y, w);
            cbChat = AddCombo("Talkativeness", new[] { "Quiet", "Normal", "Chatty" }, Idx(new[] { "quiet", "normal", "chatty" }, S.Chattiness), col2, y, w);
            y += Px(54);
            cbSize = AddCombo("Size on screen", new[] { "Small", "Medium", "Large", "Extra large" }, Idx(new[] { "small", "medium", "large", "xl" }, S.Size), Px(20), y, w);
            cbSleep = AddCombo("Falls asleep after (no activity)", new[] { "3 minutes", "5 minutes", "10 minutes", "20 minutes", "45 minutes" }, Idx(new[] { "3", "5", "10", "20", "45" }, S.IdleSleepMinutes.ToString()), col2, y, w);
            y += Px(58);
            ckAuto = AddCheck("Start with Windows (auto-start at login)", AutoStart.IsEnabled(), Px(20), y, w);
            ckTop = AddCheck("Always on top of other windows", S.AlwaysOnTop, col2, y, w); y += Px(27);
            ckSound = AddCheck("Sounds (soft chime for reminders)", S.Sound, Px(20), y, w);
            ckBreaks = AddCheck("Break, posture and eye-rest reminders", S.BreakReminders, col2, y, w); y += Px(27);
            ckWander = AddCheck("Let it wander around the screen", S.Wander, Px(20), y, w);
            ckHotkey = AddCheck("Hotkey Ctrl+Alt+B shows / hides the buddy", S.Hotkey, col2, y, w); y += Px(34);

            var note = new Label { Left = Px(20), Top = y, Width = Px(660), Height = Px(36), ForeColor = Color.FromArgb(120, 120, 135), Text = "Privacy: the buddy only counts that you typed, never what you typed. Everything stays on this PC. No internet needed." };
            Controls.Add(note);

            var ok = new Button { Text = firstRun ? "Let's go!" : "Save", Left = Px(470), Top = Px(544), Width = Px(110), Height = Px(32), DialogResult = DialogResult.OK };
            var cancel = new Button { Text = "Cancel", Left = Px(590), Top = Px(544), Width = Px(90), Height = Px(32), DialogResult = DialogResult.Cancel };
            ok.Click += (a, b) => Apply();
            Controls.Add(ok); Controls.Add(cancel);
            AcceptButton = ok; CancelButton = cancel;
        }

        static int WaterIndex(int m) { switch (m) { case 0: return 0; case 15: return 1; case 20: return 2; case 45: return 4; case 60: return 5; case 90: return 6; default: return 3; } }
        static int Idx(string[] arr, string v) { for (int i = 0; i < arr.Length; i++) if (arr[i] == v) return i; return 0; }

        void Select(string id)
        {
            string prevDefault = Characters.DisplayName(selected);
            selected = id;
            foreach (var p in panels) { p.Selected = p.Ch.Id == id; p.Invalidate(); }
            if (tbPet.Text.Trim() == "" || tbPet.Text.Trim() == prevDefault) tbPet.Text = Characters.DisplayName(id);
        }

        TextBox AddText(string label, string value, int x, int y, int w)
        {
            Controls.Add(new Label { Text = label, Left = x, Top = y, AutoSize = true, ForeColor = Color.FromArgb(70, 70, 85) });
            var tb = new TextBox { Left = x, Top = y + Px(21), Width = w, Text = value };
            Controls.Add(tb); return tb;
        }
        ComboBox AddCombo(string label, string[] items, int idx, int x, int y, int w)
        {
            Controls.Add(new Label { Text = label, Left = x, Top = y, AutoSize = true, ForeColor = Color.FromArgb(70, 70, 85) });
            var cb = new ComboBox { Left = x, Top = y + Px(21), Width = w, DropDownStyle = ComboBoxStyle.DropDownList };
            cb.Items.AddRange(items); cb.SelectedIndex = Math.Max(0, Math.Min(items.Length - 1, idx));
            Controls.Add(cb); return cb;
        }
        CheckBox AddCheck(string label, bool val, int x, int y, int w)
        {
            var ck = new CheckBox { Text = label, Left = x, Top = y, Width = w, Height = Px(24), Checked = val };
            Controls.Add(ck); return ck;
        }

        void Apply()
        {
            S.Character = selected;
            S.PetName = tbPet.Text.Trim() == "" ? Characters.DisplayName(selected) : tbPet.Text.Trim();
            S.UserName = tbUser.Text.Trim();
            S.Birthday = tbBday.Text.Trim();
            S.WaterMinutes = new[] { 0, 15, 20, 30, 45, 60, 90 }[cbWater.SelectedIndex];
            S.Language = new[] { "en", "hi", "hing", "mix" }[cbLang.SelectedIndex];
            S.Chattiness = new[] { "quiet", "normal", "chatty" }[cbChat.SelectedIndex];
            S.Size = new[] { "small", "medium", "large", "xl" }[cbSize.SelectedIndex];
            S.IdleSleepMinutes = new[] { 3, 5, 10, 20, 45 }[cbSleep.SelectedIndex];
            S.AlwaysOnTop = ckTop.Checked; S.Sound = ckSound.Checked; S.Wander = ckWander.Checked; S.BreakReminders = ckBreaks.Checked; S.Hotkey = ckHotkey.Checked;
            AutoStartWanted = ckAuto.Checked;
            S.Save();
        }
    }

    public static class InputDialog
    {
        public static string Ask(string title, string prompt, string value)
        {
            using (var f = new Form())
            {
                f.Text = title; f.Font = new Font("Segoe UI", 9.5f); f.AutoScaleMode = AutoScaleMode.None; f.FormBorderStyle = FormBorderStyle.FixedDialog; f.MaximizeBox = false; f.MinimizeBox = false;
                f.StartPosition = FormStartPosition.CenterScreen; f.TopMost = true; f.ShowInTaskbar = true;
                try { f.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
                float k; using (var g = f.CreateGraphics()) k = g.DpiX / 96f;
                f.ClientSize = new Size((int)(380 * k), (int)(136 * k));
                var lbl = new Label { Text = prompt, Left = (int)(14 * k), Top = (int)(14 * k), Width = (int)(350 * k), AutoSize = true };
                var tb = new TextBox { Left = (int)(14 * k), Top = (int)(44 * k), Width = (int)(350 * k), Text = value, MaxLength = 24 };
                var ok = new Button { Text = "OK", Left = (int)(190 * k), Top = (int)(88 * k), Width = (int)(84 * k), Height = (int)(30 * k), DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Cancel", Left = (int)(280 * k), Top = (int)(88 * k), Width = (int)(84 * k), Height = (int)(30 * k), DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                tb.SelectAll();
                return f.ShowDialog() == DialogResult.OK ? tb.Text : null;
            }
        }
    }

    public class StatsDialog : Form
    {
        PetForm pet;
        float k = 1f;
        int Px(float v) { return (int)Math.Round(v * k); }

        public StatsDialog(PetForm p)
        {
            pet = p;
            Text = "SJ Buddy - Stats and what " + p.S.PetName + " learned";
            Font = new Font("Segoe UI", 9.5f); AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; TopMost = true; ShowInTaskbar = true; BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            using (var g = CreateGraphics()) k = g.DpiX / 96f;
            ClientSize = new Size(Px(600), Px(470));
            string who = p.S.PetName == p.C.Name ? p.C.Name : p.S.PetName + " (" + p.C.Name + ")";
            var title = new Label { Text = who + "  -  Level " + p.B.Level + " \"" + p.B.Title(p.S.Language) + "\"", Font = new Font("Segoe UI", 13, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 120), Left = Px(16), Top = Px(12), AutoSize = true };
            Controls.Add(title);
            var xp = new ProgressBar { Left = Px(16), Top = Px(46), Width = Px(568), Height = Px(12), Minimum = 0, Maximum = 1000 };
            int prev = (p.B.Level - 1) * (p.B.Level - 1) * 50; int next = p.B.XpForNext;
            xp.Value = (int)Math.Max(0, Math.Min(1000, 1000.0 * (p.B.Xp - prev) / Math.Max(1, next - prev)));
            Controls.Add(xp);
            Controls.Add(new Label { Text = "XP " + p.B.Xp + " / " + next + " for the next level. XP comes from sips (+10), clicks (+2), pets (+3), typing minutes (+1) and breaks (+5).", Left = Px(16), Top = Px(62), Width = Px(568), Height = Px(38), ForeColor = Color.FromArgb(110, 110, 125) });
            var box = new RichTextBox { Left = Px(16), Top = Px(104), Width = Px(568), Height = Px(310), ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.FromArgb(247, 248, 252), Font = new Font(Devanagari(p.S.Language) ? "Nirmala UI" : "Segoe UI", 10f) };
            var sb = new System.Text.StringBuilder();
            foreach (var line in p.B.Summary(p.S.PetName, p.S.Language)) sb.Append("•  ").Append(line).Append("\n\n");
            box.Text = sb.ToString();
            Controls.Add(box);
            var sip = new Button { Text = "Log a sip now", Left = Px(16), Top = Px(426), Width = Px(140), Height = Px(32) };
            sip.Click += (s, e) => { pet.LogSipFromDialog(); Close(); };
            var reset = new Button { Text = "Reset learning", Left = Px(164), Top = Px(426), Width = Px(140), Height = Px(32) };
            reset.Click += (s, e) => { if (MessageBox.Show(this, "Forget everything learned (stats, level, habits)?", "SJ Buddy", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) { pet.ResetLearning(); Close(); } };
            var close = new Button { Text = "Close", Left = Px(484), Top = Px(426), Width = Px(100), Height = Px(32), DialogResult = DialogResult.OK };
            Controls.AddRange(new Control[] { sip, reset, close });
            CancelButton = close;
        }
        static bool Devanagari(string lang) { return lang == "hi"; }
    }

    public class AboutDialog : Form
    {
        float k = 1f;
        int Px(float v) { return (int)Math.Round(v * k); }

        public AboutDialog()
        {
            Text = "About SJ Buddy";
            Font = new Font("Segoe UI", 9.5f); AutoScaleMode = AutoScaleMode.None; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen; TopMost = true; ShowInTaskbar = true; BackColor = Color.White;
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
            using (var g = CreateGraphics()) k = g.DpiX / 96f;
            ClientSize = new Size(Px(520), Px(440));
            int ls = Px(72);
            var logo = new Panel { Left = Px(20), Top = Px(18), Width = ls, Height = ls };
            logo.Paint += (s, e) =>
            {
                var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
                using (var path = DU.RR(1, 1, ls - 2, ls - 2, ls * 0.25f)) using (var br = new LinearGradientBrush(new Point(0, 0), new Point(ls, ls), Color.FromArgb(86, 150, 255), Color.FromArgb(28, 88, 214))) g.FillPath(br, path);
                using (var f = new Font("Segoe UI", 22, FontStyle.Bold)) using (var b = new SolidBrush(Color.White)) { var sz = g.MeasureString("SJ", f); g.DrawString("SJ", f, b, (ls - sz.Width) / 2, (ls - sz.Height) / 2); }
            };
            Controls.Add(logo);
            Controls.Add(new Label { Text = "SJ Buddy", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(30, 60, 120), Left = Px(104), Top = Px(20), AutoSize = true });
            Controls.Add(new Label { Text = "Version 1.0  -  Made by SJ", Left = Px(106), Top = Px(58), AutoSize = true, ForeColor = Color.FromArgb(90, 90, 110) });
            var body = new Label
            {
                Left = Px(20), Top = Px(104), Width = Px(480), Height = Px(290), ForeColor = Color.FromArgb(60, 60, 75),
                Text = "A tiny desktop companion with five characters: Shinchan, SpongeBob, Doraemon, Jethalal and Bhide.\n\n" +
                       "•  Reacts when you type, rests when you're idle, sleeps when you're away.\n" +
                       "•  Reminds you to sip water (default every 30 minutes) and keeps your streak.\n" +
                       "•  Learns your routine: usual start time, typing speed, lunch hour, how chatty you like it, how fast you respond to reminders.\n" +
                       "•  Random animations and punchlines in English, Hindi and Hinglish.\n" +
                       "•  Drag to move, click to talk, double-click to pet, right-click for the menu, Ctrl+Alt+B to hide/show.\n" +
                       "•  Runs from the system tray, can start with Windows, works offline and needs no installation.\n\n" +
                       "Privacy: it counts keystrokes to know when you type; it never records what you type.\n" +
                       "Add your own lines: create my-lines.txt next to the exe (see the guide).\n\n" +
                       "Characters belong to their respective creators; this is a fan-made companion for personal use."
            };
            Controls.Add(body);
            var ok = new Button { Text = "Close", Left = Px(400), Top = Px(398), Width = Px(100), Height = Px(32), DialogResult = DialogResult.OK };
            Controls.Add(ok); AcceptButton = ok; CancelButton = ok;
        }
    }
}
