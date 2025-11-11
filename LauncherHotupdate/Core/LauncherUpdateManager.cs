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
        private readonly CookieContainer _cookieContainer;
        private readonly Uri _endpoint;
        private readonly string _authFilePath;
        private static readonly string endpointSaveFile = Path.Combine(AppContext.BaseDirectory, "launcher_endpoint.txt");

        public HttpClient HttpClient => _httpClient;
        public string Endpoint => _endpoint.ToString();

        public LauncherUpdateManager()
        {
            if (!File.Exists(endpointSaveFile))
                File.WriteAllText(endpointSaveFile, "http://localhost:5000/Hotupdate");

            _endpoint = new Uri(File.ReadAllText(endpointSaveFile));

            _cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler { CookieContainer = _cookieContainer, UseCookies = true };
            _httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NHLauncher");
            Directory.CreateDirectory(appData);
            _authFilePath = Path.Combine(appData, "auth.json");

            // 初始化 Cookie
            var authData = LoadAuthData();
            if (!string.IsNullOrEmpty(authData?.Token))
                _cookieContainer.Add(new Cookie("LauncherAuth", authData.Token) { Domain = _endpoint.Host, Path = "/" });
        }

        #region 登录 / 登出 / 验证

        public async Task<(string? error, string? token)> LoginAsync(string userName, string password)
        {
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return ("非法的用户名或密码", null);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var form = BuildForm("login", ("userName", userName), ("password", PasswordHelper.HashPasswordClient(password)));
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                var content = await resp.Content.ReadAsStringAsync();

                if (!resp.IsSuccessStatusCode)
                    return ($"登录失败，状态码 {resp.StatusCode}, 内容: {content}", null);

                var cookie = GetCookieFromContainer() ?? TryParseCookie(resp);
                if (!string.IsNullOrEmpty(cookie))
                {
                    SaveAuthData(userName, cookie);
                    return (null, cookie);
                }
                return ("登录失败，未收到认证信息", null);
            }
            catch (TaskCanceledException) { return ("登录请求超时（服务未启动或网络问题）", null); }
            catch (Exception ex) { return ($"登录异常：{ex.Message}", null); }
        }

        public (string? error, string? token) Login(string userName, string password)
            => LoginAsync(userName, password).GetAwaiter().GetResult();

        public async Task<string> LogoutAsync()
        {
            var authData = LoadAuthData();
            if (!string.IsNullOrEmpty(authData?.Token))
                SetAuthCookie(authData.Token);

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var form = BuildForm("logout");
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                var content = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return $"Logout失败，状态码 {resp.StatusCode}, 内容: {content}";
            }
            catch (TaskCanceledException) { return "Logout请求超时"; }
            catch (Exception ex) { return $"Logout异常：{ex.Message}"; }
            finally { DeleteAuthData(); }

            return "成功";
        }

        public void Logout() => LogoutAsync().GetAwaiter().GetResult();

        public async Task<(bool isLoggedIn, string? userName)> IsLoggedIn()
        {
            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token)) return (false, null);
            try { return await VerifyAsync(); }
            catch { return (false, null); }
        }

        public async Task<(bool, string?)> VerifyAsync()
        {
            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token)) return (false, null);

            SetAuthCookie(authData.Token);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var form = BuildForm("verify");
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);
                return (resp.IsSuccessStatusCode, authData.UserName);
            }
            catch { return (false, null); }
        }

        public async Task<(string? error, List<Project>? projects)> GetProjectsAsync()
        {
            var authData = LoadAuthData();
            if (string.IsNullOrEmpty(authData?.Token)) return ("未登录或登录信息丢失", null);

            SetAuthCookie(authData.Token);
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var form = BuildForm("getprojects");
                var resp = await _httpClient.PostAsync(_endpoint, form, cts.Token);

                if (!resp.IsSuccessStatusCode) return ($"获取项目失败：{resp.StatusCode}", null);

                var json = await resp.Content.ReadAsStringAsync();
                var wrapper = JsonConvert.DeserializeObject<ProjectWrapper>(json);
                return (null, wrapper?.Projects ?? new List<Project>());
            }
            catch (TaskCanceledException) { return ("请求超时（服务未启动或网络异常）", null); }
            catch (Exception ex) { return ($"异常：{ex.Message}", null); }
        }

        private class ProjectWrapper { public string? message; public List<Project>? Projects; }

        #endregion

        #region 文件存储 / Auth管理

        private class AuthData
        {
            public string UserName = "";
            public string Token = "";
            public DateTime SaveTimeUtc;

            public AuthData() { }
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
            if (!File.Exists(_authFilePath)) return null;
            try { return JsonConvert.DeserializeObject<AuthData>(File.ReadAllText(_authFilePath)); }
            catch { return null; }
        }

        private void DeleteAuthData()
        {
            if (File.Exists(_authFilePath))
            {
                try { File.Delete(_authFilePath); } catch { }
            }
        }

        #endregion

        #region 工具 / 私有方法

        private MultipartFormDataContent BuildForm(string cmd, params (string name, string value)[] fields)
        {
            var form = new MultipartFormDataContent { { new StringContent(cmd), "cmd" } };
            foreach (var (name, value) in fields)
                form.Add(new StringContent(value), name);
            return form;
        }

        private void SetAuthCookie(string token)
        {
            _httpClient.DefaultRequestHeaders.Remove("Cookie");
            _httpClient.DefaultRequestHeaders.Add("Cookie", $"LauncherAuth={token}");
        }

        private string? GetCookieFromContainer()
        {
            try
            {
                var uri = new UriBuilder(_endpoint.Scheme, _endpoint.Host).Uri;
                foreach (Cookie c in _cookieContainer.GetCookies(uri))
                    if (c.Name.Equals("LauncherAuth", StringComparison.OrdinalIgnoreCase))
                        return c.Value;
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
