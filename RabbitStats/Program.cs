using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using CommandLine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RabbitStats
{
    class Program
    {

        private static options Options = new options();
        private static string host;
        private static string port;
        private static string username;
        private static string password;

        static void Main(string[] args)
        {

            //Parse command line options and quit if invalid.
            if (Parser.Default.ParseArguments(args, Options))
            {
                host = Options.ActualHost.ToString();
                port = Options.ActualPort.ToString();
                username = Options.ActualUsername.ToString();
                password = Options.ActualPassword.ToString();

                using (WebClient wc = new WebClient())
                {
                    GetStats(wc);
                }
            }

        }

        private static void GetStats(WebClient wc)
        {
            string URI = "http://" + host + ":" + port + "/api/overview";
            string RabbitAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(username + ":" + password));

            wc.Headers.Add("User-Agent: RabbitStats");
            wc.Headers.Add("Host: " + host + ":" + port);
            wc.Headers.Add("WWW-Authenticate: Basic realm='RabbitMQ Management'");
            wc.Headers.Add("Authorization: Basic " + RabbitAuth);

            var output = new JObject();

            try
                {
                    var st = wc.OpenRead(URI);
                    var sr = new StreamReader(st);
                    string res = sr.ReadToEnd();

                    output = JObject.Parse(res);

                }
            catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    
                }
            
            Console.WriteLine(output);
        }

    }
}
