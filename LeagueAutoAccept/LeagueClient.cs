using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LeagueAutoAccept
{
    class LeagueClient
    {
        private static Regex TOKEN_REGEX = new Regex("\"--remoting-auth-token=(.+?)\"");
        private static Regex PORT_REGEX = new Regex("\"--app-port=(\\d+?)\"");

        public static Process[] getRiotClientProcesses()
        {
            return Process.GetProcessesByName("RiotClientUx");
        }

        public static Process[] getLeagueClientProcesses()
        {
            return Process.GetProcessesByName("LeagueClientUx");
        }

        public static Tuple<string, string> getAPIPortAndToken(Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                var commandLine = (string)objects.Cast<ManagementBaseObject>().SingleOrDefault()["CommandLine"];
                if (commandLine == null)
                {
                    var currentProcessInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        WorkingDirectory = Environment.CurrentDirectory,
                        FileName = Assembly.GetEntryAssembly().Location,
                        Verb = "runas"
                    };

                    Process.Start(currentProcessInfo);
                    Environment.Exit(0);
                }

                try
                {
                    var port = PORT_REGEX.Match(commandLine).Groups[1].Value;
                    var token = TOKEN_REGEX.Match(commandLine).Groups[1].Value;
                    return new Tuple<string, string>(port, token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }

        public static string sendAPIRequest(string port, string token, string method, string endpointUrl, string body)
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes("riot:" + token));
            ServicePointManager.ServerCertificateValidationCallback = (send, certificate, chain, sslPolicyErrors) => { return true; };
            using (var client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.Authorization, "Basic " + auth);
                try
                {
                    var result = client.UploadString("https://127.0.0.1:" + port + endpointUrl, method, body);
                    return result;
                }
                catch (WebException ex)
                {
                    return new StreamReader(ex.Response.GetResponseStream()).ReadToEnd();
                }
            }
        }
    }
}