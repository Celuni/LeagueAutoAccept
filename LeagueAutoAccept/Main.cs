using System;
using System.Diagnostics;
using Timer = System.Timers.Timer;
using System.Windows.Forms;

namespace LeagueAutoAccept
{
    class Main : ApplicationContext
    {
        private bool enabled = true;
        private readonly NotifyIcon NotifyIcon;
        private Timer timer;
        private MenuItem aboutMenu;
        private MenuItem enabledMenu;
        private MenuItem quitMenu;

        public Main()
        {
            NotifyIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.Icon,
                Visible = true,
                BalloonTipTitle = "LeagueAutoAccept",
                BalloonTipText = "All ready checks will be accepted automatically."
            };
            NotifyIcon.ShowBalloonTip(5000);
            NotifyMenu();
            StartTimer();
        }

        private void StartTimer()
        {
            timer = new Timer();
            timer.Elapsed += ReadyCheckAccept;
            timer.Interval = 500;
            timer.Start();
        }

        private void ReadyCheckAccept(object sender, EventArgs eventArgs)
        {
            foreach (Process process in LeagueClient.GetLeagueClientProcesses())
            {
                var apiAuth = LeagueClient.GetAPIPortAndToken(process);
                string result = LeagueClient.SendAPIRequest(apiAuth, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
                Console.WriteLine(result);
            }
        }

        private void NotifyMenu()
        {
            aboutMenu = new MenuItem("LeagueAutoAccept")
            {
                Enabled = false
            };

            enabledMenu = new MenuItem("Enabled", (a, e) =>
            {   
                enabled = !enabled;
                NotifyMenu();
                if (!enabled) timer.Dispose(); else StartTimer();
            })
            {
                Checked = enabled
            };

            quitMenu = new MenuItem("Quit", (a, e) =>
            {
                aboutMenu.Dispose();
                enabledMenu.Dispose();
                Application.Exit();
            });

            NotifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { aboutMenu, enabledMenu, quitMenu });
        }
    }
}