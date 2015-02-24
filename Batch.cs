using System;
using System.Diagnostics;
using System.Net;

using ManyConsole;

using Newtonsoft.Json.Linq;

namespace toutv
{
    public class Batch : ConsoleCommand
    {
        public string Show { get; set; }

        public Batch()
        {
            this.IsCommand("batch", "Batch downloads a TV Show");
            this.HasRequiredOption("show=", "TV Show to download", x => Show = x);
        }

        public override int Run(string[] remainingArguments)
        {
            var client = new WebClient();
            var showdata = JObject.Parse(client.DownloadString(string.Format("http://ici.tou.tv/presentation/{0}?excludeLineups=False&v=2&d=phone-android", Show)));

            foreach (var season in showdata["SeasonLineups"])
            {
                foreach (var episode in season["LineupItems"])
                {
                    Console.WriteLine("Getting {0} {1}", season["Title"].Value<string>(), episode["Title"].Value<string>());
                    
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "toutv",
                            Arguments = string.Format("fetch -m {0}", episode["Url"].Value<string>())
                        }
                    };
                    process.Start();
                    process.WaitForExit();
                }
            }

            return 0;
        }
    }
}
