using System;
using System.Windows.Forms;

namespace LeagueAutoAccept
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var main = new Main();
            Application.EnableVisualStyles();
            Application.Run(main);
        }
    }
}