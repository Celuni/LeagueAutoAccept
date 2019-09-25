using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace LeagueAutoAccept
{
    class Main : ApplicationContext
    {
        private NotifyIcon notifyIcon;
        private bool enabled = true;

        public Main()
        {
            notifyIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.Icon,
                Visible = true,
                BalloonTipTitle = "LeagueAutoAccept",
                BalloonTipText = "All ready checks will be accepted automatically."
            };
            notifyIcon.ShowBalloonTip(5000);
            notifyMenu();

            Timer timer = new Timer();
            timer.Tick += new EventHandler(readyCheckAccept);
            timer.Interval = 500;
            timer.Start();
        }

        private void readyCheckAccept(object sender, EventArgs e)
        {
            if (enabled)
            {
                foreach (Process process in LeagueClient.getLeagueClientProcesses())
                {
                    var apiAuth = LeagueClient.getAPIPortAndToken(process);
                    string result = LeagueClient.sendAPIRequest(apiAuth.Item1, apiAuth.Item2, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
                    Console.WriteLine(result);
                }
            }
        }

        private void notifyMenu()
        {
            var aboutMenu = new MenuItem("LeagueAutoAccept");
            aboutMenu.Enabled = false;

            var enabledMenu = new MenuItem("Enabled", (a, e) =>
            {
                enabled = !enabled;
                notifyMenu();
            });
            enabledMenu.Checked = enabled;

            var quitMenu = new MenuItem("Quit", (a, e) =>
            {
                Application.Exit();
            });

            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { aboutMenu, enabledMenu, quitMenu });
        }
    }
}