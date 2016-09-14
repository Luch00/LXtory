using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;

namespace ScreenShotterWPF
{
    public static class Uploader
    {
        // For imgur
        private const string ClientID = "";
        private const string ClientSecret = "";
        // For Gyazo
        private const string GyazoClientID = "";
        private const string GyazoClientSecret = "";
        // For Dropbox
        private const string DropboxClientID = "";
        private const string DropboxClientSecret = "";

        // Actions to main form

        public static Action<int> ProgressBarUpdate { private get; set; }

        // Use pin to get auth token
        /*public void GetToken(string pin)
        {
            string s = "";
            using (var w = new WebClient())
            {
                w.Proxy = null;
                NameValueCollection v = new NameValueCollection();
                v.Add("client_id", ClientID);
                v.Add("client_secret", ClientSecret);
                v.Add("grant_type", "pin");
                v.Add("pin", pin);
                byte[] response = w.UploadValues("https://api.imgur.com/oauth2/token", v);
                s = Encoding.UTF8.GetString(response, 0, response.Length);
            }
            dynamic json = JObject.Parse(s);
            Properties.Settings.Default.accessToken = json.access_token;
            Properties.Settings.Default.refreshToken = json.refresh_token;
            Properties.Settings.Default.username = json.account_username;
            Console.WriteLine(s);
            GetTokenInfo();
        }*/

        public static async Task GetToken(string code)
        {
            try
            {
                string s;
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    NameValueCollection v = new NameValueCollection();
                    v.Add("client_id", ClientID);
                    v.Add("client_secret", ClientSecret);
                    v.Add("grant_type", "authorization_code");
                    v.Add("code", code);
                    var response = await w.UploadValuesTaskAsync("https://api.imgur.com/oauth2/token", v);
                    s = Encoding.UTF8.GetString(response, 0, response.Length);
                }
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(s);
                Properties.Settings.Default.accessToken = json["access_token"];
                Properties.Settings.Default.refreshToken = json["refresh_token"];
                Properties.Settings.Default.username = json["account_username"];
                Properties.Settings.Default.imgurTokenExpire = json["expires_in"];
                Properties.Settings.Default.lastRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error While fetching tokens", e);
            }
        }

        public static async Task GetGyazoToken(string code)
        {
            try
            {
                string s = string.Empty;
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    NameValueCollection v = new NameValueCollection();
                    v.Add("client_id", GyazoClientID);
                    v.Add("redirect_uri", "http://localhost:8080/LXtory_Auth/");
                    v.Add("code", code);
                    v.Add("grant_type", "authorization_code");
                    v.Add("scope", "public");
                    var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{GyazoClientID}:{GyazoClientSecret}"));
                    w.Headers[HttpRequestHeader.Authorization] = $"Basic {base64}";

                    var response = await w.UploadValuesTaskAsync("http://api.gyazo.com/oauth/token", "POST", v);
                    s = Encoding.UTF8.GetString(response, 0, response.Length);
                }
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(s);
                Properties.Settings.Default.gyazoToken = json["access_token"];
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error while fetching tokens", e);
            }
        }

        public static async Task GetDropboxToken(string code)
        {
            try
            {
                string s = string.Empty;
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    NameValueCollection v = new NameValueCollection();
                    v.Add("client_id", DropboxClientID);
                    v.Add("client_secret", DropboxClientSecret);
                    v.Add("code", code);
                    v.Add("grant_type", "authorization_code");
                    v.Add("redirect_uri", "http://localhost:8080/LXtory_Auth/");
                    var response = await w.UploadValuesTaskAsync("https://api.dropboxapi.com/oauth2/token", v);
                    s = Encoding.UTF8.GetString(response, 0, response.Length);
                }
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(s);
                Properties.Settings.Default.dropboxToken = json["access_token"];
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error while fetching tokens", e);
            }
        }

        public static async Task<string> GetDropboxSharedUrl(string path)
        {
            try
            {
                string s = string.Empty;
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    JObject param = new JObject(
                        new JProperty("path", path));
                    w.Headers[HttpRequestHeader.Authorization] = $"Bearer {Properties.Settings.Default.dropboxToken}";
                    w.Headers[HttpRequestHeader.ContentType] = "application/json";
                    s = await w.UploadStringTaskAsync("https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings", param.ToString(Formatting.None));
                }
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(s);
                var url = json["url"];
                return url;
            }
            catch (Exception)
            {
                BalloonMessage.ShowMessage("Failed to get shared url", BalloonIcon.Warning);
                return "";
                //throw;
            }
        }

        // Get Albums TODO MAYBE
        /*public dynamic GetAlbums()
        {
            string s;
            using (var w = new WebClient())
            {
                w.Proxy = null;
                w.Headers.Add("Authorization", "Bearer " + Properties.Settings.Default.accessToken);
                Stream stream = w.OpenRead("https://api.imgur.com/3/account/" + Properties.Settings.Default.username + "/albums");
                StreamReader reader = new StreamReader(stream);
                s = reader.ReadToEnd();
            }
            Console.WriteLine(s);
            return s;
        }*/

        // Refresh imgur auth token
        public static async Task RefreshToken()
        {
            if (Properties.Settings.Default.refreshToken != string.Empty)
            {
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    w.UploadValuesCompleted += webClient_TokenRefreshCompleted;
                    NameValueCollection v = new NameValueCollection();
                    v.Add("refresh_token", Properties.Settings.Default.refreshToken);
                    v.Add("client_id", ClientID);
                    v.Add("client_secret", ClientSecret);
                    v.Add("grant_type", "refresh_token");
                    await w.UploadValuesTaskAsync(new Uri("https://api.imgur.com/oauth2/token"), v);
                }
            }
        }

        // Event to run after auth token is refreshed
        private static void webClient_TokenRefreshCompleted(object sender, UploadValuesCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                var v = e.Error as WebException;
                if (v != null && v.Status == WebExceptionStatus.NameResolutionFailure)
                {
                    throw new Exception("NameResolutionFailure", e.Error);
                    //return;
                }
                if (v != null && v.Status == WebExceptionStatus.UnknownError)
                {
                    throw new Exception("UnknownError", e.Error);
                    //return;
                }
                throw new Exception("Error refreshing Imgur tokens.", e.Error);
            }
            var s = Encoding.UTF8.GetString(e.Result, 0, e.Result.Length);
            try
            {
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(s);
                Properties.Settings.Default.accessToken = json["access_token"];
                Properties.Settings.Default.refreshToken = json["refresh_token"];
                Properties.Settings.Default.imgurTokenExpire = json["expires_in"];
                Properties.Settings.Default.lastRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
            catch (Exception ex)
            {
                throw new Exception("Error happened while parsing tokens", ex);
            }
        }

        private static async Task<string> UploadData(string uri, HttpContent formData, Dictionary<string, string> extraHeaders = null)
        {
            ProgressMessageHandler progress = new ProgressMessageHandler();
            progress.HttpSendProgress += HttpSendProgress;

            HttpRequestMessage message = new HttpRequestMessage();
            
            message.Method = HttpMethod.Post;
            message.Content = formData;
            message.RequestUri = new Uri(uri);

            using (var client = HttpClientFactory.Create(progress))
            {
                client.DefaultRequestHeaders.Add("User-Agent", "LXtory/1.0");
                client.DefaultRequestHeaders.Add("Connection", "Close");
                
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> header in extraHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var response = await client.SendAsync(message);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    Console.WriteLine(error);
                }
                throw new Exception($"Upload error.\n{response.ReasonPhrase}");
            }
        }

        private static void HttpSendProgress(object sender, HttpProgressEventArgs e)
        {
            ProgressBarUpdate(e.ProgressPercentage);
        }

        private static void Wc_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage <= 50 ? e.ProgressPercentage * 2 : e.ProgressPercentage;
            ProgressBarUpdate(progress);
        }

        public static async Task<string> HttpGyazoUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(Properties.Settings.Default.gyazoToken), "access_token");
                form.Add(
                    img.image != null
                        ? new StreamContent(new MemoryStream(img.image))
                        : new StreamContent(new FileStream(img.filepath, FileMode.Open)), "imagedata", img.filename);
                img.image = null;
                return await UploadData("http://upload.gyazo.com/api/upload", form);
            }
        }

        public static async Task<string> HttpDropboxUpload(XImage img)
        {
            HttpContent content = img.image != null ? new StreamContent(new MemoryStream(img.image)) : new StreamContent(new FileStream(img.filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            JObject args = new JObject(
                new JProperty("path", $"{Properties.Settings.Default.dropboxPath}{img.filename}"),
                new JProperty("mode", "add"),
                new JProperty("autorename", true),
                new JProperty("mute", false));
            Console.WriteLine(args.ToString(Formatting.None));
            content.Headers.Add("Content-Type", "application/octet-stream");
            content.Headers.Add("Dropbox-API-Arg", args.ToString(Formatting.None));
            var headers = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {Properties.Settings.Default.dropboxToken}"
            };
            img.image = null;
            return await UploadData("https://content.dropboxapi.com/2/files/upload", content, headers);
        }

        public static async Task<string> HttpPuushUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(Properties.Settings.Default.puush_key), "k");
                form.Add(new StringContent("LXtory"), "z");
                form.Add(
                    img.image != null
                        ? new StreamContent(new MemoryStream(img.image))
                        : new StreamContent(new FileStream(img.filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)), "f", img.filename);
                img.image = null;
                
                return await UploadData("https://puush.me/api/up", form);
            }
        }

        public static async Task<string> HttpImgurUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(img.filename), "title");
                form.Add(
                    img.image != null
                        ? new StreamContent(new MemoryStream(img.image))
                        : new StreamContent(new FileStream(img.filepath, FileMode.Open)), "image", img.filename);
                img.image = null;

                var extraHeaders = new Dictionary<string, string>
                {
                    ["Authorization"] = img.anonupload ? "Client-ID 83c1c8bf9f4d2b1" : $"Bearer {Properties.Settings.Default.accessToken}"
                };
                return await UploadData("https://api.imgur.com/3/image", form, extraHeaders);
            }
        }

        public static async Task<string> FTPUpload(XImage img)
        {
            using (var wc = new WebClient())
            {
                wc.Proxy = null;
                wc.UploadProgressChanged += Wc_UploadProgressChanged;
                wc.Credentials = new NetworkCredential(Properties.Settings.Default.ftpUsername, Properties.Settings.Default.ftpPassword);
                //wc.BaseAddress = $"ftp://{Properties.Settings.Default.ftpHost}:{Properties.Settings.Default.ftpPort}/{Properties.Settings.Default.ftpPath}";
                wc.BaseAddress = $"ftp://{Properties.Settings.Default.ftpHost}:{Properties.Settings.Default.ftpPort}";
                string path = $"/{Properties.Settings.Default.ftpPath}{(Properties.Settings.Default.ftpPath == string.Empty ? "" : "/")}{img.filename}";
                if (img.image != null)
                {
                    await wc.UploadDataTaskAsync(path, img.image);
                }
                else
                {
                    await wc.UploadFileTaskAsync(path, img.filepath);
                }
                img.image = null;

                // return some kind of url
                return $"http://{Properties.Settings.Default.ftpHost}{path}";
            }
        }

        public static string SFTPUpload(XImage img, ConnectionInfo ftpConnectionInfo)
        {
            if (ftpConnectionInfo == null)
            {
                throw new Exception("Invalid FTP connection information.");
            }
            using (var client = new SftpClient(ftpConnectionInfo))
            {
                try
                {
                    client.ConnectionInfo.Timeout = new TimeSpan(0, 0, 30);
                    client.Connect();
                    long fileSize = img.image.Length;
                    string path = $"/{Properties.Settings.Default.ftpPath}{(Properties.Settings.Default.ftpPath == string.Empty ? "" : "/")}{img.filename}";
                    if (img.image != null)
                    {
                        using (var stream = new MemoryStream(img.image))
                        {
                            client.UploadFile(stream, path,
                                bytesUploaded =>
                                {
                                    int percent = (int)(((double)bytesUploaded / fileSize) * 100.0);
                                    ProgressBarUpdate(percent);
                                });
                        }
                        img.image = null;
                    }
                    else
                    {
                        using (var stream = new FileStream(img.filepath, FileMode.Open))
                        {
                            client.UploadFile(stream, path,
                                bytesUploaded =>
                                {
                                    int percent = (int)(((double)bytesUploaded / fileSize) * 100.0);
                                    ProgressBarUpdate(percent);
                                });
                        }
                    }
                    
                    // return some kind of url
                    return $"http://{Properties.Settings.Default.ftpHost}{path}";
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }
    }
}
