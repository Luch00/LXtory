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
using System.Net.Http.Headers;

namespace ScreenShotterWPF
{
    public class Uploader
    {
        // For imgur
        private const string ClientID = "83c1c8bf9f4d2b1";
        private const string ClientSecret = "33dac3b1adfcefab926d83f1cb21412cf32ee36a";
        // For Gyazo
        private const string GyazoClientID = "f6f7ea4ac48869d64d585050fb041a9a85b28f531a1a43833028f75a0a3a6183";
        private const string GyazoClientSecret = "e78f75312829d3e6c6816c35e07cd6a34efa908260d47bf4ad622531c26f6bee";
        
        // Actions to main form
        private static Action<int> progressBarUpdate;

        public Uploader(Action<int> p)
        {
            progressBarUpdate = p;
        }

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
                string s = "";
                using (var w = new WebClient())
                {
                    w.Proxy = null;
                    NameValueCollection v = new NameValueCollection();
                    v.Add("client_id", GyazoClientID);
                    //v.Add("client_secret", GyazoClientSecret);
                    v.Add("redirect_uri", "http://localhost:8080/LXtory_Auth/");
                    v.Add("code", code);
                    v.Add("grant_type", "authorization_code");
                    v.Add("scope", "public");
                    var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{GyazoClientID}:{GyazoClientSecret}"));
                    w.Headers[HttpRequestHeader.Authorization] = $"Basic {base64}";

                    var response = await w.UploadValuesTaskAsync("http://api.gyazo.com/oauth/token", "POST", v);

                    //w.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    //string parameters = $"client_id={GyazoClientID}&client_secret={GyazoClientSecret}&code={code}&grant_type=authorization_code&access_token=0f90443b5477ae0f2668f057b7686c65ede778512fef6d276f09ecbcd2d1fa54";
                    //var response = w.UploadString("https://api.gyazo.com/oauth/token", parameters);
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
                    w.UploadValuesCompleted += new UploadValuesCompletedEventHandler(webClient_TokenRefreshCompleted);
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

        

        private static async Task<string> UploadData(string uri, string boundary, MultipartFormDataContent formData, Dictionary<HttpRequestHeader, string> extraHeaders = null )
        {
            using (var wc = new NotAliveWebClient())
            {
                wc.Proxy = null;
                wc.UploadProgressChanged += Wc_UploadProgressChanged;
                wc.Headers.Add(HttpRequestHeader.ContentType, $"multipart/form-data; boundary={boundary}");
                wc.Headers.Add(HttpRequestHeader.UserAgent, "LXtory/1.0");
                if (extraHeaders != null)
                {
                    foreach (KeyValuePair<HttpRequestHeader, string> header in extraHeaders)
                    {
                        wc.Headers.Add(header.Key, header.Value);
                    }
                }
                byte[] formBytes = formData.ReadAsByteArrayAsync().Result;
                formData.Dispose();

                byte[] response = await wc.UploadDataTaskAsync(uri, formBytes);
                return Encoding.UTF8.GetString(response);
            }
        }

        private static void Wc_UploadProgressChanged(object sender, UploadProgressChangedEventArgs e)
        {
            int progress = e.ProgressPercentage <= 50 ? e.ProgressPercentage * 2 : e.ProgressPercentage;
            //Console.WriteLine($"{e.BytesSent} / {e.TotalBytesToSend} -- {e.ProgressPercentage}");
            progressBarUpdate(progress);
        }

        private static string GetFileContentType(string file)
        {
            string ext = Path.GetExtension(file)?.ToLower() ?? ".png";
            switch (ext)
            {
                default:
                case "":
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".bmp":
                    return "image/bmp";
                case ".gif":
                    return "image/gif";
            }
        }

        public static async Task<string> HttpGyazoUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(Properties.Settings.Default.gyazoToken), "access_token");
                form.Headers.ContentType = new MediaTypeHeaderValue(GetFileContentType(img.filepath));
                form.Add(new ByteArrayContent(img.image, 0, img.image.Length), "imagedata", img.filename);
                img.image = null;

                return await UploadData("http://upload.gyazo.com/api/upload", boundary, form);
            }
        }

        public static async Task<string> HttpPuushUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(Properties.Settings.Default.puush_key), "k");
                form.Add(new StringContent("LXtory"), "z");
                form.Headers.ContentType = new MediaTypeHeaderValue(GetFileContentType(img.filepath));
                form.Add(new ByteArrayContent(img.image, 0, img.image.Length), "f", img.filename);
                img.image = null;

                return await UploadData("https://puush.me/api/up", boundary, form);
            }
        }

        public static async Task<string> HttpImgurUpload(XImage img)
        {
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            using (var form = new MultipartFormDataContent(boundary))
            {
                form.Add(new StringContent(Properties.Settings.Default.puush_key), "title");
                form.Headers.ContentType = new MediaTypeHeaderValue(GetFileContentType(img.filepath));
                form.Add(new ByteArrayContent(img.image, 0, img.image.Length), "image", img.filename);

                img.image = null;
                var extraHeaders = new Dictionary<HttpRequestHeader, string>
                {
                    [HttpRequestHeader.Authorization] = img.anonupload ? "Client-ID 83c1c8bf9f4d2b1" : $"Bearer {Properties.Settings.Default.accessToken}"
                };
                return await UploadData("https://api.imgur.com/3/image", boundary, form, extraHeaders);
            }
        }

        private class NotAliveWebClient : WebClient
        {
            protected override WebRequest GetWebRequest(Uri address)
            {
                WebRequest request = base.GetWebRequest(address);
                if (request is HttpWebRequest)
                {
                    (request as HttpWebRequest).KeepAlive = false;
                    (request as HttpWebRequest).AllowWriteStreamBuffering = false;
                }
                return request;
            }
        }

        //private static Exception ExceptionStatus(WebException e)
        //{
        //    HttpWebResponse r = (HttpWebResponse)e.Response;
        //    if (r.StatusCode == HttpStatusCode.Forbidden)
        //    {
        //        return new Exception("Error: Server returned code 403", e);
        //    }
        //    if (r.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        //    {
        //        return new Exception("Error: Server returned code 413", e);
        //    }
        //    if (r.StatusCode == HttpStatusCode.BadRequest)
        //    {
        //        return new Exception("Error: Server returned code 400", e);
        //    }
        //    return new Exception(e.Message);
        //}

        // NYI
        private void SFTPUpload(XImage img)
        {
            using (var client = new Renci.SshNet.SftpClient("host", 22, "username", "password"))
            {
                try
                {
                    client.ConnectionInfo.Timeout = new TimeSpan(0, 0, 30);
                    client.Connect();
                    long fileSize = img.image.Length;
                    client.UploadFile(new MemoryStream(img.image), "/upload/" + img.filename,
                        bytesUploaded =>
                        {
                            //int percent = (int)(((double)bytesUploaded / fileSize) * 100.0);
                            //ProgressUpdate(bytesUploaded, fileSize);
                        });
                }
                catch (Exception)
                {

                    throw;
                }
            }
        }

        // TRY DIS ON WIN10!!
        //private async Task<bool> HttpUpload(XImage img)
        /*private async void HttpUpload(XImage img)
        {
            ProgressMessageHandler progress = new ProgressMessageHandler();
            progress.HttpSendProgress += new EventHandler<HttpProgressEventArgs>(HttpSendProgress);
            //using (var client = new HttpClient(progress))
            using (var client = HttpClientFactory.Create(progress))
            {
                using (var content = new MultipartFormDataContent("Upload----" + DateTime.Now.ToString(CultureInfo.InvariantCulture)))
                {
                    if (img.anonupload)
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Client-ID 83c1c8bf9f4d2b1");
                    }
                    else
                    {
                        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + Properties.Settings.Default.accessToken);
                    }
                    client.DefaultRequestHeaders.TransferEncodingChunked = true;
                    string fileContentType = "image/png";

                    string ext = Path.GetExtension(img.filepath).ToLower();
                    if (ext == "" || ext == ".png")
                    {
                        fileContentType = "image/png";
                    }
                    if (ext == ".jpg" || ext == ".jpeg")
                    {
                        fileContentType = "image/jpeg";
                    }
                    if (ext == ".bmp")
                    {
                        fileContentType = "image/bmp";
                    }
                    if (ext == ".gif")
                    {
                        fileContentType = "image/gif";
                    }

                    var values = new[]
                    {
                        new  KeyValuePair<string, string>("name", img.filename),
                        new  KeyValuePair<string, string>("title", img.filename)
                    };

                    foreach (var vp in values)
                    {
                        content.Add(new StringContent(vp.Value), String.Format("\"{0}\"", vp.Key));
                    }

                    var imageContent = new ByteArrayContent(img.image);
                    imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse(fileContentType);
                    content.Add(imageContent, "image", img.filename);
                    
                    try
                    {
                        var message = await client.PostAsync("https://api.imgur.com/3/image", content);
                        if (message.IsSuccessStatusCode)
                        {
                            var response = message.Content.ReadAsStringAsync();
                            dynamic json = JObject.Parse(response.Result);
                            img.image = null;
                            string link = json.data.link;
                            addXImageToList(img, link);
                            //return true;
                        }
                        else
                        {
                            Console.WriteLine("Other Errors happened");
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Errors happened: " + e.Message);
                        //throw;
                    }
                }
            }
            return false;
        }*/

        /*private void HttpSendProgress(object sender, HttpProgressEventArgs e)
        {
            HttpRequestMessage request = sender as HttpRequestMessage;
            Console.WriteLine("%: " + e.BytesTransferred);
            progressBarUpdate(e.ProgressPercentage);
        }*/
    }
}
