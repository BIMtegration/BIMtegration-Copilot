using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using Newtonsoft.Json;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio para manejar la autenticación OAuth2 con GitHub
    /// </summary>
    public class GitHubAuthService
    {
        private const string CLIENT_ID = "Ov23lijFOcxMmiR9NSut";
        private const string CLIENT_SECRET = "6f8fa6f43e0d96eff0ac6e78e6a46b29689d5f0b";
        private const string REDIRECT_URI = "http://localhost:8080/callback";
        private const string GITHUB_AUTH_URL = "https://github.com/login/oauth/authorize";
        private const string GITHUB_TOKEN_URL = "https://github.com/login/oauth/access_token";
        private const string GITHUB_USER_URL = "https://api.github.com/user";
        
        private HttpListener _httpListener;
        private readonly HttpClient _httpClient;

        public GitHubAuthService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RoslynCopilot-Revit-Addin");
        }

        /// <summary>
        /// Inicia el proceso de autenticación OAuth2 con GitHub
        /// </summary>
        /// <returns>Token de acceso si es exitoso, null si falla</returns>
        public async Task<GitHubAuthResult> AuthenticateAsync()
        {
            try
            {
                // 1. Iniciar el servidor local para recibir el callback
                if (!StartLocalServer())
                {
                    return new GitHubAuthResult 
                    { 
                        Success = false, 
                        ErrorMessage = "No se pudo iniciar el servidor local para OAuth2" 
                    };
                }

                // 2. Generar URL de autorización
                var authUrl = GenerateAuthUrl();

                // 3. Abrir navegador
                OpenBrowser(authUrl);

                // 4. Esperar por el callback
                var authCode = await WaitForCallbackAsync();
                
                // 5. Detener servidor local
                StopLocalServer();

                if (string.IsNullOrEmpty(authCode))
                {
                    return new GitHubAuthResult 
                    { 
                        Success = false, 
                        ErrorMessage = "No se recibió código de autorización" 
                    };
                }

                // 6. Intercambiar código por token
                var accessToken = await ExchangeCodeForTokenAsync(authCode);
                
                if (string.IsNullOrEmpty(accessToken))
                {
                    return new GitHubAuthResult 
                    { 
                        Success = false, 
                        ErrorMessage = "No se pudo obtener el token de acceso" 
                    };
                }

                // 7. Obtener información del usuario
                var userInfo = await GetUserInfoAsync(accessToken);

                return new GitHubAuthResult
                {
                    Success = true,
                    AccessToken = accessToken,
                    Username = userInfo?.Login ?? "Usuario desconocido",
                    UserEmail = userInfo?.Email,
                    AvatarUrl = userInfo?.AvatarUrl
                };
            }
            catch (Exception ex)
            {
                return new GitHubAuthResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Error durante la autenticación: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Verifica si un token es válido
        /// </summary>
        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.GetAsync(GITHUB_USER_URL);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene información del usuario usando el token
        /// </summary>
        public async Task<GitHubUser> GetUserInfoAsync(string accessToken)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
                
                var response = await _httpClient.GetAsync(GITHUB_USER_URL);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<GitHubUser>(jsonContent);
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private bool StartLocalServer()
        {
            try
            {
                _httpListener = new HttpListener();
                _httpListener.Prefixes.Add(REDIRECT_URI + "/");
                _httpListener.Start();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void StopLocalServer()
        {
            try
            {
                _httpListener?.Stop();
                _httpListener?.Close();
            }
            catch { }
        }

        private string GenerateAuthUrl()
        {
            var state = Guid.NewGuid().ToString("N").Substring(0, 16); // Seguridad adicional
            var scopes = "user:email,repo"; // Permisos que necesitamos
            
            return $"{GITHUB_AUTH_URL}?" +
                   $"client_id={CLIENT_ID}&" +
                   $"redirect_uri={Uri.EscapeDataString(REDIRECT_URI)}&" +
                   $"scope={Uri.EscapeDataString(scopes)}&" +
                   $"state={state}&" +
                   $"allow_signup=true";
        }

        private void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                MessageBox.Show($"No se pudo abrir el navegador automáticamente.\n\n" +
                               $"Por favor, copia esta URL en tu navegador:\n{url}",
                               "Autenticación GitHub", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async Task<string> WaitForCallbackAsync()
        {
            try
            {
                var context = await _httpListener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                // Extraer el código de autorización
                var code = request.QueryString.Get("code");
                var error = request.QueryString.Get("error");

                // Responder al navegador
                string responseString = "";
                if (!string.IsNullOrEmpty(code))
                {
                    responseString = CreateSuccessHtml();
                }
                else
                {
                    responseString = CreateErrorHtml(error ?? "Error desconocido");
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                response.ContentType = "text/html";
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();

                return code;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            try
            {
                var parameters = new Dictionary<string, string>
                {
                    {"client_id", CLIENT_ID},
                    {"client_secret", CLIENT_SECRET},
                    {"code", code},
                    {"redirect_uri", REDIRECT_URI}
                };

                var content = new FormUrlEncodedContent(parameters);
                
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                _httpClient.DefaultRequestHeaders.Add("User-Agent", "RoslynCopilot-Revit-Addin");

                var response = await _httpClient.PostAsync(GITHUB_TOKEN_URL, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var jsonContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<GitHubTokenResponse>(jsonContent);
                    return tokenResponse?.AccessToken;
                }
                
                return null;
            }
            catch
            {
                return null;
            }
        }

        private string CreateSuccessHtml()
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <title>Autenticación Exitosa</title>
    <meta charset='utf-8'>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; text-align: center; padding: 50px; background: #f8f9fa; }
        .container { max-width: 400px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
        .success { color: #28a745; font-size: 48px; margin-bottom: 20px; }
        h1 { color: #333; margin-bottom: 15px; }
        p { color: #666; line-height: 1.5; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='success'>✅</div>
        <h1>¡Autenticación Exitosa!</h1>
        <p>Te has conectado correctamente con GitHub.</p>
        <p>Ya puedes cerrar esta ventana y volver a Revit.</p>
    </div>
    <script>
        setTimeout(function() { window.close(); }, 3000);
    </script>
</body>
</html>";
        }

        private string CreateErrorHtml(string error)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <title>Error de Autenticación</title>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; text-align: center; padding: 50px; background: #f8f9fa; }}
        .container {{ max-width: 400px; margin: 0 auto; background: white; padding: 40px; border-radius: 8px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .error {{ color: #dc3545; font-size: 48px; margin-bottom: 20px; }}
        h1 {{ color: #333; margin-bottom: 15px; }}
        p {{ color: #666; line-height: 1.5; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='error'>❌</div>
        <h1>Error de Autenticación</h1>
        <p>Hubo un problema durante la autenticación:</p>
        <p><strong>{error}</strong></p>
        <p>Por favor, intenta nuevamente.</p>
    </div>
</body>
</html>";
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            StopLocalServer();
        }
    }

    /// <summary>
    /// Resultado de la autenticación con GitHub
    /// </summary>
    public class GitHubAuthResult
    {
        public bool Success { get; set; }
        public string AccessToken { get; set; }
        public string Username { get; set; }
        public string UserEmail { get; set; }
        public string AvatarUrl { get; set; }
        public string ErrorMessage { get; set; }
    }

    /// <summary>
    /// Información del usuario de GitHub
    /// </summary>
    public class GitHubUser
    {
        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("avatar_url")]
        public string AvatarUrl { get; set; }

        [JsonProperty("bio")]
        public string Bio { get; set; }

        [JsonProperty("public_repos")]
        public int PublicRepos { get; set; }
    }

    /// <summary>
    /// Respuesta del token de GitHub
    /// </summary>
    public class GitHubTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("scope")]
        public string Scope { get; set; }
    }
}