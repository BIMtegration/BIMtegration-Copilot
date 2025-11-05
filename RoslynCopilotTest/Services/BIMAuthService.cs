using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio de autenticación con BIMtegration Auth Server (Google Apps Script)
    /// </summary>
    public class BIMAuthService
    {
        private const string AUTH_SERVER_URL = "https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec";
        private static readonly string TokenFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot",
            "bim_auth.dat"
        );
        private static readonly byte[] entropy = Encoding.UTF8.GetBytes("BIMtegration2025");
        
        private string _currentToken;
        private UserInfo _currentUser;

        public BIMAuthService()
        {
            LoadSavedToken();
        }

        /// <summary>
        /// Indica si hay un usuario autenticado
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(_currentToken);

        /// <summary>
        /// Información del usuario actual
        /// </summary>
        public UserInfo CurrentUser => _currentUser;

        /// <summary>
        /// Realiza login con usuario y contraseña
        /// </summary>
        public async Task<LoginResult> LoginAsync(string usuario, string clave)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(30);

                    // Crear payload según formato de tu GAS
                    var payload = new
                    {
                        action = "login",
                        usuario = usuario,
                        clave = clave
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(AUTH_SERVER_URL, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    // Log de respuesta del backend
                    LogToFile($"[BIMAuthService.LoginAsync] Respuesta del backend recibida (length: {responseBody?.Length ?? 0})");

                    // Parsear JSON usando JObject para acceso seguro
                    var jObject = JObject.Parse(responseBody);
                    
                    bool ok = jObject["ok"]?.Value<bool>() ?? false;
                    string token = jObject["token"]?.Value<string>();
                    string plan = jObject["plan"]?.Value<string>() ?? "premium";
                    
                    LogToFile($"[BIMAuthService.LoginAsync] ok={ok}, token length={token?.Length ?? 0}, plan={plan}");

                    // Intentar obtener botones de userData
                    var buttons = new List<PremiumButtonInfo>();
                    try
                    {
                        var userDataObj = jObject["userData"];
                        LogToFile($"[BIMAuthService.LoginAsync] userData found: {userDataObj != null}");
                        
                        if (userDataObj != null)
                        {
                            // Intenta obtener Buttons del userData
                            var buttonsToken = userDataObj["Buttons"];
                            string buttonsString = buttonsToken?.Value<string>();
                            
                            LogToFile($"[BIMAuthService.LoginAsync] buttonsToken type: {buttonsToken?.Type}");
                            LogToFile($"[BIMAuthService.LoginAsync] buttonsToken is null: {buttonsToken == null}");
                            LogToFile($"[BIMAuthService.LoginAsync] buttonsString = '{buttonsString}'");
                            LogToFile($"[BIMAuthService.LoginAsync] buttonsString length: {buttonsString?.Length ?? 0}");
                            
                            // Si buttonsString está vacío, intenta parsear el JSON entero nuevamente
                            if (string.IsNullOrEmpty(buttonsString) && responseBody.Contains("\"Buttons\":"))
                            {
                                LogToFile($"[BIMAuthService.LoginAsync] 🔍 Intentando extraer Buttons manualmente del JSON...");
                                
                                // Buscar la posición de "Buttons":
                                int buttonsIndex = responseBody.IndexOf("\"Buttons\":");
                                if (buttonsIndex > 0)
                                {
                                    int startQuote = responseBody.IndexOf("\"", buttonsIndex + 10);
                                    if (startQuote > 0)
                                    {
                                        int endQuote = responseBody.IndexOf("\"", startQuote + 1);
                                        // Buscar el cierre real (puede haber escapes)
                                        while (endQuote > 0 && responseBody[endQuote - 1] == '\\')
                                        {
                                            endQuote = responseBody.IndexOf("\"", endQuote + 1);
                                        }
                                        
                                        if (endQuote > startQuote)
                                        {
                                            buttonsString = responseBody.Substring(startQuote + 1, endQuote - startQuote - 1);
                                            LogToFile($"[BIMAuthService.LoginAsync] ✅ Buttons extraído manualmente: length={buttonsString.Length}");
                                        }
                                    }
                                }
                            }
                            
                            if (!string.IsNullOrWhiteSpace(buttonsString))
                            {
                                buttons = PremiumButtonInfo.ParseFromString(buttonsString);
                                LogToFile($"[BIMAuthService.LoginAsync] ✅ Parseados {buttons.Count} botones:");
                                foreach (var btn in buttons)
                                {
                                    LogToFile($"  - {btn.name} ({btn.company})");
                                }
                            }
                            else
                            {
                                LogToFile($"[BIMAuthService.LoginAsync] ⚠️ userData.Buttons está vacío o null");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"[BIMAuthService.LoginAsync] ❌ Error extrayendo userData.Buttons: {ex.GetType().Name} - {ex.Message}");
                    }

                    if (ok)
                    {
                        // Guardar token y usuario
                        _currentToken = token;
                        _currentUser = new UserInfo
                        {
                            Usuario = usuario,
                            Plan = plan
                        };

                        // Persistir token de forma segura
                        SaveToken(token, usuario, plan);

                        LogToFile($"[BIMAuthService.LoginAsync] ✅ Login exitoso - Botones para retornar: {buttons.Count}");

                        return new LoginResult
                        {
                            Success = true,
                            Message = "Login exitoso",
                            Token = token,
                            User = _currentUser,
                            Buttons = buttons
                        };
                    }
                    else
                    {
                        string errorMsg = jObject["error"]?.Value<string>() ?? "Error de autenticación desconocido";
                        LogToFile($"[BIMAuthService.LoginAsync] ❌ Login falló: {errorMsg}");
                        
                        return new LoginResult
                        {
                            Success = false,
                            Message = errorMsg
                        };
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"Error de conexión: {ex.Message}"
                };
            }
            catch (TaskCanceledException)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = "Timeout: El servidor tardó demasiado en responder"
                };
            }
            catch (Exception ex)
            {
                return new LoginResult
                {
                    Success = false,
                    Message = $"Error inesperado: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Valida si el token actual es válido
        /// </summary>
        public async Task<bool> ValidateTokenAsync()
        {
            if (string.IsNullOrEmpty(_currentToken))
                return false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(15);

                    var payload = new
                    {
                        action = "validate",
                        token = _currentToken
                    };

                    var json = JsonConvert.SerializeObject(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(AUTH_SERVER_URL, content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var validationResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseBody);

                    if (validationResponse?.ok != true)
                    {
                        // Token inválido, limpiar sesión
                        Logout();
                        return false;
                    }

                    return true;
                }
            }
            catch
            {
                // En caso de error de red, asumir que el token puede ser válido
                // (no forzar logout por problemas de conectividad)
                return !string.IsNullOrEmpty(_currentToken);
            }
        }

        /// <summary>
        /// Cierra la sesión actual
        /// </summary>
        public void Logout()
        {
            _currentToken = null;
            _currentUser = null;
            ClearSavedToken();
        }

        /// <summary>
        /// Guarda el token de forma segura usando DPAPI
        /// </summary>
        private void SaveToken(string token, string usuario, string plan = "")
        {
            try
            {
                var tokenData = new BIMTokenData
                {
                    Token = token,
                    Usuario = usuario,
                    Plan = plan,
                    SavedAt = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(tokenData);
                var jsonBytes = Encoding.UTF8.GetBytes(json);

                // Encriptar usando DPAPI (Windows Data Protection API)
                var encryptedData = ProtectedData.Protect(jsonBytes, entropy, DataProtectionScope.CurrentUser);

                // Crear directorio si no existe
                var directory = Path.GetDirectoryName(TokenFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Guardar archivo encriptado
                File.WriteAllBytes(TokenFilePath, encryptedData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando token: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga el token guardado si existe
        /// </summary>
        private void LoadSavedToken()
        {
            try
            {
                if (!File.Exists(TokenFilePath))
                    return;

                // Leer archivo encriptado
                var encryptedData = File.ReadAllBytes(TokenFilePath);

                // Desencriptar
                var jsonBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
                var json = Encoding.UTF8.GetString(jsonBytes);

                var tokenData = JsonConvert.DeserializeObject<BIMTokenData>(json);
                
                if (tokenData != null && !string.IsNullOrEmpty(tokenData.Token))
                {
                    _currentToken = tokenData.Token;
                    _currentUser = new UserInfo
                    {
                        Usuario = tokenData.Usuario,
                        Plan = tokenData.Plan
                    };

                    // Validar token en background (no bloquear inicio)
                    Task.Run(async () =>
                    {
                        if (!await ValidateTokenAsync())
                        {
                            System.Diagnostics.Debug.WriteLine("Token guardado inválido, requiere re-login");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando token: {ex.Message}");
            }
        }

        /// <summary>
        /// Elimina el token guardado
        /// </summary>
        private void ClearSavedToken()
        {
            try
            {
                if (File.Exists(TokenFilePath))
                {
                    File.Delete(TokenFilePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error eliminando token: {ex.Message}");
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                string logDir = System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "RoslynCopilot"
                );
                System.IO.Directory.CreateDirectory(logDir);

                string logFile = System.IO.Path.Combine(logDir, "premium-buttons-debug.log");
                string timestamp = System.DateTime.Now.ToString("HH:mm:ss.fff");
                System.IO.File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
                
                System.Diagnostics.Debug.WriteLine(message);
            }
            catch { /* Ignorar errores de logging */ }
        }

        #region Response Classes

        private class LoginResponse
        {
            [JsonProperty("ok")]
            public bool ok { get; set; }
            
            [JsonProperty("token")]
            public string token { get; set; }
            
            [JsonProperty("error")]
            public string error { get; set; }
            
            [JsonProperty("plan")]
            public string plan { get; set; }
            
            // Alternativa: si viene como List<PremiumButtonInfo>
            [JsonProperty("Buttons")]
            public List<PremiumButtonInfo> Buttons { get; set; } = new List<PremiumButtonInfo>();
            
            // Alternativa: si viene como string
            [JsonProperty("buttons_string")]
            public string ButtonsString { get; set; }
            
            // Nueva: userData con los botones dentro
            [JsonProperty("userData")]
            public UserData userData { get; set; }
        }

        private class UserData
        {
            [JsonProperty("usuario")]
            public string usuario { get; set; }
            
            [JsonProperty("nombre")]
            public string nombre { get; set; }
            
            [JsonProperty("Buttons")]
            public string Buttons { get; set; }  // Los botones vienen aquí como string
            
            [JsonProperty("extra")]
            public Dictionary<string, object> extra { get; set; }
        }

        private class ValidationResponse
        {
            public bool ok { get; set; }
        }

        private class BIMTokenData
        {
            public string Token { get; set; }
            public string Usuario { get; set; }
            public string Plan { get; set; }
            public DateTime SavedAt { get; set; }
        }

        #endregion
    }

    #region Public Classes

    /// <summary>
    /// Resultado de un intento de login
    /// </summary>
    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Token { get; set; }
        public UserInfo User { get; set; }
        public List<PremiumButtonInfo> Buttons { get; set; } = new List<PremiumButtonInfo>();
    }

    /// <summary>
    /// Información del usuario autenticado
    /// </summary>
    public class UserInfo
    {
        public string Usuario { get; set; }
        public string Plan { get; set; }
    }

    /// <summary>
    /// Información de botón premium desde el backend
    /// </summary>
    public class PremiumButtonInfo
    {
        public string id { get; set; }          // ID único del script
        public string name { get; set; }        // Nombre del botón
        public string url { get; set; }         // URL a Google Drive con JSON completo
        public string company { get; set; }     // Empresa/Proveedor (para agrupar en UI)

        /// <summary>
        /// Parsea un string en formato "nombre1,url1;nombre2,url2;nombre3,url3,company3" en lista de PremiumButtonInfo
        /// También soporta solo IDs de Google Drive: "nombre1,id1;nombre2,id2"
        /// </summary>
        public static List<PremiumButtonInfo> ParseFromString(string input)
        {
            var result = new List<PremiumButtonInfo>();
            if (string.IsNullOrWhiteSpace(input))
                return result;

            try
            {
                // Dividir por punto y coma (separador de botones)
                var items = input.Split(';');

                foreach (var item in items)
                {
                    var trimmed = item.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                        continue;

                    // Dividir por coma (nombre, url/id, [company])
                    var parts = trimmed.Split(',');
                    if (parts.Length < 2)
                        continue;

                    string name = parts[0].Trim();
                    string urlOrId = parts[1].Trim();
                    string company = parts.Length > 2 ? parts[2].Trim() : "Premium";

                    // Si no comienza con "https", es un ID de Google Drive
                    string finalUrl = urlOrId.StartsWith("https")
                        ? urlOrId
                        : BuildGoogleDriveUrl(urlOrId);

                    result.Add(new PremiumButtonInfo
                    {
                        id = Guid.NewGuid().ToString("N").Substring(0, 12),
                        name = name,
                        url = finalUrl,
                        company = company
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing premium buttons: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Construye URL de descarga directa desde ID de Google Drive
        /// </summary>
        private static string BuildGoogleDriveUrl(string fileId)
        {
            if (string.IsNullOrWhiteSpace(fileId))
                return string.Empty;

            return $"https://drive.usercontent.google.com/u/0/uc?id={fileId.Trim()}&export=download";
        }
    }

    #endregion
}
