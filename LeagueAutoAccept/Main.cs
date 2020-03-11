using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.UI.Notifications;
using LeagueAutoAccept.Properties;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using WebSocketSharp;

namespace LeagueAutoAccept
{
    internal class Main : ApplicationContext
    {
        private readonly NotifyIcon _notifyIcon;
        private bool _enabled = true;
        private bool _noLcuRunning = true;
        private bool _runOnStartup;
        private readonly RegistryKey _regStartUp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public Main()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = Resources.Icon,
                Visible = true
            };
            NotifyMenu();
            StartCheckingForLcuStart();
            if (_regStartUp?.GetValue("LeagueAutoAccept") != null) _runOnStartup = true;
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

                    var result = LeagueClient.SendApiRequest(apiAuth, "POST", "/lol-matchmaking/v1/ready-check/accept", "");
                    Debug.WriteLine(result);
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
            _notifyIcon.ContextMenu = new ContextMenu();
            _notifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Title) {Enabled = false});
            _notifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Enabled, (a, e) =>
            {
                _enabled = !_enabled;
                NotifyMenu();
            })
            {
                Checked = _enabled
            });
            _notifyIcon.ContextMenu.MenuItems.Add(new MenuItem("Run on startup", (a, e) =>
            {
                _runOnStartup = !_runOnStartup;
                if (_runOnStartup)
                {
                    _regStartUp?.SetValue("LeagueAutoAccept", Application.ExecutablePath);
                }
                else
                {
                    _regStartUp?.DeleteValue("LeagueAutoAccept", false);
                }
                NotifyMenu();
            })
            {
                Checked = _runOnStartup
            });
            _notifyIcon.ContextMenu.MenuItems.Add(new MenuItem(Resources.Quit, (a, e) => Application.Exit()));
        }

        public static void ShowToast(string content)
        {
            var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            var stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(Resources.Title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(content));
            ToastNotificationManager.CreateToastNotifier("LeagueAutoAccept").Show(new ToastNotification(toastXml));
        }
    }
}