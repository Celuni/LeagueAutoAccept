using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using WebSocketSharp;

namespace LeagueAutoAccept
{
    class Main : ApplicationContext
    {
        private bool enabled = true;
        private readonly NotifyIcon NotifyIcon;
        private MenuItem aboutMenu;
        private MenuItem enabledMenu;
        private MenuItem quitMenu;
        private readonly WebSocket ws;

        public Main()
        {
            NotifyIcon = new NotifyIcon()
            {
                Icon = Properties.Resources.Icon,
                Visible = true,
                BalloonTipTitle = "LeagueAutoAccept",
                BalloonTipText = "All ready checks will be accepted automatically."
            };
            NotifyIcon.ShowBalloonTip(500);
            NotifyMenu();

            foreach (Process process in LeagueClient.GetLeagueClientProcesses())
            {
                var apiAuth = LeagueClient.GetAPIPortAndToken(process);
                ws = new WebSocket($"wss://127.0.0.1:{apiAuth.Port}/", "wamp");
                ws.SetCredentials("riot", apiAuth.Token, true);
                ws.SslConfiguration.ServerCertificateValidationCallback = (send, certificate, chain, sslPolicyErrors) => true;
                ws.OnMessage += (s, e) =>
                {
                    if (e.IsText && enabled)
                    {
                        var eventArray = JArray.Parse(e.Data);
                        var eventNumber = eventArray[0].ToObject<int>();
                        if (eventNumber == 8)
                        {
                            var leagueEvent = eventArray[2];
                            var leagueEventData = leagueEvent.Value<string>("data");
                            if (leagueEventData == "ReadyCheck")
                            {
                                NotifyIcon.BalloonTipText = "Accepted Ready Check!";
                                NotifyIcon.ShowBalloonTip(200);

                                string result = LeagueClient.SendAPIRequest(apiAuth, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
                                Console.WriteLine(result);
                            }
                        }
                    }
                };
                ws.Connect();
                ws.Send("[5, \"OnJsonApiEvent_lol-gameflow_v1_gameflow-phase\"]");
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