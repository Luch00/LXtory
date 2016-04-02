using System;
using System.Text;
using System.Collections.Specialized;
using System.Net;
using System.IO;
using System.Globalization;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace ScreenShotterWPF
{
    public class Uploader
    {
        // For imgur
        private const string ClientID = "83c1c8bf9f4d2b1";
        private const string ClientSecret = "33dac3b1adfcefab926d83f1cb21412cf32ee36a";
        
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
            //throw new Exception("FART");
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

        /*private void WriteCallback2(IAsyncResult asyncResult)
        {
            WebRequestState reqState = ((WebRequestState)(asyncResult.AsyncState));
            Stream responseStream = reqState.streamResponse;
            responseStream.EndWrite(asyncResult);
            StatusChange("Uploading.." + uploading + "/" + totalUploading);
            reqState.bytesWritten += reqState.buffer_size;

            if (reqState.bytesWritten != reqState.totalBytes)
            {
                if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                {
                    reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                }

                double pctComplete = ((double)reqState.bytesWritten / (double)reqState.totalBytes) * 100.0f;
                progressBarUpdate((int)pctComplete);
                Console.WriteLine("Uploaded: " + pctComplete.ToString() + "%");
                if (pctComplete >= 10 && pctComplete < 20)
                {
                    IconChange("10");
                }
                else if (pctComplete >= 20 && pctComplete < 30)
                {
                    IconChange("20");
                }
                else if (pctComplete >= 30 && pctComplete < 40)
                {
                    IconChange("30");
                }
                else if (pctComplete >= 40 && pctComplete < 50)
                {
                    IconChange("40");
                }
                else if (pctComplete >= 50 && pctComplete < 60)
                {
                    IconChange("50");
                }
                else if (pctComplete >= 60 && pctComplete < 70)
                {
                    IconChange("60");
                }
                else if (pctComplete >= 70 && pctComplete < 80)
                {
                    IconChange("70");
                }
                else if (pctComplete >= 80 && pctComplete < 90)
                {
                    IconChange("80");
                }
                else if (pctComplete >= 90)
                {
                    IconChange("90");
                }
                responseStream.BeginWrite(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size, new AsyncCallback(WriteCallback2), reqState);
                return;
            }
            else
            {
                responseStream.Close();
                reqState.streamResponse.Close();
                try
                {
                    reqState.request.BeginGetResponse(ReadHttpResponseCallback2, reqState);
                }
                catch (WebException wex)
                {
                    Console.WriteLine("ERROR: " + wex.Message);
                    HttpWebResponse r = (HttpWebResponse)wex.Response;
                    IconChange("E");
                    if (r.StatusCode == HttpStatusCode.Forbidden)
                    {
                        StatusChange("Error: Server returned code 403");
                        //uploadQ.Clear();
                        //uploading = 1;
                        //totalUploading = 1;
                    }
                    if (r.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                    {
                        StatusChange("Error: Server returned code 413");
                        //uploadQ.Clear();
                        //uploading = 1;
                        //totalUploading = 1;
                    }
                    if (r.StatusCode == HttpStatusCode.BadRequest)
                    {
                        StatusChange("Error: Server returned code 400");
                        //uploadQ.Clear();
                        //uploading = 1;
                        //totalUploading = 1;
                    }
                }
            }
        }*/

        /*private void ReadHttpResponseCallback2(IAsyncResult asyncResult)
        {
            WebRequestState reqState = ((WebRequestState)(asyncResult.AsyncState));
            HttpWebResponse webresponse = (HttpWebResponse)reqState.request.EndGetResponse(asyncResult);
            StreamReader reader = new StreamReader(webresponse.GetResponseStream());
            string response = reader.ReadToEnd();
            reader.Close();
            progressBarUpdate(100);
            StatusChange("Done");

            dynamic json;

            json = JObject.Parse(response);
            string link = json.data.link;

            currentUpload.image = null;
            addXImageToList(currentUpload, link);

            IconChange("F");

            if(queue.Count == 0)
            {
                uploading = 0;
                totalUploading = 0;
                StatusChange("Done");
                delayedIconChange(5000);
            }
            uploadComplete = true;
        }*/

        /*private void HttpWebRequestUpload()
        {
            WebRequestState reqState = null;

            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(new Uri("https://api.imgur.com/3/image"));            
            
            webrequest.Proxy = null;

            if (uploadQ[0].anonupload)
            {
                webrequest.Headers.Add("Authorization", "Client-ID 83c1c8bf9f4d2b1");
            }
            else
            {
                webrequest.Headers.Add("Authorization", "Bearer " + Properties.Settings.Default.accessToken);
            }

            string fileContentType = "image/png";

            string ext = Path.GetExtension(uploadQ[0].filepath).ToLower();
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
            webrequest.KeepAlive = false;
            webrequest.Timeout = 300000;

            // build the body of the response
            StringBuilder sb = new StringBuilder();
            // Boundary, needs to be added between each block. Always add -- to it
            sb.AppendFormat("--{0}\r\n", boundary);
            // Title field
            sb.AppendFormat("Content-Disposition: form-data; name=\"title\";\r\n\r\n{0}\r\n", uploadQ[0].filename);

            sb.AppendFormat("--{0}\r\n", boundary);
            // filename of the file to upload
            sb.AppendFormat("Content-Disposition: form-data; name=\"image\"; filename=\"{0}\"\r\n", uploadQ[0].filename);
            // Change this according to file ext
            sb.AppendFormat("Content-Type: {0}\r\n\r\n", fileContentType);
            // file comes here
            // Get the bytes for the body
            byte[] body = Encoding.UTF8.GetBytes(sb.ToString());
            // This is the last part, add -- before and after boundary
            byte[] footer = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
            
            // Length of the whole thing
            webrequest.ContentLength = body.Length + uploadQ[0].image.Length + footer.Length;

            // Copy all data into single byte array
            byte[] content = new byte[body.Length + uploadQ[0].image.Length + footer.Length];
            System.Buffer.BlockCopy(body, 0, content, 0, body.Length);
            System.Buffer.BlockCopy(uploadQ[0].image, 0, content, body.Length, uploadQ[0].image.Length);
            System.Buffer.BlockCopy(footer, 0, content, body.Length + uploadQ[0].image.Length, footer.Length);            

            reqState = new HttpWebRequestState(body.Length + uploadQ[0].image.Length + footer.Length);
            reqState.request = webrequest;            
            System.Buffer.BlockCopy(content, 0, reqState.bufferWrite, 0, content.Length);
            reqState.totalBytes = webrequest.ContentLength;
            reqState.fileURI = new Uri("https://api.imgur.com/3/image");
            reqState.transferStart = DateTime.Now;
            reqState.buffer_size = 4096;
            if (reqState.totalBytes < 4096)
            {
                reqState.buffer_size = (int)reqState.totalBytes;
            }
            Stream responseStream = webrequest.GetRequestStream();
            reqState.streamResponse = responseStream;
            
            StatusChange("Uploading.." + uploading + "/" + totalUploading);
            
            responseStream.BeginWrite(content, 0, reqState.buffer_size, WriteCallback, reqState);
        }*/

        /*private void WriteCallback(IAsyncResult asyncResult)
        {
            WebRequestState reqState = ((WebRequestState)(asyncResult.AsyncState));
            Stream responseStream = reqState.streamResponse;
            responseStream.EndWrite(asyncResult);
            StatusChange("Uploading.." + uploading + "/" + totalUploading);
            reqState.bytesWritten += reqState.buffer_size;

            if (reqState.bytesWritten != reqState.totalBytes)
            {
                if ((reqState.bytesWritten + reqState.buffer_size) > reqState.totalBytes)
                {
                    reqState.buffer_size = (int)reqState.totalBytes - reqState.bytesWritten;
                }

                double pctComplete = ((double)reqState.bytesWritten / (double)reqState.totalBytes) * 100.0f;
                progressBarUpdate((int)pctComplete);
                Console.WriteLine("Uploaded: " + pctComplete.ToString() + "%");
                if (pctComplete >= 10 && pctComplete < 20)
                {
                    IconChange("10");
                }
                else if (pctComplete >= 20 && pctComplete < 30)
                {
                    IconChange("20");
                }
                else if (pctComplete >= 30 && pctComplete < 40)
                {
                    IconChange("30");
                }
                else if (pctComplete >= 40 && pctComplete < 50)
                {
                    IconChange("40");
                }
                else if (pctComplete >= 50 && pctComplete < 60)
                {
                    IconChange("50");
                }
                else if (pctComplete >= 60 && pctComplete < 70)
                {
                    IconChange("60");
                }
                else if (pctComplete >= 70 && pctComplete < 80)
                {
                    IconChange("70");
                }
                else if (pctComplete >= 80 && pctComplete < 90)
                {
                    IconChange("80");
                }
                else if (pctComplete >= 90)
                {
                    IconChange("90");
                }
                responseStream.BeginWrite(reqState.bufferWrite, reqState.bytesWritten, reqState.buffer_size, new AsyncCallback(WriteCallback), reqState);
                return;
            }
            else
            {
                responseStream.Close();
                reqState.streamResponse.Close();
                try
                {
                    reqState.request.BeginGetResponse(ReadHttpResponseCallback, reqState);
                }
                catch (WebException wex)
                {
                    Console.WriteLine("ERROR: " + wex.Message);
                    HttpWebResponse r = (HttpWebResponse)wex.Response;
                    IconChange("E");
                    if (r.StatusCode == HttpStatusCode.Forbidden)
                    {
                        StatusChange("Error: Server returned code 403");
                        uploadQ.Clear();
                        uploading = 1;
                        totalUploading = 1;
                    }
                    if (r.StatusCode == HttpStatusCode.RequestEntityTooLarge)
                    {
                        StatusChange("Error: Server returned code 413");
                        uploadQ.Clear();
                        uploading = 1;
                        totalUploading = 1;
                    }
                    if (r.StatusCode == HttpStatusCode.BadRequest)
                    {
                        StatusChange("Error: Server returned code 400");
                        uploadQ.Clear();
                        uploading = 1;
                        totalUploading = 1;
                    }
                }
            }
        }*/

        /*private void ReadHttpResponseCallback(IAsyncResult asyncResult)
        {
            WebRequestState reqState = ((WebRequestState)(asyncResult.AsyncState));
            HttpWebResponse webresponse = (HttpWebResponse)reqState.request.EndGetResponse(asyncResult);
            StreamReader reader = new StreamReader(webresponse.GetResponseStream());
            string response = reader.ReadToEnd();
            reader.Close();
            progressBarUpdate(100);
            StatusChange("Done");

            dynamic json;
            
            json = JObject.Parse(response);
            string link = json.data.link;

            uploadQ[0].image = null;

            addXImageToList(uploadQ[0], link);
            uploadQ.Remove(uploadQ[0]);

            IconChange("F");

            if (uploadQ.Count > 0)
            {
                if (uploadQ[0].image == null)
                {
                    using (FileStream fileData = File.Open(uploadQ[0].filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            fileData.CopyTo(ms);
                            uploadQ[0].image = new byte[ms.Length];
                            uploadQ[0].image = ms.ToArray();
                        }
                    }
                }
                uploading++;

                HttpWebRequestUpload();
            }
            else
            {
                uploading = 1;
                totalUploading = 1;
                StatusChange("Done");
                //w.Dispose();
                delayedIconChange(5000);
            }
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
