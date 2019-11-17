using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using LeagueAutoAccept.Properties;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace LeagueAutoAccept
{
    internal class Main : ApplicationContext
    {
        internal static NotifyIcon NotifyIcon;
        private bool _enabled = true;
        private bool _noLcuRunning = true;

        public Main()
        {
            NotifyIcon = new NotifyIcon
            {
                Icon = Resources.Icon,
                Visible = true,
                BalloonTipTitle = Resources.Title,
                BalloonTipText = Resources.NotificationStartText
            };
            NotifyIcon.ShowBalloonTip(500);
            NotifyMenu();
            StartCheckingForLcuStart();
        }

        private async void StartCheckingForLcuStart()
        {
            while (_noLcuRunning)
                if (LeagueClient.GetLeagueClientProcesses().Length == 0)
                {
                    await Task.Run(() => Thread.Sleep(30000));
                }
                else
                {
                    _noLcuRunning = false;
                    AcceptReadyChecks();
                }
        }

        private void AcceptReadyChecks()
        {
            foreach (var process in LeagueClient.GetLeagueClientProcesses())
            {
                var apiAuth = LeagueClient.GetApiPortAndToken(process);
                var ws = new WebSocket($"wss://127.0.0.1:{apiAuth.Port}/", "wamp");
                ws.SetCredentials("riot", apiAuth.Token, true);
                ws.SslConfiguration.ServerCertificateValidationCallback = (send, certificate, chain, sslPolicyErrors) => true;
                ws.OnMessage += (s, e) =>
                {
                    if (!e.IsText || !_enabled) return;

                    var eventArray = JArray.Parse(e.Data);
                    var eventNumber = eventArray[0].ToObject<int>();

                    if (eventNumber != 8) return;

                    var leagueEvent = eventArray[2];
                    var leagueEventData = leagueEvent.Value<string>("data");

                    if (leagueEventData != "ReadyCheck") return;

                    NotifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    NotifyIcon.BalloonTipText = Resources.NotificationAcceptReadyCheck;
                    NotifyIcon.ShowBalloonTip(200);
                    var result = LeagueClient.SendApiRequest(apiAuth, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
                    Trace.WriteLine(result);
                };
                ws.Connect();
                ws.Send("[5, \"OnJsonApiEvent_lol-gameflow_v1_gameflow-phase\"]");
                ws.OnClose += (sender, args) =>
                {
                    _noLcuRunning = true;
                    StartCheckingForLcuStart();
                };
            }
        }

        private void NotifyMenu()
        {
            NotifyIcon.ContextMenu = new ContextMenu();
            NotifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Title) {Enabled = false});
            NotifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Enabled, (a, e) =>
            {
                _enabled = !_enabled;
                NotifyMenu();
            })
            {
                Checked = _enabled
            });
            NotifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Quit, (a, e) => Application.Exit()));
        }
    }
}