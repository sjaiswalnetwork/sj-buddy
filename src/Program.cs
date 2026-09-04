using System;
using System.Threading;
using System.Windows.Forms;

namespace SJBuddy
{
    static class Program
    {
        public static bool AutoStarted;
        public static string StartCommand = "";          // "settings" | "stats" | "" (from the command line)
        public static EventWaitHandle ShowEvent, SettingsEvent, StatsEvent;

        [STAThread]
        static void Main(string[] args)
        {
            string sheetPath = null;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--autostart") AutoStarted = true;
                if (args[i] == "--settings") StartCommand = "settings";
                if (args[i] == "--stats") StartCommand = "stats";
                if (args[i] == "--sheet" && i + 1 < args.Length) sheetPath = args[i + 1];
            }
            try { Native.SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (sheetPath != null) { Preview.RenderSheet(sheetPath); return; }

            bool created;
            using (var mutex = new Mutex(true, "Local\\SJBuddy.SingleInstance", out created))
            {
                if (!created)
                {
                    // Second launch: tell the running copy to show itself (or open a dialog).
                    string evName = StartCommand == "settings" ? "Local\\SJBuddy.SettingsEvent" : StartCommand == "stats" ? "Local\\SJBuddy.StatsEvent" : "Local\\SJBuddy.ShowEvent";
                    try { var ev = EventWaitHandle.OpenExisting(evName); ev.Set(); }
                    catch { MessageBox.Show("SJ Buddy is already running. Look for its icon in the system tray (bottom-right, near the clock).", "SJ Buddy", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                    return;
                }
                ShowEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\SJBuddy.ShowEvent");
                SettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\SJBuddy.SettingsEvent");
                StatsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\SJBuddy.StatsEvent");
                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
                Application.ThreadException += (s, e) => Paths.Log(e.Exception.ToString());
                AppDomain.CurrentDomain.UnhandledException += (s, e) => Paths.Log(e.ExceptionObject.ToString());
                try { Application.Run(new PetForm()); }
                catch (Exception ex)
                {
                    Paths.Log(ex.ToString());
                    MessageBox.Show("SJ Buddy hit an error and has to close.\n\n" + ex.Message + "\n\nDetails were saved to:\n" + Paths.LogFile, "SJ Buddy", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                GC.KeepAlive(mutex);
            }
        }
    }
}
