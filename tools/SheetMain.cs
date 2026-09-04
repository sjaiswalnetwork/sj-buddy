using System;

namespace SJBuddy
{
    // Standalone entry point used only to render the character preview sheet during development.
    static class SheetMain
    {
        [STAThread]
        static void Main(string[] args)
        {
            string path = args.Length > 0 ? args[0] : "sheet.png";
            Preview.RenderSheet(path);
            Console.WriteLine("wrote " + path);
        }
    }
}
