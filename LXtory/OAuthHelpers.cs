using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LXtory
{
    static class OAuthHelpers
    {
        public static string ImgurID => "";
        public static string ImgurSecret => "";

        public static string GyazoID => "";
        public static string GyazoSecret => "";

        public static string DropboxID => "";
        public static string DropboxSecret => "";

        public static string GoogleDriveID => "";
        public static string GoogleDriveSecret => "";

        private static async Task<string> GetTokenResponse(string uri, NameValueCollection values, Dictionary<HttpRequestHeader, string> headers = null)
        {
            string responseString;
            using (var w = new WebClient())
            {
                w.Proxy = null;
                if (headers != null)
                {
                    foreach (var header in headers)
                    {
                        w.Headers[header.Key] = header.Value;
                    }
                }
                var response = await w.UploadValuesTaskAsync(uri, values);
                responseString = Encoding.UTF8.GetString(response, 0, response.Length);
            }
            return responseString;
        }

        public static bool TokenNeedsRefresh(UploadSite site)
        {
            switch (site)
            {
                case UploadSite.Imgur:
                    {
                        TimeSpan diff = DateTime.Now.Subtract(Properties.Settings.Default.lastRefreshTime);
                        //return diff.TotalSeconds >= settings.imgurTokenExpire;
                        return diff.TotalSeconds >= 3600; // BECAUSE IMGUR FAILS
                    }

                case UploadSite.GoogleDrive:
                    {
                        TimeSpan diff = DateTime.Now.Subtract(Properties.Settings.Default.gdriveRefreshTime);
                        return diff.TotalSeconds >= Properties.Settings.Default.gdriveTokenExpire;
                    }
                default:
                    return true;
            }
        }

        public static async Task GetImgurToken(string code)
        {
            try
            {
                NameValueCollection v = new NameValueCollection
                {
                    {"client_id", ImgurID},
                    {"client_secret", ImgurSecret},
                    {"grant_type", "authorization_code"},
                    {"code", code}
                };
                var response = await GetTokenResponse("https://api.imgur.com/oauth2/token", v);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                Properties.Settings.Default.accessToken = json["access_token"];
                Properties.Settings.Default.refreshToken = json["refresh_token"];
                Properties.Settings.Default.username = json["account_username"];
                Properties.Settings.Default.imgurTokenExpire = Int32.Parse(json["expires_in"]);
                Properties.Settings.Default.lastRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error while fetching tokens", e);
            }
        }

        public static async Task RefreshImgurToken()
        {
            if (Properties.Settings.Default.refreshToken != string.Empty)
            {
                NameValueCollection v = new NameValueCollection
                {
                    {"refresh_token", Properties.Settings.Default.refreshToken},
                    {"client_id", ImgurID},
                    {"client_secret", ImgurSecret},
                    {"grant_type", "refresh_token"}
                };
                var response = await GetTokenResponse("https://api.imgur.com/oauth2/token", v);
                
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                Properties.Settings.Default.accessToken = json["access_token"];
                Properties.Settings.Default.refreshToken = json["refresh_token"];
                Properties.Settings.Default.imgurTokenExpire = Int32.Parse(json["expires_in"]);
                Properties.Settings.Default.lastRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
        }

        public static async Task GetGyazoToken(string code)
        {
            try
            {
                NameValueCollection v = new NameValueCollection
                {
                    {"client_id", GyazoID},
                    {"redirect_uri", "http://localhost:8080/LXtory_Auth/"},
                    {"code", code},
                    {"grant_type", "authorization_code"},
                    {"scope", "public"}
                };
                var base64 = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{GyazoID}:{GyazoSecret}"));
                var header = new Dictionary<HttpRequestHeader, string>
                {
                    [HttpRequestHeader.Authorization] = $"Basic {base64}"
                };
                var response = await GetTokenResponse("http://api.gyazo.com/oauth/token", v, header);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
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
                NameValueCollection v = new NameValueCollection
                {
                    {"client_id", DropboxID},
                    {"client_secret", DropboxSecret},
                    {"code", code},
                    {"grant_type", "authorization_code"},
                    {"redirect_uri", "http://localhost:8080/LXtory_Auth/"}
                };
                var response = await GetTokenResponse("https://api.dropboxapi.com/oauth2/token", v);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                Properties.Settings.Default.dropboxToken = json["access_token"];
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error while fetching tokens", e);
            }
        }

        public static async Task GetGoogleDriveToken(string code, string redirectUri, string codeVerifier)
        {
            try
            {
                var v = new NameValueCollection
                {
                    { "code", code},
                    { "redirect_uri", redirectUri},
                    { "client_id", GoogleDriveID },
                    { "client_secret", GoogleDriveSecret},
                    { "code_verifier", codeVerifier},
                    { "scope", ""},
                    { "grant_type", "authorization_code"}
                };
                var response = await GetTokenResponse("https://www.googleapis.com/oauth2/v4/token", v);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                Properties.Settings.Default.gdriveToken = json["access_token"];
                Properties.Settings.Default.gdriveRefreshToken = json["refresh_token"];
                Properties.Settings.Default.gdriveTokenExpire = Int32.Parse(json["expires_in"]);
                Properties.Settings.Default.gdriveRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
            catch (Exception e)
            {
                throw new Exception("Error while fetching tokens", e);
            }
        }

        public static async Task RefreshGoogleDriveToken()
        {
            if (Properties.Settings.Default.gdriveRefreshToken != string.Empty)
            {
                var v = new NameValueCollection
                {
                    {"client_id", GoogleDriveID},
                    {"client_secret", GoogleDriveSecret},
                    {"refresh_token", Properties.Settings.Default.gdriveRefreshToken},
                    {"grant_type", "refresh_token"}
                };
                var response = await GetTokenResponse("https://www.googleapis.com/oauth2/v4/token", v);
                var json = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
                Properties.Settings.Default.gdriveToken = json["access_token"];
                Properties.Settings.Default.gdriveTokenExpire = Int32.Parse(json["expires_in"]);
                Properties.Settings.Default.gdriveRefreshTime = DateTime.Now;
                Properties.Settings.Default.Save();
            }
        }
    }
}
