using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace SJBuddy
{
    // Tiny key=value file store (no dependencies, human readable).
    public class KvStore
    {
        protected Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public string FilePath;
        public KvStore(string path) { FilePath = path; Load(); }

        public void Load()
        {
            map.Clear();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (var raw in File.ReadAllLines(FilePath, Encoding.UTF8))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    int i = line.IndexOf('=');
                    if (i <= 0) continue;
                    map[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
            }
            catch { }
        }

        public void Save()
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                var sb = new StringBuilder();
                sb.AppendLine("# SJ Buddy data file. Safe to delete (it just resets). Made by SJ.");
                var keys = new List<string>(map.Keys);
                keys.Sort(StringComparer.OrdinalIgnoreCase);
                foreach (var k in keys) sb.Append(k).Append('=').Append(map[k]).AppendLine();
                File.WriteAllText(FilePath, sb.ToString(), Encoding.UTF8);
            }
            catch { }
        }

        public string Get(string k, string d) { string v; return map.TryGetValue(k, out v) ? v : d; }
        public int GetInt(string k, int d) { int v; var s = Get(k, null); return (s != null && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) ? v : d; }
        public long GetLong(string k, long d) { long v; var s = Get(k, null); return (s != null && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v)) ? v : d; }
        public double GetDouble(string k, double d) { double v; var s = Get(k, null); return (s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) ? v : d; }
        public bool GetBool(string k, bool d) { var v = Get(k, null); if (v == null) return d; return v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase); }
        public void Set(string k, string v) { map[k] = v ?? ""; }
        public void Set(string k, int v) { map[k] = v.ToString(CultureInfo.InvariantCulture); }
        public void Set(string k, long v) { map[k] = v.ToString(CultureInfo.InvariantCulture); }
        public void Set(string k, double v) { map[k] = v.ToString("R", CultureInfo.InvariantCulture); }
        public void Set(string k, bool v) { map[k] = v ? "1" : "0"; }
        public void Clear() { map.Clear(); }
    }

    public static class Paths
    {
        public static readonly string ExeDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string ExePath = Application.ExecutablePath;
        static string dataDir;
        // Data folder: %APPDATA%\SJ Buddy normally; the exe folder if a file named "portable.txt" sits next to the exe.
        public static string DataDir
        {
            get
            {
                if (dataDir != null) return dataDir;
                try
                {
                    if (File.Exists(Path.Combine(ExeDir, "portable.txt"))) { dataDir = ExeDir; return dataDir; }
                }
                catch { }
                dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SJ Buddy");
                try { Directory.CreateDirectory(dataDir); } catch { }
                return dataDir;
            }
        }
        public static string SettingsFile { get { return Path.Combine(DataDir, "settings.txt"); } }
        public static string BrainFile { get { return Path.Combine(DataDir, "brain.txt"); } }
        public static string LogFile { get { return Path.Combine(DataDir, "error.log"); } }
        public static string CustomLinesFile { get { return Path.Combine(ExeDir, "my-lines.txt"); } }
        public static string CustomLinesFile2 { get { return Path.Combine(DataDir, "my-lines.txt"); } }

        public static void Log(string msg)
        {
            try { File.AppendAllText(LogFile, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine); } catch { }
        }
    }

    public class Settings : KvStore
    {
        public Settings() : base(Paths.SettingsFile) { }

        public string Character { get { return Get("character", "doraemon"); } set { Set("character", value); } }
        public string PetName { get { return Get("petname", ""); } set { Set("petname", value); } }
        public string UserName { get { return Get("username", ""); } set { Set("username", value); } }
        public string Birthday { get { return Get("birthday", ""); } set { Set("birthday", value); } }   // dd-MM
        public int WaterMinutes { get { return GetInt("water_minutes", 30); } set { Set("water_minutes", value); } }
        public string Language { get { return Get("language", "hing"); } set { Set("language", value); } } // en | hi | hing | mix
        public string Chattiness { get { return Get("chattiness", "normal"); } set { Set("chattiness", value); } } // quiet | normal | chatty
        public string Size { get { return Get("size", "medium"); } set { Set("size", value); } } // small | medium | large | xl
        public bool Sound { get { return GetBool("sound", true); } set { Set("sound", value); } }
        public bool Voice { get { return GetBool("voice", false); } set { Set("voice", value); } }
        public bool AlwaysOnTop { get { return GetBool("always_on_top", true); } set { Set("always_on_top", value); } }
        public bool Wander { get { return GetBool("wander", true); } set { Set("wander", value); } }
        public bool BreakReminders { get { return GetBool("break_reminders", true); } set { Set("break_reminders", value); } }
        public int IdleSleepMinutes { get { return GetInt("idle_sleep_minutes", 10); } set { Set("idle_sleep_minutes", value); } }
        public int PosX { get { return GetInt("pos_x", int.MinValue); } set { Set("pos_x", value); } }
        public int PosY { get { return GetInt("pos_y", int.MinValue); } set { Set("pos_y", value); } }
        public bool FirstRunDone { get { return GetBool("first_run_done", false); } set { Set("first_run_done", value); } }
        public bool Hotkey { get { return GetBool("hotkey", true); } set { Set("hotkey", value); } }
        public bool StartHidden { get { return GetBool("start_hidden", false); } set { Set("start_hidden", value); } }

        public float SizeFactor
        {
            get
            {
                switch (Size) { case "small": return 0.55f; case "large": return 1.0f; case "xl": return 1.3f; default: return 0.75f; }
            }
        }
        public double ChattinessFactor
        {
            get { switch (Chattiness) { case "quiet": return 0.45; case "chatty": return 1.8; default: return 1.0; } }
        }
    }

    // "Start with Windows" via the per-user Run registry key (no admin needed).
    public static class AutoStart
    {
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string ValueName = "SJ Buddy";
        public static bool IsEnabled()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (k == null) return false;
                    var v = k.GetValue(ValueName) as string;
                    return !string.IsNullOrEmpty(v);
                }
            }
            catch { return false; }
        }
        public static bool PointsToThisExe()
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, false))
                {
                    if (k == null) return false;
                    var v = k.GetValue(ValueName) as string;
                    return v != null && v.IndexOf(Paths.ExePath, StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch { return false; }
        }
        public static void Set(bool enabled)
        {
            try
            {
                using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (enabled) k.SetValue(ValueName, "\"" + Paths.ExePath + "\" --autostart");
                    else k.DeleteValue(ValueName, false);
                }
            }
            catch { }
        }
    }
}
