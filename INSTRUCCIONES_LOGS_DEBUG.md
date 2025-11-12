# 📋 Sistema de Logs Debug - BIMtegration Copilot

## 🎯 Descripción General

BIMtegration Copilot cuenta con un **sistema integrado de logging** que permite:
- ✅ Registrar eventos del sistema automáticamente
- ✅ Visualizar logs en tiempo real en la UI
- ✅ Debuggear problemas de premium buttons
- ✅ Auditar acciones de usuarios
- ✅ Guardar historial persistente

---

## 📍 Ubicación de los Logs

### En el Sistema de Archivos:
```
C:\Users\[USERNAME]\AppData\Roaming\RoslynCopilot\
└── premium-buttons-debug.log
```

### En la Interfaz:
```
BIMtegration Copilot
  └── ⚙️ Settings (Tab)
       └── 📋 Logs (TextArea)
```

---

## 🔍 Cómo Ver los Logs en la UI

### Paso 1: Abre BIMtegration Copilot en Revit
- Revit → Add-ins → BIMtegration Copilot

### Paso 2: Ve a la pestaña **Settings**
- Busca el botón/pestaña "⚙️ Settings" o "Configuración"

### Paso 3: Encuentra la sección **Logs**
```
┌─────────────────────────────────┐
│  📋 Debug Logs                  │
├─────────────────────────────────┤
│ [14:23:45.200] [BIMLoginWindow] │
│ ✅ Login exitoso - Usuario: ... │
│ [14:23:45.145] [Premium]        │
│ ✓ Cache HIT para Genehmigungen  │
│ [14:23:45.156] [Premium]        │
│ Descargado desde URL...         │
│                                 │
│ (últimas 1000 líneas)           │
└─────────────────────────────────┘
```

### Paso 4: Lee los logs
- El archivo se actualiza automáticamente
- Los logs más recientes aparecen abajo
- Se almacenan las últimas **1000 líneas**

---

## 💻 Cómo Agregar Logs en el Código

### Opción 1: Usar `LogToFile()` en BIMLoginWindow.cs

Si estás en `BIMLoginWindow.cs`, puedes usar directamente:

```csharp
LogToFile($"[MiClase] Mi mensaje de debug");
```

**Ejemplo:**
```csharp
private void MiFunction()
{
    LogToFile("[MiClase.MiFunction] Iniciando proceso...");
    
    try
    {
        var resultado = HacerAlgo();
        LogToFile($"[MiClase.MiFunction] ✅ Éxito: {resultado}");
    }
    catch (Exception ex)
    {
        LogToFile($"[MiClase.MiFunction] ❌ Error: {ex.Message}");
    }
}
```

---

### Opción 2: Crear tu propia función LogToFile

Si necesitas logs en otras clases (como `PremiumButtonsCacheManager.cs`, `BIMAuthService.cs`, etc.), crea una función similar:

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
    catch { /* Ignorar errores de logging */ }
}
```

**Cópiala en la clase donde necesites usar logs.**

---

## 📊 Formato de Logs Recomendado

### Estructura básica:
```
[timestamp] [ClassName.MethodName] mensaje
```

### Patrones útiles:

#### ✅ Éxito:
```csharp
LogToFile($"[BIMLoginWindow] ✅ Login exitoso - Usuario: {usuario}");
LogToFile($"[Premium] ✓ Descarga completada: {count} scripts");
```

#### ❌ Error:
```csharp
LogToFile($"[BIMAuthService] ❌ Autenticación falló: {ex.Message}");
LogToFile($"[Premium] Error: {ex.GetType().Name} - {ex.Message}");
```

#### ⚠️ Advertencia:
```csharp
LogToFile($"[Premium] ⚠️ Reintentos agotados para: {buttonName}");
LogToFile($"[Cache] ⚠️ Archivo no encontrado en caché");
```

#### ℹ️ Información:
```csharp
LogToFile($"[Premium] Iniciando descarga desde: {url}");
LogToFile($"[ScriptPanel] ℹ️ Se detectaron {count} botones premium");
```

#### 📊 Datos:
```csharp
LogToFile($"[Premium] JSON Preview: {json.Substring(0, 200)}...");
LogToFile($"[Premium] Code length: {script?.Code?.Length ?? 0} caracteres");
```

---

## 🎨 Usando Emojis para Mejor Visualización

### Recomendados:
- ✅ `✅` - Operación exitosa
- ❌ `❌` - Error / Fallo
- ⚠️ `⚠️` - Advertencia / Precaución
- ℹ️ `ℹ️` - Información
- 📁 `📁` - Archivos
- 🔍 `🔍` - Búsqueda / Análisis
- 📊 `📊` - Datos / Estadísticas
- 🔄 `🔄` - Proceso / Iteración
- 💾 `💾` - Guardado
- 🚀 `🚀` - Inicio / Lanzamiento
- ⏳ `⏳` - En progreso / Esperando

---

## 📝 Ejemplos Prácticos

### Ejemplo 1: Logging en Descargas de Premium Buttons

```csharp
// En PremiumButtonsCacheManager.cs
private static async Task<ScriptDefinition> DownloadFromUrlAsync(string url)
{
    LogToFile($"[Premium] ⏳ Iniciando descarga desde: {url}");
    
    try
    {
        using (var client = new HttpClient())
        {
            var json = await client.GetStringAsync(url);
            LogToFile($"[Premium] ✅ Descarga completada ({json.Length} bytes)");
            
            var script = JsonConvert.DeserializeObject<ScriptDefinition>(json);
            LogToFile($"[Premium] 📊 Script: {script.Name}, Código: {script.Code?.Length ?? 0} chars");
            
            return script;
        }
    }
    catch (Exception ex)
    {
        LogToFile($"[Premium] ❌ Error en descarga: {ex.GetType().Name} - {ex.Message}");
        throw;
    }
}
```

### Ejemplo 2: Logging en Autenticación

```csharp
// En BIMAuthService.cs
public async Task<LoginResult> LoginAsync(string usuario, string clave)
{
    LogToFile($"[BIMAuthService] ⏳ Intentando login para: {usuario}");
    
    try
    {
        var response = await client.PostAsync(AUTH_SERVER_URL, content);
        var responseBody = await response.Content.ReadAsStringAsync();
        
        LogToFile($"[BIMAuthService] ✅ Respuesta recibida: {responseBody.Length} bytes");
        
        var jObject = JObject.Parse(responseBody);
        bool ok = jObject["ok"]?.Value<bool>() ?? false;
        
        if (ok)
        {
            LogToFile($"[BIMAuthService] ✅ Login exitoso - Usuario: {usuario}");
            return new LoginResult { Success = true };
        }
        else
        {
            LogToFile($"[BIMAuthService] ❌ Credenciales inválidas");
            return new LoginResult { Success = false };
        }
    }
    catch (Exception ex)
    {
        LogToFile($"[BIMAuthService] ❌ Excepción: {ex.GetType().Name} - {ex.Message}");
        throw;
    }
}
```

### Ejemplo 3: Logging en Ejecución de Scripts

```csharp
// En ScriptPanel.xaml.cs
private async Task ExecuteScript(ScriptDefinition script)
{
    LogToFile($"[ScriptPanel] ⏳ Ejecutando script: {script.Name}");
    
    try
    {
        var result = await ExecuteRoslynScript(script.Code);
        LogToFile($"[ScriptPanel] ✅ Script ejecutado - Resultado: {result}");
    }
    catch (Exception ex)
    {
        LogToFile($"[ScriptPanel] ❌ Error ejecutando {script.Name}: {ex.Message}");
    }
}
```

---

## 🔧 Troubleshooting

### P: ¿Dónde está el archivo de log?
**R:** En `C:\Users\[USERNAME]\AppData\Roaming\RoslynCopilot\premium-buttons-debug.log`

### P: ¿Por qué no veo logs en la UI?
**R:** 
1. Abre la pestaña **Settings** en BIMtegration
2. Asegúrate de que has usado al menos una vez el login o premium buttons
3. El archivo de log se crea la primera vez que se ejecuta `LogToFile()`

### P: ¿Se borran los logs automáticamente?
**R:** No. El archivo crece indefinidamente. Si es muy grande, puedes:
- Borrarlo manualmente
- O modificar el código para rotar logs (máx 1000 líneas)

### P: ¿Cómo agrego logs a mi script personalizado?
**R:** Los scripts personalizados (en "Crear Script") se ejecutan vía Roslyn. Para loguear desde un script, necesitarías:
1. Exponer la función `LogToFile()` como variable global
2. O registrar logs post-ejecución en la función que llama

---

## 📚 Variables Disponibles en Logs

Cuando escribes logs, tienes acceso a:

```csharp
// Información del timestamp
DateTime.Now.ToString("HH:mm:ss.fff")  // [14:23:45.200]

// Información del contexto
nameof(MiClase)                        // "MiClase"
GetType().Name                         // "MiClase"

// Información del error
ex.GetType().Name                      // "HttpRequestException"
ex.Message                             // "The connection was reset"
ex.StackTrace                          // Stack trace completo
ex.InnerException?.Message             // Excepciones anidadas
```

---

## 🚀 Best Practices

✅ **HACER:**
- Loguear al inicio de funciones importantes
- Incluir valores relevantes: nombres, URLs, tamaños
- Usar emojis para categorizar tipos de eventos
- Loguear errores con el tipo de excepción

❌ **NO HACER:**
- Loguear datos sensibles (contraseñas, tokens)
- Crear logs en loops (pueden saturar el archivo)
- Loguear objetos muy grandes sin limitar
- Ignorar excepciones en `catch` sin loguear

---

## 📄 Archivo de Log Ejemplo

```
[14:23:45.200] [BIMLoginWindow] ⏳ Intentando login para: juan@empresa.com
[14:23:45.215] [BIMAuthService] ⏳ Enviando credenciales a servidor...
[14:23:45.450] [BIMAuthService] ✅ Respuesta recibida: 2847 bytes
[14:23:45.451] [BIMAuthService] 📊 Plan detectado: PREMIUM
[14:23:45.452] [BIMLoginWindow] ✅ Login exitoso - Usuario: juan@empresa.com
[14:23:45.453] [BIMLoginWindow] Plan: PREMIUM
[14:23:45.454] [BIMLoginWindow] Botones premium recibidos: 4
[14:23:45.455] [BIMLoginWindow]   - Genehmigungen (Empresa: METRIKA 360)
[14:23:45.456] [BIMLoginWindow]   - Elemente Nummerieren (Empresa: METRIKA 360)
[14:23:45.500] [PremiumButtonsCacheManager] ⏳ Iniciando descarga de 4 botones premium
[14:23:45.501] [Premium] ⏳ Procesando botón: Genehmigungen (ID: btn_001)
[14:23:45.502] [Premium] Cache MISS - iniciando descarga
[14:23:45.750] [Premium] ✅ Descarga completada (128567 bytes)
[14:23:45.751] [Premium] ✓ Estructura envuelta detectada y deserializada
[14:23:45.752] [Premium] 📊 Script: Genehmigungen, Código: 45823 chars
[14:23:46.100] [PremiumButtonsCacheManager] ✅ Descarga completada: 4 exitosas, 0 con error
[14:23:50.000] [ScriptPanel] ⏳ Ejecutando script: Genehmigungen
[14:23:50.100] [ScriptPanel] ✅ Script ejecutado - Resultado: ✔ Script sent
```

---

## 🎓 Resumen Rápido

| Necesito... | Usa... | Ejemplo |
|------------|--------|---------|
| Loguear éxito | `✅` | `LogToFile($"[Clase] ✅ Operación completada");` |
| Loguear error | `❌` | `LogToFile($"[Clase] ❌ Error: {ex.Message}");` |
| Loguear progreso | `⏳` | `LogToFile($"[Clase] ⏳ Procesando...");` |
| Loguear datos | `📊` | `LogToFile($"[Clase] 📊 Total: {count} items");` |
| Loguear advertencia | `⚠️` | `LogToFile($"[Clase] ⚠️ Precaución: {msg}");` |

---

**¡Ahora tienes todo lo que necesitas para debuggear BIMtegration Copilot! 🎯**
