using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

using ManyConsole;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace toutv
{
    public class Fetch : ConsoleCommand
    {
        public string MediaUrl { get; set; }

        private const string UrlGetClaims = "https://services.radio-canada.ca/media/validation/v2/GetClaims?token={0}";
        private const string UrlGetMediaMetadata = "http://ici.tou.tv/presentation/{0}?excludeLineups=True&v=2&d=phone-android";
        private const string UrlGetMediaPlaylist = "https://services.radio-canada.ca/media/validation/v2/?appCode=toutv&deviceType=androidcenc&connectionType=wifi&idMedia={0}&claims={1}&output=json&deviceId=8XV5T15A23003790";

        private const string UserAgent = "TouTvApp/2.4.0.3 (Huawei/angler/(Nexus/6P); 6.0.1/-/API23; en-us)";

        public Fetch()
        {
            this.IsCommand("fetch", "Download a media");
            this.HasRequiredOption("m=|media", "tou.tv slug to the media. Look at the tou.tv website Ex: infoman/S15E23", x => MediaUrl = x);
        }

        public override int Run(string[] remainingArguments)
        {
            // Get media metadata
            var stringData = Encoding.UTF8.GetString(new WebClient().DownloadData(string.Format(UrlGetMediaMetadata, MediaUrl)));
            var metaData = new MediaMetaData(JObject.Parse(stringData));
          
            // Get what would be the resulting output filename and check if the file already exists
            var outputFileName = GetOutputFileName(metaData);
            if (File.Exists(outputFileName))
            {
                Console.WriteLine("File \"{0}\" already exists. Skipping.");
                return 0;
            }

            // Read the access_token from the token file
            var accessToken = File.ReadAllText(".toutv_token");

            // Exhange the access_token for a user claims
            var request1 = WebRequest.CreateHttp(string.Format(UrlGetClaims, accessToken));
            request1.PreAuthenticate = true;
            request1.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;
            request1.Accept = "application/json";
            request1.UserAgent = UserAgent;
            request1.Host = "services.radio-canada.ca";

            var o = JObject.Parse(new StreamReader(request1.GetResponse().GetResponseStream()).ReadToEnd());
            var claim = o["claims"].Value<string>();

            // Get the media URL
            var request2 = WebRequest.CreateHttp(string.Format(UrlGetMediaPlaylist, metaData.MediaId, claim));
            request2.PreAuthenticate = true;
            request2.Headers[HttpRequestHeader.Authorization] = "Bearer " + accessToken;
            request2.UserAgent = UserAgent;
            request2.Host = "services.radio-canada.ca";

            o = JObject.Parse(new StreamReader(request2.GetResponse().GetResponseStream()).ReadToEnd());
            var url = o["url"].Value<string>();

            // Start a ffmpeg process downloading the file
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = string.Format("-i \"{0}\" -movflags faststart -sn -c copy \"{1}\"", url, outputFileName)
                }
            };

            try
            {
                process.Start();
            }
            catch (Win32Exception ex)
            {
                Console.WriteLine("[ERROR] Could not find ffmpeg installed on your system.");
                return 0;
            }

            process.WaitForExit();

            return 0;
        }

        private static string GetOutputFileName(MediaMetaData metaData)
        {
            var outputfilename = string.Format("{0} {1}{2}", metaData.Title, metaData.Saison, metaData.Episode).Trim();

            foreach (var c in Path.GetInvalidFileNameChars())
                outputfilename = outputfilename.Replace(c, '_');

            return outputfilename + ".mp4";
        }
    }

    internal class MediaMetaData
    {
        public string MediaId { get; set; }
        public string Title { get; set; }
        public string Saison { get; set; }
        public string Episode { get; set; }

        public MediaMetaData(JObject json)
        {
            MediaId = json["IdMedia"].Value<string>();
            Title = json["Title"].Value<string>();
            Saison = json["StatsMetas"]["rc.saison"].Value<string>();
            Episode = json["StatsMetas"]["rc.episode"].Value<string>();
        }
    }
}
