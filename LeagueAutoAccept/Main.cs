using Newtonsoft.Json.Linq;
using System.Threading;
using System.Windows.Forms;
using LeagueAutoAccept.Properties;
using WebSocketSharp;

namespace LeagueAutoAccept
{
    class Main : ApplicationContext
    {
        private bool _enabled = true;
        private bool _noLcuRunning = true;
        private readonly NotifyIcon _notifyIcon;
        private MenuItem _aboutMenu;
        private MenuItem _enabledMenu;
        private MenuItem _quitMenu;

        public Main()
        {
            _notifyIcon = new NotifyIcon()
            {
                Icon = Resources.Icon,
                Visible = true,
                BalloonTipTitle = Resources.Title,
                BalloonTipText = Resources.NotificationStartText
            };
            _notifyIcon.ShowBalloonTip(500);
            NotifyMenu();
            StartCheckingForLcuStart();
        }

        private void StartCheckingForLcuStart()
        {
            while (_noLcuRunning)
            {
                if (LeagueClient.GetLeagueClientProcesses().Length == 0)
                {
                    Thread.Sleep(30000);
                }
                else
                {
                    _noLcuRunning = false;
                    AcceptReadyChecks();
                }
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
                    
                    _notifyIcon.BalloonTipText = Resources.NotificationAcceptReadyCheck;
                    _notifyIcon.ShowBalloonTip(200);
                    LeagueClient.SendApiRequest(apiAuth, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
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
            _aboutMenu = new MenuItem(Resources.Title)
            {
                Enabled = false
            };

            _enabledMenu = new MenuItem("Enabled", (a, e) =>
            {
                _enabled = !_enabled;
                NotifyMenu();
            })
            {
                Checked = _enabled
            };

            _quitMenu = new MenuItem("Quit", (a, e) =>
            {
                Application.Exit();
            });

            _notifyIcon.ContextMenu = new ContextMenu(new[] { _aboutMenu, _enabledMenu, _quitMenu });
        }
    }
}