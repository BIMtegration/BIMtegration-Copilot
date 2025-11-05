# 🔐 Sistema de Autenticación BIMtegration

## 📋 Resumen General

Sistema completo de autenticación que conecta el addon de Revit con un backend de Google Apps Script para validar usuarios y controlar acceso a funciones premium.

---

## 🌐 Backend - Google Apps Script

### URL del Servidor
```
https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec
```

### Endpoints Disponibles

#### 1. LOGIN - Autenticar Usuario

**Request:**
```http
POST /exec
Content-Type: application/json

{
  "action": "login",
  "usuario": "email@ejemplo.com",
  "clave": "contraseña123"
}
```

**Response Exitoso:**
```json
{
  "ok": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "error": "",
  "plan": "premium"
}
```

**Response Error:**
```json
{
  "ok": false,
  "token": "",
  "error": "Usuario o contraseña incorrectos",
  "plan": ""
}
```

---

#### 2. VALIDATE - Validar Token

**Request:**
```http
POST /exec
Content-Type: application/json

{
  "action": "validate",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Response Token Válido:**
```json
{
  "ok": true
}
```

**Response Token Inválido:**
```json
{
  "ok": false
}
```

---

## 💻 Implementación en C# (.NET)

### Ejemplo Completo de Login

```csharp
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public async Task<LoginResult> LoginAsync(string usuario, string clave)
{
    const string AUTH_SERVER_URL = "https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec";
    
    try
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(30);

            // Crear payload
            var payload = new
            {
                action = "login",
                usuario = usuario,
                clave = clave
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Hacer POST request
            var response = await client.PostAsync(AUTH_SERVER_URL, content);
            response.EnsureSuccessStatusCode();

            // Parsear respuesta
            var responseBody = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseBody);

            if (loginResponse?.ok == true)
            {
                return new LoginResult
                {
                    Success = true,
                    Message = "Login exitoso",
                    Token = loginResponse.token,
                    Plan = loginResponse.plan
                };
            }
            else
            {
                return new LoginResult
                {
                    Success = false,
                    Message = loginResponse?.error ?? "Error desconocido"
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
            Message = $"Error: {ex.Message}"
        };
    }
}

// Clases de respuesta
private class LoginResponse
{
    public bool ok { get; set; }
    public string token { get; set; }
    public string error { get; set; }
    public string plan { get; set; }
}

public class LoginResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public string Token { get; set; }
    public string Plan { get; set; }
}
```

---

### Ejemplo de Validación de Token

```csharp
public async Task<bool> ValidateTokenAsync(string token)
{
    const string AUTH_SERVER_URL = "https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec";
    
    try
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(15);

            // Crear payload de validación
            var payload = new
            {
                action = "validate",
                token = token
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Hacer POST request
            var response = await client.PostAsync(AUTH_SERVER_URL, content);
            response.EnsureSuccessStatusCode();

            // Parsear respuesta
            var responseBody = await response.Content.ReadAsStringAsync();
            var validationResponse = JsonConvert.DeserializeObject<ValidationResponse>(responseBody);

            return validationResponse?.ok == true;
        }
    }
    catch (HttpRequestException)
    {
        // Error de red - asumir token válido para no bloquear al usuario
        return true;
    }
    catch (TaskCanceledException)
    {
        // Timeout - asumir token válido
        return true;
    }
    catch
    {
        return false;
    }
}

private class ValidationResponse
{
    public bool ok { get; set; }
}
```

---

## 🔒 Almacenamiento Seguro de Tokens

### Usando DPAPI (Windows Data Protection API)

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

public class TokenStorage
{
    private static readonly string TokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoslynCopilot",
        "bim_auth.dat"
    );
    
    private static readonly byte[] entropy = Encoding.UTF8.GetBytes("BIMtegration2025");

    // Guardar token encriptado
    public void SaveToken(string token, string usuario, string plan)
    {
        var tokenData = new
        {
            Token = token,
            Usuario = usuario,
            Plan = plan,
            SavedAt = DateTime.UtcNow
        };

        var json = JsonConvert.SerializeObject(tokenData);
        var jsonBytes = Encoding.UTF8.GetBytes(json);

        // Encriptar con DPAPI
        var encryptedData = ProtectedData.Protect(
            jsonBytes, 
            entropy, 
            DataProtectionScope.CurrentUser
        );

        // Crear directorio si no existe
        var directory = Path.GetDirectoryName(TokenFilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Guardar archivo
        File.WriteAllBytes(TokenFilePath, encryptedData);
    }

    // Cargar token encriptado
    public TokenData LoadToken()
    {
        if (!File.Exists(TokenFilePath))
            return null;

        try
        {
            // Leer archivo
            var encryptedData = File.ReadAllBytes(TokenFilePath);

            // Desencriptar con DPAPI
            var jsonBytes = ProtectedData.Unprotect(
                encryptedData, 
                entropy, 
                DataProtectionScope.CurrentUser
            );
            
            var json = Encoding.UTF8.GetString(jsonBytes);
            return JsonConvert.DeserializeObject<TokenData>(json);
        }
        catch
        {
            return null;
        }
    }

    // Eliminar token
    public void DeleteToken()
    {
        if (File.Exists(TokenFilePath))
        {
            File.Delete(TokenFilePath);
        }
    }
}

public class TokenData
{
    public string Token { get; set; }
    public string Usuario { get; set; }
    public string Plan { get; set; }
    public DateTime SavedAt { get; set; }
}
```

---

## 🚀 Flujo Completo de Autenticación

### 1. Login Inicial
```
Usuario ingresa credenciales
    ↓
POST /exec con {action: "login", usuario, clave}
    ↓
Servidor valida en base de datos
    ↓
Retorna {ok: true, token: "...", plan: "premium"}
    ↓
Cliente guarda token encriptado en disco
    ↓
Cliente desbloquea funciones premium
```

### 2. Inicio de Sesión Automático
```
Usuario abre el addon
    ↓
Cliente carga token guardado del disco
    ↓
POST /exec con {action: "validate", token: "..."}
    ↓
Si ok: true → Sesión restaurada
Si ok: false → Requiere re-login
```

### 3. Logout
```
Usuario hace clic en logout
    ↓
Cliente elimina token del disco
    ↓
Cliente bloquea funciones premium
```

---

## 📝 Notas Importantes

### ✅ Buenas Prácticas

1. **Timeout**: Siempre usar timeout de 15-30 segundos para evitar bloqueos
2. **Try-Catch**: Manejar excepciones de red (HttpRequestException, TaskCanceledException)
3. **Background Validation**: Validar tokens en background al iniciar para no bloquear UI
4. **Fail-Safe**: Si hay error de red al validar, asumir token válido (no bloquear usuario)
5. **DPAPI**: Usar DataProtectionScope.CurrentUser para encriptación por usuario

### ⚠️ Consideraciones de Seguridad

- **No guardar contraseñas**: Solo guardar tokens
- **DPAPI**: Los tokens encriptados solo pueden ser leídos por el usuario de Windows que los creó
- **Ubicación**: Guardar en AppData (no en carpeta del programa)
- **Validación periódica**: Revalidar tokens ocasionalmente

### 🔧 Troubleshooting

**Error: "Could not connect to server"**
- Verificar conexión a internet
- Verificar que la URL del servidor sea correcta
- Verificar firewall/proxy

**Error: "Token invalid"**
- Token expiró o fue revocado
- Usuario necesita hacer re-login

**Error: "Timeout"**
- Servidor tardó demasiado
- Aumentar timeout o reintenta

---

## 📚 Referencias

### Paquetes NuGet Necesarios
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
<PackageReference Include="System.Net.Http" Version="4.3.4" />
```

### Namespaces Requeridos
```csharp
using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
```

---

## 🎯 Ejemplo de Uso Completo en el Addon

```csharp
// En ScriptPanel.xaml.cs

private BIMAuthService _bimAuthService;

public ScriptPanel()
{
    // Inicializar servicio (carga token automáticamente)
    _bimAuthService = new BIMAuthService();
    
    InitializeUI();
    
    // Actualizar estado de botones según autenticación
    UpdateAuthDependentFeatures();
}

private void LoginButton_Click(object sender, EventArgs e)
{
    // Abrir ventana de login
    var loginWindow = new BIMLoginWindow(_bimAuthService);
    loginWindow.ShowDialog();
    
    if (loginWindow.LoginSuccessful)
    {
        var user = loginWindow.AuthenticatedUser;
        MessageBox.Show($"Bienvenido {user.Usuario}!\nPlan: {user.Plan}");
        UpdateAuthDependentFeatures();
    }
}

private void UpdateAuthDependentFeatures()
{
    bool isAuthenticated = _bimAuthService.IsAuthenticated;
    
    // Habilitar/deshabilitar funciones premium
    _advancedTab.IsEnabled = isAuthenticated;
    _aiModelingTab.IsEnabled = isAuthenticated;
    _addScriptButton.IsEnabled = isAuthenticated;
    _exportButton.IsEnabled = isAuthenticated;
}
```

---

## 📞 Contacto y Soporte

Para modificaciones en el backend (Google Apps Script) o cambios en la estructura de respuestas, contactar al administrador del sistema.

**Última actualización:** Octubre 2025
