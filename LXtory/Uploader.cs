using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;

namespace LXtory
{
    public static class Uploader
    {
        public static Action<int> ProgressBarUpdate { private get; set; }

        public static async Task<string> GetDropboxSharedUrl(string path)
        {
            try
            {
                string s;
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    JObject param = new JObject(
                        new JProperty("path", path));
                    w.Headers[HttpRequestHeader.Authorization] = $"Bearer {Properties.Settings.Default.dropboxToken}";
                    w.Headers[HttpRequestHeader.ContentType] = "application/json";
                    s = await w.UploadStringTaskAsync("https://api.dropboxapi.com/2/sharing/create_shared_link_with_settings", param.ToString(Formatting.None));
                }
                dynamic json = JsonConvert.DeserializeObject<ExpandoObject>(s);
                return json.url;
            }
            catch (Exception)
            {
                BalloonMessage.ShowMessage("Failed to get shared url", BalloonIcon.Warning);
                return "";
            }
        }

        public static async Task SetGoogleDriveFileShared(string fileID, CancellationToken token)
        {
            try
            {
                JObject permission = new JObject(
                    new JProperty("role", "reader"),
                    new JProperty("type", "anyone"));
                HttpContent content = new StringContent(permission.ToString(Formatting.None), Encoding.UTF8, "application/json");
                var headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {Properties.Settings.Default.gdriveToken}"
                };
                await UploadData($"https://www.googleapis.com/drive/v3/files/{fileID}/permissions", content, token, headers);

            }
            catch (Exception)
            {
                BalloonMessage.ShowMessage("Failed to set file shared", BalloonIcon.Warning);
            }
        }

        private static async Task<string> UploadData(string uri, HttpContent formData, CancellationToken token, Dictionary<string, string> extraHeaders = null)
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
                client.Timeout = Timeout.InfiniteTimeSpan;
                
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<string, string> header in extraHeaders)
                    {
                        client.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
                var response = await client.SendAsync(message, token);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    if (error.Length > 512)
                    {
                        error = "";
                    }
                    throw new Exception($"Upload error.\r\n{response.ReasonPhrase}\r\n{error}");
                }
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

        public static async Task<string> HttpGyazoUpload(XImage img, CancellationToken token)
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
                return await UploadData("http://upload.gyazo.com/api/upload", form, token);
            }
        }

        public static async Task<string> HttpDropboxUpload(XImage img, CancellationToken token)
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
            return await UploadData("https://content.dropboxapi.com/2/files/upload", content, token, headers);
        }

        public static async Task<string> HttpGoogleDriveUpload(XImage img, CancellationToken token)
        {
            JObject metadata = new JObject(
                new JProperty("name", img.filename),
                new JProperty("viewersCanCopyContent", true));
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(metadata.ToString(Formatting.None), Encoding.UTF8, "application/json"));
                form.Add(
                    img.image != null
                        ? new StreamContent(new MemoryStream(img.image))
                        : new StreamContent(new FileStream(img.filepath, FileMode.Open)));
                img.image = null;
                var headers = new Dictionary<string, string>
                {
                    ["Authorization"] = $"Bearer {Properties.Settings.Default.gdriveToken}"
                };
                return await UploadData("https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart", form, token, headers);
            }
        }

        public static async Task<string> HttpPuushUpload(XImage img, CancellationToken token)
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
                
                return await UploadData("https://puush.me/api/up", form, token);
            }
        }

        public static async Task<string> HttpImgurUpload(XImage img, bool noAccount, CancellationToken token)
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
                    ["Authorization"] = noAccount ? "Client-ID 83c1c8bf9f4d2b1" : $"Bearer {Properties.Settings.Default.accessToken}"
                };
                return await UploadData("https://api.imgur.com/3/image", form, token, extraHeaders);
            }
        }

        public static async Task<string> GetImgurAlbums()
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {Properties.Settings.Default.accessToken}");
                var response = await client.GetStringAsync($"https://api.imgur.com/3/account/{Properties.Settings.Default.username}/albums/");
                return response;
            }
        }

        public static async Task AddToImgurAlbum(string id)
        {
            if (string.IsNullOrEmpty(Properties.Settings.Default.imgurAlbumId))
                return;

            using (var w = new WebClient())
            {
                w.Proxy = null;
                JObject param = new JObject(
                    new JProperty("ids", id));
                w.Headers[HttpRequestHeader.Authorization] = $"Bearer {Properties.Settings.Default.accessToken}";
                w.Headers[HttpRequestHeader.ContentType] = "application/json";
                await w.UploadStringTaskAsync($"https://api.imgur.com/3/album/{Properties.Settings.Default.imgurAlbumId}/add", param.ToString(Formatting.None));
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

        public static string SFTPUpload(XImage img, ConnectionInfo ftpConnectionInfo, CancellationToken token)
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
                                    // Maybe works as cancel?
                                    if (token.IsCancellationRequested)
                                    {
                                        if (client != null) client.Disconnect();
                                    }
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
                                    // Maybe works as cancel?
                                    if (token.IsCancellationRequested)
                                    {
                                        if (client != null) client.Disconnect();
                                    }
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
