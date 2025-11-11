using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Launcher.Shared;

namespace LauncherHotupdate.Core
{
    public class LauncherUpdateManager
    {
        private readonly HttpClient _httpClient;
        public HttpClient HttpClient => _httpClient;

        private readonly CookieContainer _cookieContainer;
        private readonly Uri _endpoint;
        public string Endpoint => _endpoint.ToString();
        private readonly string _authFilePath;

        private static string endpointSaveFile = Path.Combine(AppContext.BaseDirectory, "launcher_endpoint.txt");

        public LauncherUpdateManager()
        {
            if (!File.Exists(endpointSaveFile))
                File.WriteAllText(endpointSaveFile, "http://localhost:5000/Hotupdate");

            _endpoint = new Uri(File.ReadAllText(endpointSaveFile));

            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = _cookieContainer, UseCookies = true };

            _httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMinutes(5) // 默认下载/更新超时
            };

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "NHLauncher");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            _authFilePath = Path.Combine(dir, "auth.json");

            // 读取 cookie
            var authData = LoadAuthData();
            if (!string.IsNullOrEmpty(authData?.Token))
            {
                _cookieContainer.Add(new Cookie("LauncherAuth", authData.Token)
                {
                    Domain = _endpoint.Host,
                    Path = "/"
                });
            }
        }
        #region 登录 / 登出 / 验证

        public async Task<(string?, string?)> LoginAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return ("非法的用户名或密码", null);

            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 登录短超时

            try
            {
                var form = new MultipartFormDataContent
                {
                    { new StringContent("login"), "cmd" },
                    { new StringContent(userName), "userName" },
                    { new StringContent(PasswordHelper.HashPasswordClient(password)), "password" }
                };

                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return ($"登录失败，服务器返回状态码 {resp.StatusCode}, Content:{content}", null);

                var cookie = GetCookieFromContainer() ?? TryParseCookie(resp);
                if (!string.IsNullOrEmpty(cookie))
                {
                    SaveAuthData(userName, cookie);
                    return (null, cookie);
                }

                return ("登录失败，未收到认证信息", null);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Login请求超时（服务未启动或网络问题）");
                return ("登录请求超时（服务未启动或网络问题）", null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Login异常：" + ex);
                return ($"登录异常：{ex.Message}", null);
            }
        }

        public (string?, string?) Login(string userName, string password)
            => LoginAsync(userName, password).GetAwaiter().GetResult();

        public async Task<string> LogoutAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Logout短超时
            var authData = LoadAuthData();
            if (!string.IsNullOrEmpty(authData?.Token))
            {
                _httpClient.DefaultRequestHeaders.Remove("Cookie");
                _httpClient.DefaultRequestHeaders.Add("Cookie", $"LauncherAuth={authData.Token}");
            }

            try
            {
                var form = new MultipartFormDataContent
                {
                    { new StringContent("logout"), "cmd" }
                };
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return ($"登录失败，服务器返回状态码 {resp.StatusCode}, Content:{content}");
            }
            catch (TaskCanceledException)
            {
                return "Logout请求超时";
            }
            catch (Exception ex)
            {
                return "Logout异常：" + ex;
            }
            finally
            {
                DeleteCookie();
            }
            return "成功";
        }

        public void Logout() => LogoutAsync().GetAwaiter().GetResult();

        public async Task<(bool, string?)> IsLoggedIn()
        {
            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token))
                return (false, null);

            try
            {
               return  await VerifyAsync();
            }
            catch
            {
                return (false, null);
            }
        }

        public async Task<(bool,string?)> VerifyAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // Verify短超时

            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token))
                return (false,null);

            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"LauncherAuth={authData.Token}");

            try
            {
                var form = new MultipartFormDataContent
                {
                    { new StringContent("verify"), "cmd" }
                };
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                return (resp.IsSuccessStatusCode,authData.UserName);
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Verify请求超时");
                return (false, null);
            }
            catch
            {
                return (false, null);
            }
        }
        public async Task<(string? error, List<Project>? projects)> GetProjectsAsync()
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)); // 短超时
            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token))
                return ("未登录或登录信息丢失", null);

            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"LauncherAuth={authData.Token}");

            try
            {
                var form = new MultipartFormDataContent
        {
            { new StringContent("getprojects"), "cmd" }
        };

                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                if (!resp.IsSuccessStatusCode)
                {
                    return ($"获取项目失败：{resp.StatusCode}", null);
                }

                var json = await resp.Content.ReadAsStringAsync();
                var projects = JsonConvert.DeserializeObject<ProjectWrapper>(json);
                return (null, projects?.Projects ?? new List<Project>());
            }
            catch (TaskCanceledException)
            {
                return ("请求超时（服务未启动或网络异常）", null);
            }
            catch (Exception ex)
            {
                return ($"异常：{ex.Message}", null);
            }
        }

        private class ProjectWrapper
        {
            public string? message;
            public List<Project>? Projects;
        }
        #endregion

        #region 文件存储逻辑

        private class AuthData
        {
            public string UserName = "";
            public string Token;
            public DateTime SaveTimeUtc;
            public AuthData(string userName, string token, DateTime saveTimeUtc)
            {
                UserName = userName;
                Token = token;
                SaveTimeUtc = saveTimeUtc;
            }
        }

        private void SaveAuthData(string userName, string token)
        {
            var json = JsonConvert.SerializeObject(new AuthData(userName, token, DateTime.UtcNow));
            File.WriteAllText(_authFilePath, json);
        }

        private AuthData? LoadAuthData()
        {
            if (!File.Exists(_authFilePath))
                return null;

            try
            {
                var json = File.ReadAllText(_authFilePath);
                var data = JsonConvert.DeserializeObject<AuthData>(json);
                return data;
            }
            catch
            {
                return null;
            }
        }

        private void DeleteCookie()
        {
            if (File.Exists(_authFilePath))
            {
                try { File.Delete(_authFilePath); } catch { }
            }
        }

        #endregion

        #region 工具函数

        private string? GetCookieFromContainer()
        {
            try
            {
                var uri = new UriBuilder(_endpoint.Scheme, _endpoint.Host).Uri;
                var cookies = _cookieContainer.GetCookies(uri);
                foreach (Cookie c in cookies)
                {
                    if (c.Name.Equals("LauncherAuth", StringComparison.OrdinalIgnoreCase))
                        return c.Value;
                }
            }
            catch { }
            return null;
        }

        private string? TryParseCookie(HttpResponseMessage resp)
        {
            if (resp.Headers.TryGetValues("Set-Cookie", out var vals))
            {
                foreach (var c in vals)
                {
                    if (c.StartsWith("LauncherAuth="))
                        return c.Split('=')[1].Split(';')[0];
                }
            }
            return null;
        }

        #endregion
    }
}
