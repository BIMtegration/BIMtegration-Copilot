# 🔧 Guía Técnica - Integración de LogToFile en Clases BIMtegration

## 📋 Resumen de Clases que Usan Logs

| Clase | Archivo | Logs Actuales | Necesita Función |
|-------|---------|---------------|------------------|
| `BIMLoginWindow` | `BIMLoginWindow.cs` | ✅ Sí (tiene `LogToFile`) | ❌ No |
| `PremiumButtonsCacheManager` | `PremiumButtonsCacheManager.cs` | ✅ Sí | ❌ No |
| `BIMAuthService` | `BIMAuthService.cs` | ✅ Sí | ❌ No |
| `ScriptPanel` | `ScriptPanel.xaml.cs` | ⚠️ Parcial | ✅ Necesita |

---

## 🎯 Paso a Paso: Agregar LogToFile a Cualquier Clase

### Paso 1: Copia la Función

Agrega esta función **al final de tu clase** (antes del cierre de llaves):

```csharp
/// <summary>
/// Registra un mensaje en el archivo de debug log
/// </summary>
private static void LogToFile(string message)
{
    try
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot"
        );
        Directory.CreateDirectory(logDir);

        string logFile = Path.Combine(logDir, "premium-buttons-debug.log");
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
        
        System.Diagnostics.Debug.WriteLine(message);
    }
    catch { /* Ignorar errores de logging */ }
}
```

### Paso 2: Usa LogToFile en tu Código

Ahora puedes llamar a `LogToFile()` en cualquier método de la clase:

```csharp
public void MiMetodo()
{
    LogToFile($"[MiClase.MiMetodo] Iniciando...");
    
    try
    {
        // Tu código aquí
        LogToFile($"[MiClase.MiMetodo] ✅ Éxito");
    }
    catch (Exception ex)
    {
        LogToFile($"[MiClase.MiMetodo] ❌ Error: {ex.Message}");
    }
}
```

### Paso 3: Visualiza en Settings

- Abre BIMtegration Copilot
- Ve a **Settings** → **Logs**
- Verás los logs en tiempo real

---

## 📍 Ubicaciones Recomendadas para Agregar Logs

### 1. En BIMLoginWindow.cs ✅ (YA HECHO)

```csharp
private async void LoginButton_Click(object sender, RoutedEventArgs e)
{
    LogToFile($"[BIMLoginWindow] ⏳ Iniciando login...");
    // ... resto del código
    LogToFile($"[BIMLoginWindow] ✅ Login completado");
}
```

### 2. En ScriptPanel.xaml.cs ⚠️ (AGREGAR)

**Ubicación:** Método `ExecuteScript()` - línea 1757

```csharp
private async Task ExecuteScript(ScriptDefinition script)
{
    LogToFile($"[ScriptPanel] ⏳ Ejecutando: {script.Name}");
    
    try
    {
        var result = await ExecuteRoslynScript(script.Code);
        LogToFile($"[ScriptPanel] ✅ {script.Name} completado");
    }
    catch (Exception ex)
    {
        LogToFile($"[ScriptPanel] ❌ Error en {script.Name}: {ex.Message}");
    }
}
```

### 3. En BIMAuthService.cs ✅ (YA HECHO)

**Ubicación:** Método `LoginAsync()` - línea 45

```csharp
public async Task<LoginResult> LoginAsync(string usuario, string clave)
{
    LogToFile($"[BIMAuthService] ⏳ Login para: {usuario}");
    // ... código
}
```

### 4. En PremiumButtonsCacheManager.cs ✅ (YA HECHO)

**Ubicación:** Método `DownloadFromUrlAsync()` - línea 280

```csharp
private static async Task<ScriptDefinition> DownloadFromUrlAsync(string url)
{
    LogToFile($"[Premium] ⏳ Descargando: {url}");
    // ... código
}
```

---

## 🎨 Patrones de Logging por Módulo

### Patrón: Premium Buttons

```csharp
// Inicio
LogToFile($"[Premium] ⏳ Iniciando descarga de {count} botones");

// Progreso
LogToFile($"[Premium] ℹ️ Procesando botón {i}/{total}: {buttonName}");

// Éxito
LogToFile($"[Premium] ✅ Descarga completada: {successCount} exitosas");

// Error
LogToFile($"[Premium] ❌ Error en {buttonName}: {ex.Message}");
```

### Patrón: Autenticación

```csharp
// Intento
LogToFile($"[BIMAuthService] ⏳ Enviando credenciales a {url}");

// Respuesta
LogToFile($"[BIMAuthService] ✅ Respuesta: {response.StatusCode}");

// Datos
LogToFile($"[BIMAuthService] 📊 Usuario: {usuario}, Plan: {plan}");

// Error
LogToFile($"[BIMAuthService] ❌ Autenticación fallida: {error}");
```

### Patrón: Ejecución de Scripts

```csharp
// Inicio
LogToFile($"[ScriptPanel] ⏳ Ejecutando script: {script.Name}");

// Etapas
LogToFile($"[ScriptPanel] 📊 Código: {script.Code.Length} caracteres");
LogToFile($"[ScriptPanel] 🔄 Compilando con Roslyn...");

// Resultado
LogToFile($"[ScriptPanel] ✅ Script completado. Resultado: {result}");

// Error
LogToFile($"[ScriptPanel] ❌ Error en línea {lineNumber}: {errorMsg}");
```

---

## 🔍 Debugging Común

### Caso 1: Problema con Premium Buttons No Descargables

```csharp
// En DownloadPremiumButtonsAsync()
LogToFile($"[PremiumButtonsCacheManager] 🔍 Verificando caché para: {buttonId}");
LogToFile($"[PremiumButtonsCacheManager] 📁 Ruta de caché: {cachePath}");
LogToFile($"[PremiumButtonsCacheManager] 📊 Archivos en caché: {cacheFiles.Length}");

if (cached)
{
    LogToFile($"[PremiumButtonsCacheManager] ✅ Cargado desde caché");
}
else
{
    LogToFile($"[PremiumButtonsCacheManager] 🌐 Descargando desde: {url}");
}
```

### Caso 2: Problema con Script que No Ejecuta

```csharp
// En ExecuteScript()
LogToFile($"[ScriptPanel] 🔍 Validando script: {script.Name}");
LogToFile($"[ScriptPanel] ✓ Código presente: {!string.IsNullOrEmpty(script.Code)}");
LogToFile($"[ScriptPanel] ✓ UIApplication disponible: {uiApp != null}");
LogToFile($"[ScriptPanel] ✓ Documento abierto: {uiApp?.ActiveUIDocument?.Document != null}");

if (uiApp == null)
{
    LogToFile($"[ScriptPanel] ❌ UIApplication no disponible");
    return;
}

LogToFile($"[ScriptPanel] 🚀 Iniciando ejecución de Roslyn");
```

### Caso 3: Problema con Login Fallido

```csharp
// En LoginAsync()
LogToFile($"[BIMAuthService] 🔍 Preparando payload...");
LogToFile($"[BIMAuthService] 📊 Usuario: {usuario}");
LogToFile($"[BIMAuthService] 📊 URL del servidor: {AUTH_SERVER_URL}");

var response = await client.PostAsync(AUTH_SERVER_URL, content);
LogToFile($"[BIMAuthService] 📊 Status Code: {response.StatusCode}");
LogToFile($"[BIMAuthService] 📊 Respuesta length: {responseBody.Length}");

if (response.StatusCode != System.Net.HttpStatusCode.OK)
{
    LogToFile($"[BIMAuthService] ❌ Servidor retornó: {response.StatusCode}");
}
```

---

## 📊 Ejemplos de Salida en Settings

### Sesión Exitosa:
```
[14:23:45.200] [BIMLoginWindow] ⏳ Intentando login para: usuario@empresa.com
[14:23:45.450] [BIMAuthService] ✅ Respuesta: OK
[14:23:45.451] [BIMAuthService] 📊 Plan: PREMIUM
[14:23:45.500] [PremiumButtonsCacheManager] ⏳ Iniciando descarga de 4 botones
[14:23:46.100] [PremiumButtonsCacheManager] ✅ Descarga completada: 4 exitosas
```

### Sesión con Errores:
```
[14:23:45.200] [BIMAuthService] ⏳ Enviando credenciales...
[14:23:45.450] [BIMAuthService] 📊 Status Code: Unauthorized
[14:23:45.451] [BIMAuthService] ❌ Error: 401 Unauthorized
[14:23:45.500] [BIMLoginWindow] ❌ Login falló: Credenciales inválidas
```

---

## 🚀 Checklist: Agregar Logs a una Nueva Clase

- [ ] Copiar función `LogToFile()` al final de la clase
- [ ] Agregar `using System.IO;` si no está presente
- [ ] Agregar `using System.Diagnostics;` si usas `Debug.WriteLine`
- [ ] Loguear inicio de métodos principales
- [ ] Loguear valores importantes (URLs, ids, etc.)
- [ ] Loguear errores con `ex.Message` y `ex.GetType().Name`
- [ ] Usar emojis para categorizar
- [ ] Probar en Settings → Logs
- [ ] Verificar que aparezcan en `premium-buttons-debug.log`

---

## 💡 Tips Profesionales

### Tip 1: Loguea Cambios de Estado
```csharp
LogToFile($"[ScriptPanel] Estado anterior: {currentState} → Nuevo: {newState}");
```

### Tip 2: Loguea Tiempos de Ejecución
```csharp
var start = DateTime.Now;
// ... código
var duration = (DateTime.Now - start).TotalMilliseconds;
LogToFile($"[Premium] ✅ Descarga completada en {duration:F0}ms");
```

### Tip 3: Loguea Estadísticas
```csharp
LogToFile($"[PremiumButtons] 📊 Estadísticas: Total={total}, Exitosos={success}, Fallos={failed}");
```

### Tip 4: Loguea Contexto Completo
```csharp
LogToFile($"[ScriptPanel] Contexto: Usuario={usuario}, Script={script}, Versión={version}");
```

### Tip 5: Loguea Puntos de Decisión
```csharp
if (condition)
{
    LogToFile($"[MyClass] 🔀 Rama tomada: Opción A");
    // ... código
}
else
{
    LogToFile($"[MyClass] 🔀 Rama tomada: Opción B");
    // ... código
}
```

---

## 🔗 Referencias Rápidas

**Función completa:**
```csharp
private static void LogToFile(string message)
{
    try
    {
        string logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot"
        );
        Directory.CreateDirectory(logDir);
        string logFile = Path.Combine(logDir, "premium-buttons-debug.log");
        string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        File.AppendAllText(logFile, $"[{timestamp}] {message}\n");
        System.Diagnostics.Debug.WriteLine(message);
    }
    catch { }
}
```

**Llamada simple:**
```csharp
LogToFile($"[MiClase] ✅ Mensaje aquí");
```

**Llamada con variables:**
```csharp
LogToFile($"[MiClase] 📊 Usuario: {usuario}, Resultado: {resultado}");
```

**Llamada con excepciones:**
```csharp
LogToFile($"[MiClase] ❌ {ex.GetType().Name}: {ex.Message}");
```

---

**¡Ahora puedes debuggear cualquier módulo de BIMtegration Copilot! 🎯**
