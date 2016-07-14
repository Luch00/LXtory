using System;
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
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

        // Webclient for uploading
        //WebClient w;
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
                    //v.Add("redirect_uri", "");
                    v.Add("code", code);
                    v.Add("grant_type", "authorization_code");
                    v.Add("scope", "public");
                    var base64 = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{GyazoClientID}:{GyazoClientSecret}"));
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

        // Refresh auth token
        public async Task RefreshToken()
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
                    // maybe should have a log file?
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

        // Get the id used by default Gyazowin application if previously used on this PC
        private static void TryGetDefaultGyazoID()
        {
            string gyazoDefaulPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Gyazo\id.txt");
            if (File.Exists(gyazoDefaulPath))
            {
                string id = File.ReadAllText(gyazoDefaulPath);
                if (id.Length > 0)
                {
                    Properties.Settings.Default.gyazo_id = id;
                    Properties.Settings.Default.Save();
                }
            }
        }

        public Tuple<bool, string> HttpGyazoWebRequestUpload(XImage img)
        {
            if (Properties.Settings.Default.gyazo_id == string.Empty)
            {
                TryGetDefaultGyazoID();
            }
            WebRequestState reqState = null;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(new Uri("https://upload.gyazo.com/upload.cgi"));

            webrequest.Proxy = null;

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

            webrequest.Host = "upload.gyazo.com";
            // Random string to be used as a boundary
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";
            webrequest.KeepAlive = false;
            webrequest.Timeout = 300000;
            // build the body of the response
            StringBuilder sb = new StringBuilder();
            // Boundary, needs to be added between each block. Always add -- to it
            sb.AppendFormat("--{0}\r\n", boundary);
            // Title field
            sb.AppendFormat("Content-Disposition: form-data; name=\"id\";\r\n\r\n{0}\r\n", Properties.Settings.Default.gyazo_id);

            sb.AppendFormat("--{0}\r\n", boundary);
            // filename of the file to upload
            sb.AppendFormat("Content-Disposition: form-data; name=\"imagedata\"; filename=\"{0}\"\r\n", img.filename);
            // Change this according to file ext
            sb.AppendFormat("Content-Type: {0}\r\n\r\n", fileContentType);
            // file comes here
            // Get the bytes for the body
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
            // This is the last part, add -- before and after boundary
            byte[] footer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // Length of the whole thing
            webrequest.ContentLength = body.Length + img.image.Length + footer.Length;

            reqState = new HttpWebRequestState(body.Length + img.image.Length + footer.Length);
            reqState.request = webrequest;

            Buffer.BlockCopy(body, 0, reqState.bufferWrite, 0, body.Length);
            Buffer.BlockCopy(img.image, 0, reqState.bufferWrite, body.Length, img.image.Length);
            Buffer.BlockCopy(footer, 0, reqState.bufferWrite, body.Length + img.image.Length, footer.Length);
            //currentUpload.image = null;
            img.image = null;
            reqState.totalBytes = webrequest.ContentLength;
            reqState.fileURI = new Uri("https://upload.gyazo.com/upload.cgi");
            reqState.transferStart = DateTime.Now;
            reqState.buffer_size = 4096;
            webrequest.UserAgent = "LXtory/1.0";
            Stream requestStream = webrequest.GetRequestStream();
            reqState.streamResponse = requestStream;

            try
            {
                while (reqState.bytesWritten < reqState.totalBytes)
                {
                    if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                    {
                        reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                    }
                    requestStream.Write(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size);
                    reqState.bytesWritten += reqState.buffer_size;
                    ProgressUpdate(reqState.bytesWritten, reqState.totalBytes);
                }
                requestStream.Close();
                reqState.streamResponse.Close();
                WebResponse wr = webrequest.GetResponse();
                if (Properties.Settings.Default.gyazo_id == string.Empty && wr.Headers["X-Gyazo-Id"] != null)
                {
                    Properties.Settings.Default.gyazo_id = wr.Headers["X-Gyazo-Id"];
                    Properties.Settings.Default.Save();
                }
                Stream responseStream = wr.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string response = reader.ReadToEnd();

                reader.Close();
                progressBarUpdate(100);

                if (response.Length > 0)
                {
                    return new Tuple<bool, string>(true, response);
                }
                return new Tuple<bool, string>(false, "Failed :(");
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    throw ExceptionStatus((WebException)e);
                }
                throw new Exception("Error while uploading to Gyazo.", e);
            }
        }


        public Tuple<bool, string> HttpGyazoWebRequestUpload2(XImage img)
        {
            WebRequestState reqState = null;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(new Uri("http://upload.gyazo.com/api/upload"));

            webrequest.Proxy = null;

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
            
            webrequest.Host = "upload.gyazo.com";
            webrequest.Method = "POST";
            webrequest.KeepAlive = false;
            webrequest.Timeout = 300000;
            // Random string to be used as a boundary
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            // 
            MultipartFormDataContent form = new MultipartFormDataContent(boundary);
            form.Add(new StringContent(Properties.Settings.Default.gyazoToken), "access_token");
            form.Add(new ByteArrayContent(img.image, 0, img.image.Length), "imagedata", img.filename);
            form.Headers.ContentType = new MediaTypeHeaderValue(fileContentType);

            byte[] formData = form.ReadAsByteArrayAsync().Result;
            form.Dispose();
            // Length of the whole thing
            webrequest.ContentLength = formData.Length;
            
            reqState = new HttpWebRequestState(formData.Length);
            reqState.request = webrequest;
            
            reqState.bufferWrite = formData;
            img.image = null;
            reqState.totalBytes = webrequest.ContentLength;
            reqState.fileURI = new Uri("http://upload.gyazo.com/api/upload");
            reqState.transferStart = DateTime.Now;
            reqState.buffer_size = 4096;
            webrequest.UserAgent = "LXtory/1.0";
            Stream requestStream = webrequest.GetRequestStream();
            reqState.streamResponse = requestStream;

            try
            {
                while (reqState.bytesWritten < reqState.totalBytes)
                {
                    if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                    {
                        reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                    }
                    requestStream.Write(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size);
                    reqState.bytesWritten += reqState.buffer_size;
                    ProgressUpdate(reqState.bytesWritten, reqState.totalBytes);
                }
                requestStream.Close();
                reqState.streamResponse.Close();
                WebResponse wr = webrequest.GetResponse();

                Stream responseStream = wr.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string response = reader.ReadToEnd();

                reader.Close();
                progressBarUpdate(100);
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(response);
                string link = json["url"];

                if (link.Length > 0)
                {
                    return new Tuple<bool, string>(true, link);
                }
                return new Tuple<bool, string>(false, "Failed :(");
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    throw ExceptionStatus((WebException)e);
                }
                throw new Exception("Error while uploading to Gyazo.", e);
            }
        }

        public Tuple<bool, string> PuushHttpWebRequestUpload(XImage img)
        {
            WebRequestState reqState = null;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(new Uri("https://puush.me/api/up"));

            webrequest.Proxy = null;

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

            webrequest.Host = "puush.me";
            // Random string to be used as a boundary
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";
            webrequest.KeepAlive = false;
            webrequest.Timeout = 300000;

            // build the body of the response
            StringBuilder sb = new StringBuilder();
            // Boundary, needs to be added between each block. Always add -- to it
            sb.AppendFormat("--{0}\r\n", boundary);
            // Title field
            sb.AppendFormat("Content-Disposition: form-data; name=\"k\";\r\n\r\n{0}\r\n", Properties.Settings.Default.puush_key);

            sb.AppendFormat("--{0}\r\n", boundary);
            // z thing for some reason
            sb.AppendFormat("Content-Disposition: form-data; name=\"z\";\r\n\r\n{0}\r\n", "LXtory");

            sb.AppendFormat("--{0}\r\n", boundary);
            // filename of the file to upload
            sb.AppendFormat("Content-Disposition: form-data; name=\"f\"; filename=\"{0}\"\r\n", img.filename);
            // Change this according to file ext
            sb.AppendFormat("Content-Type: {0}\r\n\r\n", fileContentType);
            // file comes here
            // Get the bytes for the body
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
            // This is the last part, add -- before and after boundary
            byte[] footer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            
            // Length of the whole thing
            webrequest.ContentLength = body.Length + img.image.Length + footer.Length;

            reqState = new HttpWebRequestState(body.Length + img.image.Length + footer.Length);
            reqState.request = webrequest;

            // copy everything into write buffer
            Buffer.BlockCopy(body, 0, reqState.bufferWrite, 0, body.Length);
            Buffer.BlockCopy(img.image, 0, reqState.bufferWrite, body.Length, img.image.Length);
            Buffer.BlockCopy(footer, 0, reqState.bufferWrite, body.Length + img.image.Length, footer.Length);
            //currentUpload.image = null;
            img.image = null;
            reqState.totalBytes = webrequest.ContentLength;
            reqState.fileURI = new Uri("https://puush.me/api/up");
            reqState.transferStart = DateTime.Now;
            reqState.buffer_size = 4096;
            Stream requestStream = webrequest.GetRequestStream();
            reqState.streamResponse = requestStream;

            try
            {
                while (reqState.bytesWritten < reqState.totalBytes)
                {
                    if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                    {
                        reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                    }
                    requestStream.Write(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size);
                    reqState.bytesWritten += reqState.buffer_size;
                    ProgressUpdate(reqState.bytesWritten, reqState.totalBytes);
                }
                requestStream.Close();
                reqState.streamResponse.Close();
                WebResponse wr = webrequest.GetResponse();
                Stream responseStream = wr.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string response = reader.ReadToEnd();

                reader.Close();
                progressBarUpdate(100);
                if (response == "-1")
                {
                    return new Tuple<bool, string>(false, "Failed :(");
                }
                string[] split = response.Split(',');
                return new Tuple<bool, string>(true, split[1]);
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    throw ExceptionStatus((WebException) e);
                }
                throw new Exception("Error while uploading to Puush.", e);
            }
        }

        private static Exception ExceptionStatus(WebException e)
        {
            HttpWebResponse r = (HttpWebResponse)e.Response;
            if (r.StatusCode == HttpStatusCode.Forbidden)
            {
                return new Exception("Error: Server returned code 403", e);
            }
            if (r.StatusCode == HttpStatusCode.RequestEntityTooLarge)
            {
                return new Exception("Error: Server returned code 413", e);
            }
            if (r.StatusCode == HttpStatusCode.BadRequest)
            {
                return new Exception("Error: Server returned code 400", e);
            }
            return new Exception(e.Message);
        }

        private static void ProgressUpdate(double bytesWritten, double totalBytes)
        {
            double pctComplete = (bytesWritten / totalBytes) * 100.0f;
            progressBarUpdate((int)pctComplete);
            Console.WriteLine("Uploaded: " + pctComplete + "%");
        }

        public Tuple<bool, string> HttpWebRequestUpload(XImage img)
        {
            WebRequestState reqState;
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(new Uri("https://api.imgur.com/3/image"));

            webrequest.Proxy = null;

            if (img.anonupload)
            {
                webrequest.Headers.Add("Authorization", "Client-ID 83c1c8bf9f4d2b1");
            }
            else
            {
                webrequest.Headers.Add("Authorization", "Bearer " + Properties.Settings.Default.accessToken);
            }

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

            webrequest.Host = "api.imgur.com";
            // Random string to be used as a boundary
            string boundary = DateTime.Now.Ticks.ToString("x", CultureInfo.InvariantCulture);
            webrequest.ContentType = "multipart/form-data; boundary=" + boundary;
            webrequest.Method = "POST";
            webrequest.KeepAlive = true;
            webrequest.Timeout = 300000;
            // build the body of the response
            StringBuilder sb = new StringBuilder();
            // Boundary, needs to be added between each block. Always add -- to it
            sb.AppendFormat("--{0}\r\n", boundary);
            // Title field
            sb.AppendFormat("Content-Disposition: form-data; name=\"title\";\r\n\r\n{0}\r\n", img.filename);

            sb.AppendFormat("--{0}\r\n", boundary);
            // filename of the file to upload
            sb.AppendFormat("Content-Disposition: form-data; name=\"image\"; filename=\"{0}\"\r\n", img.filename);
            // Change this according to file ext
            sb.AppendFormat("Content-Type: {0}\r\n\r\n", fileContentType);
            // file comes here
            // Get the bytes for the body
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
            // This is the last part, add -- before and after boundary
            byte[] footer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            // Length of the whole thing
            webrequest.ContentLength = body.Length + img.image.Length + footer.Length;

            reqState = new HttpWebRequestState(body.Length + img.image.Length + footer.Length);
            reqState.request = webrequest;

            Buffer.BlockCopy(body, 0, reqState.bufferWrite, 0, body.Length);
            Buffer.BlockCopy(img.image, 0, reqState.bufferWrite, body.Length, img.image.Length);
            Buffer.BlockCopy(footer, 0, reqState.bufferWrite, body.Length + img.image.Length, footer.Length);
            //currentUpload.image = null;
            img.image = null;
            reqState.totalBytes = webrequest.ContentLength;
            reqState.fileURI = new Uri("https://api.imgur.com/3/image");
            reqState.transferStart = DateTime.Now;
            reqState.buffer_size = 4096;

            Stream requestStream = webrequest.GetRequestStream();
            reqState.streamResponse = requestStream;

            try
            {
                while (reqState.bytesWritten < reqState.totalBytes)
                {
                    if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                    {
                        reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                    }
                    requestStream.Write(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size);
                    reqState.bytesWritten += reqState.buffer_size;
                    ProgressUpdate(reqState.bytesWritten, reqState.totalBytes);
                }
                requestStream.Close();
                reqState.streamResponse.Close();
                WebResponse wr = webrequest.GetResponse();
                Stream responseStream = wr.GetResponseStream();
                StreamReader reader = new StreamReader(responseStream);
                string response = reader.ReadToEnd();
                
                reader.Close();
                progressBarUpdate(100);
                var serializer = new JavaScriptSerializer();
                var json = serializer.Deserialize<dynamic>(response);
                string link = json["data"]["link"];

                return new Tuple<bool, string>(true, link);
            }
            catch (Exception e)
            {
                if (e is WebException)
                {
                    throw ExceptionStatus((WebException)e);
                }
                throw new Exception("Error while uploading to Imgur.", e);
            }
        }

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
                            ProgressUpdate(bytesUploaded, fileSize);
                        });
                }
                catch (Exception)
                {

                    throw;
                }
            }
            //Renci.SshNet.SftpClient client = new Renci.SshNet.SftpClient(;
            
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

        // Class for keeping the state of an ongoing upload
        public abstract class WebRequestState
        {
            public int bytesWritten;           // # bytes read during current transfer
            public long totalBytes;            // Total bytes to read
            public double progIncrement;    // delta % for each buffer read
            public Stream streamResponse;    // Stream to read from
            public byte[] bufferWrite;        // Buffer to read data into
            public Uri fileURI;                // Uri of object being downloaded
            public DateTime transferStart;  // Used for tracking xfr rate
            public int buffer_size;         // How many bytes to send per write

            private WebRequest _request;
            public virtual WebRequest request
            {
                get { return null; }
                set { _request = value; }
            }

            private WebResponse _response;
            public virtual WebResponse response
            {
                get { return null; }
                set { _response = value; }
            }

            public WebRequestState(int buffSize)
            {
                bytesWritten = 0;
                bufferWrite = new byte[buffSize];
                streamResponse = null;
            }
        }

        // Class derived from WebRequestState to keep information about HttpWebRequest
        public class HttpWebRequestState : WebRequestState
        {
            private HttpWebRequest _request;
            public override WebRequest request
            {
                get
                {
                    return _request;
                }
                set
                {
                    _request = (HttpWebRequest)value;
                }
            }

            private HttpWebResponse _response;
            public override WebResponse response
            {
                get
                {
                    return _response;
                }
                set
                {
                    _response = (HttpWebResponse)value;
                }
            }

            public HttpWebRequestState(int buffSize) : base(buffSize) { }
        }
    }
}
