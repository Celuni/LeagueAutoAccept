﻿using System;
using System.Windows.Forms;

namespace LeagueAutoAccept
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            var main = new Main();
            Application.EnableVisualStyles();
            Application.Run(main);
        }
    }
}