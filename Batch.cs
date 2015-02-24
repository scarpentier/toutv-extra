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
        public string Season { get; set; }

        public Batch()
        {
            this.IsCommand("batch", "Batch downloads a TV Show");
            this.HasRequiredOption("s=|show", "TV Show to download", x => Show = x);
            this.HasOption("season=", "Limits the batch download to a single season. Use the 'season name'", x => Season = x);
        }

        public override int Run(string[] remainingArguments)
        {
            var client = new WebClient();
            var showdata = JObject.Parse(client.DownloadString(string.Format("http://ici.tou.tv/presentation/{0}?excludeLineups=False&v=2&d=phone-android", Show)));

            foreach (var season in showdata["SeasonLineups"])
            {
                var seasonName = season["Name"].Value<string>();
                
                // Only download specified season if present
                if (!string.IsNullOrEmpty(Season) && seasonName != Season)
                    continue;

                Console.WriteLine("Downloading season name: \"{0}\"", seasonName);

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
