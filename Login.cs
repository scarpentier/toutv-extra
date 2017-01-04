using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

using ManyConsole;

using Newtonsoft.Json;

namespace toutv
{
    public class Login: ConsoleCommand
    {
        private string UserEmail { get; set; }
        private string UserPassword { get; set; }

        private const string UrlLoginPost = "https://services.radio-canada.ca/auth/oauth/v2/authorize";
        private const string UrlLoginGet = "https://services.radio-canada.ca/auth/oauth/v2/authorize?response_type=token&client_id={0}&scope=oob+openid+profile+email+id.write+media-validation.read.privileged&state=authCode&redirect_uri=http://ici.tou.tv/profiling/callback";
        private const string ClientId = "d6f8e3b1-1f48-45d7-9e28-a25c4c514c60";

        private const string RegexSession = "name=\"sessionID\" value=\"([^\"]*)\"";

        private const string PostString = "sessionID={0}&action=login&client_id={1}&client-domain=icitv&client-platform=android&login-email={2}&login-password={3}&form-submit-btn=Ouvrir%20une%20session";

        private const string UserAgent = "Mozilla/5.0 (Linux; Android 4.4.4; SGH-I337M Build/KTU84Q) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/33.0.0.0 Mobile Safari/537.36";

        public Login()
        {
            this.IsCommand("login", "Login to the tou.tv service and writes the resulting access token on disk");
            this.HasRequiredOption("u=|user", "User email", x => UserEmail = x);
            this.HasRequiredOption("p=|pass", "User password", x => UserPassword = x);            
        }

        public override int Run(string[] remainingArguments)
        {
            // GET the login page from an empty state
            var request1 = WebRequest.CreateHttp(string.Format(UrlLoginGet, ClientId));
            request1.Headers["Origin"] = "https://services.radio-canada.ca";
            request1.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request1.UserAgent = UserAgent;
            request1.Headers.Add("X-Requested-With", "tv.tou.android");
            var response1 = request1.GetResponse() as HttpWebResponse;

            var response1Data = new StreamReader(response1.GetResponseStream()).ReadToEnd();

            // Grab the SessionID from the response
            var sessionId = new Regex(RegexSession).Match(response1Data).Groups[1].Value;
            Console.WriteLine("SessionId: {0}", sessionId);

            // Actually authenticate against the service and get the user token
            var postdata = Encoding.Default.GetBytes(string.Format(
                PostString,
                sessionId,
                ClientId,
                WebUtility.UrlEncode(UserEmail),
                WebUtility.UrlEncode(UserPassword)));

            var request = WebRequest.CreateHttp(UrlLoginPost);
            request.Method = WebRequestMethods.Http.Post;
            request.AllowAutoRedirect = false;
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.Headers.Add("Origin", "https://services.radio-canada.ca");
            request.Headers.Add("X-Requested-With", "tv.tou.android");
            request.Referer = string.Format(UrlLoginGet, ClientId);
            request.ContentType = "application/x-www-form-urlencoded";
                        
            var dataStream = request.GetRequestStream();
            dataStream.Write(postdata, 0, postdata.Length);
            dataStream.Close();

            var webResponse = request.GetResponse() as HttpWebResponse;
            var webResponseText = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
            var token = new Regex("access_token=([^&]*)").Match(webResponse.Headers["Location"]).Groups[1].Value;
            Console.WriteLine("access_token: {0}", token);
            File.WriteAllText(".toutv_token", token);
            Console.WriteLine("Token written on disk");

            return 0;
        }
    }
}
