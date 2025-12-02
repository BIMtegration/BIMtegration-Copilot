## 2. USO DE NAVEGADOR CLÁSICO (WebBrowser) Y CATÁLOGO ONLINE

Puedes mostrar páginas web, catálogos online y descargar familias directamente desde scripts Copilot usando el control clásico WebBrowser de WinForms.

### Ejemplo: Mostrar catálogo online
```csharp
using System.Windows.Forms;

var form = new Form();
form.Text = "Catálogo Online";
form.Width = 900;
form.Height = 600;

var browser = new WebBrowser();
browser.Dock = DockStyle.Fill;
browser.Url = new System.Uri("https://tucatalogo.com"); // Cambia por la URL de tu catálogo
form.Controls.Add(browser);

form.ShowDialog();

return "✅ Catálogo mostrado";
```

### Ejemplo: Insertar familia desde URL
```csharp
using System.Windows.Forms;
using System.Net;

string familyUrl = Microsoft.VisualBasic.Interaction.InputBox("URL de la familia .rfa:", "Insertar Familia", "https://tucatalogo.com/familia.rfa");
string localPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "familia_temp.rfa");

using (var client = new WebClient())
{
    client.DownloadFile(familyUrl, localPath);
}

Autodesk.Revit.DB.Family family;
if (doc.LoadFamily(localPath, out family))
{
    MessageBox.Show("✅ Familia insertada correctamente.");
}
else
{
    MessageBox.Show("❌ Error al insertar la familia.");
}

return "✅ Proceso de inserción finalizado";
```

Puedes combinar ambos ejemplos para crear flujos interactivos con catálogos web y automatizar la inserción de familias en Revit.
# INSTRUCCIONES COMPLETAS PARA GENERACIÓN DE CÓDIGO EN BIMTEGRATION COPILOT

## 1. CONTEXTO Y VARIABLES DISPONIBLES

BIMtegration Copilot ejecuta scripts C# compilados dinámicamente con Roslyn, dentro de una transacción de Revit.

### Variables de contexto automáticas
**No declares ni redefinas estas variables:**

- `doc` : `Autodesk.Revit.DB.Document` - Documento activo de Revit
- `uidoc` : `Autodesk.Revit.UI.UIDocument` - Interfaz de usuario y selección
- `app` : `Autodesk.Revit.ApplicationServices.Application` - Aplicación de Revit
- `uiapp` : `Autodesk.Revit.UI.UIApplication` - UI de aplicación

**Ejemplos de uso:**
```csharp
// Obtener elementos
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();

// Trabajar con selección
var seleccion = uidoc.Selection.GetElementIds();

// Mostrar diálogos
TaskDialog.Show("Info", "Script ejecutado correctamente");

// Acceder a propiedades
string nombreProyecto = doc.Title;
string version = app.VersionName;
```

**Referencias ya cargadas:**
- System.Net.Http, System.IO, System.Linq
- OfficeOpenXml, CsvHelper, Newtonsoft.Json
- Autodesk.Revit.DB, Autodesk.Revit.UI

**Reglas críticas:**
- No declares transacciones manuales
- No configures encoding ni licencias
- No sobrescribas las variables de contexto

## 2. ESTRUCTURA BÁSICA DEL SCRIPT

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
// ... otros usings según necesidad

try {
    // Lógica principal usando doc, uidoc, app, uiapp
    var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();
    TaskDialog.Show("Resultado", $"Se encontraron {walls.Count} muros");
    return $"✅ Proceso completado: {walls.Count} elementos procesados";
} catch (Exception ex) {
    TaskDialog.Show("Error", ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

## 3. OPERACIONES ASÍNCRONAS (async/await)

**Cuándo usar:**
- Peticiones HTTP (APIs, descargas)
- Lectura/escritura de archivos grandes
- Cálculos o procesos largos

**Ejemplo correcto:**
```csharp
using System.Net.Http;
try {
    using (var client = new HttpClient()) {
        string url = "https://api.ejemplo.com/data";
        string response = await client.GetStringAsync(url); // ✅ ASÍNCRONO
        TaskDialog.Show("Resultado", response);
        return "✅ Petición completada";
    }
} catch (Exception ex) {
    return $"❌ Error: {ex.Message}";
}
```

**Errores a evitar:**
- ❌ No uses `.Result` ni `.Wait()` (congelan Revit)
- ❌ No mezcles código síncrono y asíncrono incorrectamente

## 4. BUENAS PRÁCTICAS DE VALIDACIÓN

**Validar ruta de archivo:**
```csharp
string ruta = form.FilePath;
if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
    return "❌ La ruta del archivo no es válida o el archivo no existe.";
```

**Validar valor numérico:**
```csharp
int valor;
if (!int.TryParse(form.txtValor.Text, out valor) || valor < 0)
    return "❌ El valor debe ser un número entero positivo.";
```

**Validar selección de elementos:**
```csharp
var seleccion = uidoc.Selection.GetElementIds();
if (seleccion.Count == 0)
    return "❌ Debes seleccionar al menos un elemento.";
```

**Validar parámetros:**
```csharp
Parameter p = el.LookupParameter("NUMERO");
if (p == null)
    return "❌ El parámetro 'NUMERO' no existe en este elemento.";
if (p.IsReadOnly)
    return "❌ El parámetro 'NUMERO' es de solo lectura.";
```

## 5. MANEJO GLOBAL DE ERRORES Y LOGGING

**Manejo básico con try-catch:**
```csharp
try {
    // ... lógica principal ...
    return "✅ Proceso completado";
} catch (Exception ex) {
    TaskDialog.Show("Error", ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

**Logging personalizado:**
```csharp
public static void Log(string mensaje) {
    File.AppendAllText(@"C:\Temp\copilot_log.txt", 
        DateTime.Now + ": " + mensaje + Environment.NewLine);
}

try {
    // ... lógica ...
    Log("Script ejecutado correctamente");
    return "✅ Ok";
} catch (Exception ex) {
    Log("Error: " + ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

## 6. USO DE ExternalEvent.Raise PARA COMMIT DE CAMBIOS

**Para cambios permanentes en el modelo:**
```csharp
// Definir handler y evento
var handler = new GenericExternalEventHandler();
var externalEvent = ExternalEvent.Create(handler);

// Definir acción que modifica el modelo
handler.ActionToExecute = (uiapp) => {
    var doc = uiapp.ActiveUIDocument.Document;
    var el = doc.GetElement(someId);
    var p = el.LookupParameter("NUMERO");
    if (p != null && !p.IsReadOnly)
        p.Set("NuevoValor");
    // ... más lógica ...
};

// Ejecutar en contexto seguro de Revit
externalEvent.Raise();
```

**Con reflexión (si el host lo requiere):**
```csharp
var actionProp = handlerObj.GetType().GetProperty("ActionToExecute");
actionProp.SetValue(handlerObj, (Action<UIApplication>)accion);
var raiseMethod = externalEvent.GetType().GetMethod("Raise");
raiseMethod.Invoke(externalEvent, null);
```

## 7. CLASES INTERNAS PARA FORMULARIOS WINFORMS

**Ejemplo completo en alemán:**
```csharp
using System.Windows.Forms;

public class NumeratorForm : Form
{
    public string Parameter { get; private set; }
    public int StartValue { get; private set; }
    public string Prefix { get; private set; }
    TextBox txtParameter, txtStartValue, txtPrefix;
    
    public NumeratorForm()
    {
        Text = "Elemente nummerieren";
        Width = 300; Height = 200;
        
        Label lbl1 = new Label { Text = "Parameter:", Top = 20, Left = 10, Width = 80 };
        txtParameter = new TextBox { Top = 20, Left = 100, Width = 150, Text = "NUMMER" };
        
        Label lbl2 = new Label { Text = "Startwert:", Top = 60, Left = 10, Width = 80 };
        txtStartValue = new TextBox { Top = 60, Left = 100, Width = 150, Text = "1" };
        
        Label lbl3 = new Label { Text = "Präfix:", Top = 100, Left = 10, Width = 80 };
        txtPrefix = new TextBox { Top = 100, Left = 100, Width = 150, Text = "" };
        
        Button btnOK = new Button { Text = "Nummerieren", Top = 140, Left = 100, Width = 80 };
        btnOK.Click += (s, e) => {
            Parameter = txtParameter.Text.Trim();
            int.TryParse(txtStartValue.Text.Trim(), out int val);
            StartValue = val;
            Prefix = txtPrefix.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        };
        
        Controls.AddRange(new Control[] { lbl1, txtParameter, lbl2, txtStartValue, lbl3, txtPrefix, btnOK });
    }
}

// Uso en script principal
NumeratorForm form = new NumeratorForm();
if (form.ShowDialog() != DialogResult.OK)
    return "Vorgang vom Benutzer abgebrochen.";

string parameter = form.Parameter;
int counter = form.StartValue;
string prefix = form.Prefix;
// ... usar valores en lógica principal ...
```

## 8. CONVERSIÓN DE TIPOS (CAST)

**Casos comunes:**
```csharp
// Element a tipo específico
Element el = doc.GetElement(id);
Wall wall = el as Wall;
if (wall != null) {
    double altura = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0;
}

// FamilyInstance
FamilyInstance fi = el as FamilyInstance;
if (fi != null) {
    string tipo = fi.Symbol.Name;
}

// Parámetros
Parameter p = el.LookupParameter("NUMERO");
string valor = p.AsString() ?? p.AsValueString();

// Listas genéricas
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
```

## 9. INTERNACIONALIZACIÓN Y TRADUCCIÓN

**Con diccionario:**
```csharp
Dictionary<string, string> mensajes = new Dictionary<string, string> {
    { "es", "Proceso completado" },
    { "de", "Prozess abgeschlossen" },
    { "en", "Process completed" }
};
string idioma = "de"; // Seleccionado por usuario
TaskDialog.Show("Info", mensajes[idioma]);
```

**En formularios WinForms:**
```csharp
public class MenuForm : Form {
    public MenuForm(string idioma) {
        if (idioma == "de") {
            Text = "Menü";
            // ... otros controles en alemán
        } else if (idioma == "en") {
            Text = "Menu";
            // ... otros controles en inglés
        }
    }
}
```

## 10. INTEGRACIÓN CON GOOGLE SHEETS API

**Deserialización correcta de JSON:**
```csharp
using Newtonsoft.Json.Linq;

string rawJson = await client.GetStringAsync(url);
var obj = JObject.Parse(rawJson);
if ((string)obj["status"] != "ok")
    return "❌ Error: " + (string)obj["message"];

var data = obj["data"] as JArray;
if (data == null)
    return "❌ No se encontró la propiedad 'data' en la respuesta.";

// Usar data como lista de registros
```

**Envío de datos (POST):**
```csharp
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

var payload = new {
    campo1 = "valor1",
    campo2 = "valor2"
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
var content = new StringContent(json, Encoding.UTF8, "application/json");

using (var client = new HttpClient()) {
    var response = await client.PostAsync("https://tu-endpoint.com/api", content);
    var rawJson = await response.Content.ReadAsStringAsync();
    var obj = JObject.Parse(rawJson);
    if ((string)obj["status"] != "ok")
        return "❌ Error: " + (string)obj["message"];
    return "✅ Datos enviados correctamente";
}
```

## 11. BOTONES PREMIUM

### ¿Qué son los botones premium?

Los **botones premium** son scripts personalizados almacenados en Google Drive que se descargan automáticamente cuando te autentiques con una cuenta premium en BIMtegration Copilot. Se agrupan por empresa y aparecen en la pestaña **Advanced**.

### Características de los botones premium:

- ✅ **Descarga automática**: Se descargan cuando inicias sesión con cuenta premium
- ✅ **Caché local**: Se guardan localmente para no requerir nueva descarga
- ✅ **Grouping**: Organizados por empresa (campo "Company" en metadatos)
- ✅ **Versionamiento**: Cada descarga actualiza la versión local
- ✅ **Descarga manual**: Puedes descargar scripts premium para importar después

### Dónde aparecen:

Los botones premium aparecen en la pestaña **Advanced** bajo secciones por empresa:

```
🔒 BOTONES PREMIUM
  🏢 MiEmpresa
    📌 Script Premium 1  [⏳ cached] [▶️ Run] [💾 Download]
    📌 Script Premium 2  [✓ cached] [▶️ Run] [💾 Download]
  🏢 OtraEmpresa
    📌 Script Premium 3  [❌ Error] [🔄 Retry]
```

**Estados disponibles:**
- `✓ cached` - Script descargado y listo en caché
- `⏳ downloading` - En proceso de descarga
- `❌ Error` - Descarga fallida, mostrar botón de reintentar
- `✓ downloaded` - Descargado en esta sesión

### Formato de configuración

Los botones premium se configuran en **Google Sheets** en formato de metadatos:

```
nombre1,url1;nombre2,url2;nombre3,url3,company3
```

**Campos:**
- `nombre`: Nombre del script (máx 100 caracteres)
- `url`: URL pública de Google Drive del archivo JSON con el script
- `company`: (opcional) Nombre de la empresa para agrupación

**Formato de URL de Google Drive:**
```
https://drive.usercontent.google.com/u/0/uc?id=[FILE_ID]&export=download
```

### Estructura del JSON del script premium

Los scripts premium están alojados como archivos JSON en Google Drive con la siguiente estructura:

```json
{
  "id": "premium-script-001",
  "name": "Exportar a XML",
  "description": "Exporta elementos seleccionados a formato XML",
  "code": "using Autodesk.Revit.DB;...",
  "category": "🔒 [MiEmpresa]",
  "tags": ["export", "xml", "premium"],
  "version": "1.0",
  "author": "Mi Equipo"
}
```

### Descarga manual de botones premium

Si tu suscripción premium expira, puedes descargar botones premium ya cacheados para importarlos manualmente después:

1. Antes de que expire la suscripción, haz clic en **💾 Download** en el botón premium
2. Guarda el archivo JSON en una ubicación segura
3. Después de que expire, en la pestaña **Scripts** → **Import Selection**, carga el archivo JSON
4. El script se añadirá a tu lista local de scripts

### Caché y almacenamiento

- **Ubicación**: `C:\Users\[Usuario]\AppData\Roaming\RoslynCopilot\premium-buttons-cache\`
- **Duración**: Sesión actual de Revit (se limpia al reiniciar Revit)
- **Tamaño**: Depende del número y tamaño de scripts premium (típicamente 1-10 MB)

### Solucionar problemas de descarga

**Si un botón muestra ❌ Error:**

1. Verifica tu conexión a internet
2. Haz clic en **🔄 Retry** para reintentar la descarga
3. Si persiste el error:
   - Reinicia Revit (limpia el caché)
   - Cierra sesión y vuelve a iniciar sesión
   - Contacta al administrador si el problema continúa

**Información de debugging:**
- Abre la Consola de Depuración en Visual Studio (Debug → Windows → Output)
- Busca mensajes con prefijo `[Premium]` para ver detalles de la descarga
- Los errores comunes son: timeout de red, URL inválida, archivo corrupto

## 12. COMPATIBILIDAD FUTURA

**Buenas prácticas:**
- Usa solo APIs públicas y documentadas de Revit
- Evita clases internas o métodos obsoletos
- Valida nombres de parámetros y categorías
- Mantén scripts modulares

**APIs recomendadas:**
```csharp
// ✅ Recomendado
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();

// ✅ Validar parámetros
Parameter p = el.LookupParameter("NUMERO");
if (p == null)
    return "❌ El parámetro no existe";
```

---

**ESTRUCTURA FINAL OBLIGATORIA:**
1. Usar variables de contexto estándar
2. Manejar errores con try-catch
3. Retornar string descriptivo
4. Validar inputs antes de operaciones
5. Usar async/await para operaciones largas
6. Seguir patrones específicos para APIs externas

Estas instrucciones contienen toda la información necesaria para generar código funcional y robusto en BIMtegration Copilot.

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
REGLA DE ORO: PERSISTENCIA Y UI (SHOWDIALOG)
⚠️ IMPORTANTE: Si tu script muestra una ventana modal (form.ShowDialog()), NO uses transacciones manuales (new Transaction) ni SubTransaction directamente en el flujo principal, porque Revit revertirá los cambios (Rollback) al cerrar la ventana.

Debes usar SIEMPRE el siguiente patrón de "Evento Externo por Reflexión":

C#

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using WF = System.Windows.Forms;
using System;

// 1. OBTENER HANDLER POR REFLEXIÓN
var handlerObj = (object)externalEventHandler;
var actionProp = handlerObj.GetType().GetProperty("ActionToExecute");
if (actionProp == null) return "❌ Error: ActionToExecute no encontrado.";

// 2. DEFINIR LA ACCIÓN (Toda tu lógica va aquí dentro)
Action<UIApplication> aktion = (uiapp) =>
{
    var doc = uiapp?.ActiveUIDocument?.Document;
    var uidoc = uiapp?.ActiveUIDocument;

    try
    {
        // A. MOSTRAR UI
        var form = new MiFormulario(doc);
        if (form.ShowDialog() != WF.DialogResult.OK) return;

        // B. EJECUTAR LÓGICA (Revit gestiona la transacción aquí de forma segura)
        // ... tu código de creación/modificación ...
        
        TaskDialog.Show("Éxito", "Elementos creados correctamente.");
    }
    catch (Exception ex)
    {
        TaskDialog.Show("Error", ex.Message);
    }
};

// 3. ASIGNAR Y DISPARAR
actionProp.SetValue(handlerObj, aktion);
var raiseMethod = externalEvent.GetType().GetMethod("Raise");
raiseMethod.Invoke(externalEvent, null);

return "✅ Comando activado correctamente.";

// ... Clases del Formulario abajo ...
3. ESTRUCTURA PARA SCRIPTS SIMPLES (SIN UI)
Si el script NO abre ventanas y es una ejecución directa, puedes usar la estructura simple (la transacción es automática):

## 20. ACCESO AL TOKEN DE AUTENTICACIÓN EN SCRIPTS

**Guardar y acceder al token desde scripts Copilot:**

Los tokens de autenticación se almacenan de forma encriptada en el disco usando DPAPI. Para acceder al token desde tus scripts, debes interactuar con el servicio de autenticación disponible en el contexto.

### Obtener el token actual
```csharp
// El token está disponible a través del servicio de autenticación
// Ruta: %APPDATA%\RoslynCopilot\bim_auth.dat (encriptado)

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

// Función auxiliar para cargar el token desde el almacenamiento
string GetStoredToken()
{
    string tokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoslynCopilot",
        "bim_auth.dat"
    );
    
    if (!File.Exists(tokenFilePath))
        return null;

    try
    {
        byte[] entropy = Encoding.UTF8.GetBytes("BIMtegration2025");
        var encryptedData = File.ReadAllBytes(tokenFilePath);
        var jsonBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(jsonBytes);
        
        dynamic tokenData = JsonConvert.DeserializeObject(json);
        return tokenData.Token;
    }
    catch
    {
        return null;
    }
}

// Usar el token en tu script
var token = GetStoredToken();
if (string.IsNullOrEmpty(token))
{
    return "❌ No hay token guardado. Por favor, auténtica primero.";
}

// Ahora puedes usar el token en peticiones HTTP
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    var response = await client.GetAsync("https://api.bimtegration.com/datos");
    var datos = await response.Content.ReadAsStringAsync();
    return $"✅ Datos obtenidos: {datos}";
}
```

### Guardar datos asociados al token
```csharp
// Obtener información del usuario autenticado
string GetUserInfo()
{
    string tokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoslynCopilot",
        "bim_auth.dat"
    );
    
    if (!File.Exists(tokenFilePath))
        return "No autenticado";

    try
    {
        byte[] entropy = Encoding.UTF8.GetBytes("BIMtegration2025");
        var encryptedData = File.ReadAllBytes(tokenFilePath);
        var jsonBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(jsonBytes);
        
        dynamic tokenData = JsonConvert.DeserializeObject(json);
        return $"Usuario: {tokenData.Usuario}, Plan: {tokenData.Plan}";
    }
    catch
    {
        return "Error al leer datos de autenticación";
    }
}

var userInfo = GetUserInfo();
TaskDialog.Show("Info de Usuario", userInfo);
return $"✅ {userInfo}";
```

### Validar token antes de usar recursos premium
```csharp
// Verificar si el token es válido antes de usar una función premium
async Task<bool> IsTokenValid()
{
    var token = GetStoredToken();
    if (string.IsNullOrEmpty(token))
        return false;

    const string AUTH_SERVER_URL = "https://script.google.com/macros/s/AKfycbwZ9Qki-FSQzRNi_gr_kAMl02Rck78YQ_-6xB3R9nQ8_kFmWpwpKY1DwU-sThpjj2IL/exec";
    
    try
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromSeconds(15);
            var payload = new { action = "validate", token = token };
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            
            var response = await client.PostAsync(AUTH_SERVER_URL, content);
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic validationResponse = JsonConvert.DeserializeObject(responseBody);
            
            return validationResponse?.ok == true;
        }
    }
    catch
    {
        return true; // Fail-safe: asumir válido si hay error de red
    }
}

// Usar antes de función premium
if (!await IsTokenValid())
{
    return "❌ Token inválido o expirado. Por favor, auténtica nuevamente.";
}

// Proceder con función premium
return "✅ Token validado. Procediendo...";
```

### ⚠️ Consideraciones Importantes

1. **Encriptación**: El token está encriptado con DPAPI y solo puede ser leído por el usuario de Windows que lo creó
2. **Ubicación segura**: Se almacena en `%APPDATA%\RoslynCopilot\bim_auth.dat`
3. **Entropía fija**: La entropía es `"BIMtegration2025"` - **no cambiarla**
4. **Validación periódica**: Revalidar tokens ocasionalmente para detectar expiración
5. **Manejo de errores**: Si falla la desencriptación, el usuario necesita re-autenticarse

---

C#

try {
    // Lógica directa
    var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();
    return $"✅ Procesados {walls.Count} muros.";
} catch (Exception ex) {
    return $"❌ Error: {ex.Message}";
}

## 21. DATOS DE EMPRESA (COMPANY DATA)

### ¿Qué son los Company Data?

Los **Company Data** son variables de datos específicas de tu empresa que se descargan automáticamente cuando te autentiques con una cuenta premium. Son datos que generalmente están almacenados en Google Sheets y se vinculan a través de tu perfil de usuario.

**Características:**
- ✅ Descarga automática al hacer login
- ✅ Almacenados en caché durante la sesión
- ✅ Accesibles desde scripts premium
- ✅ Formato: Variables con Sheet ID y Sheet Name
- ✅ Disponibles después de 2-10 segundos desde el login

### Ubicación de Company Data en el Contexto

Después de autenticarte, los datos están disponibles en:

```csharp
// En BIMAuthService - Datos del usuario autenticado
var currentUser = BIMAuthService.CurrentUser;

// Acceder a los datos de empresa
var companyData = currentUser?.CompanyData;  // Dictionary<string, JToken>
var variables = currentUser?.CompanyDataVariables;  // List<CompanyDataVariable>
```

### Estructura de Company Data

El formato de configuración es:
```
variable1,sheetId1,sheetName1;variable2,sheetId2,sheetName2;...
```

**Ejemplo real:**
```
Datenbank,1j02RBg7BdZQAgOhPXM0rRHRI_BNohZuzBDNTYlKZHy4,BDB
```

Este ejemplo define:
- **Variable Name**: `Datenbank`
- **Sheet ID**: `1j02RBg7BdZQAgOhPXM0rRHRI_BNohZuzBDNTYlKZHy4`
- **Sheet Name**: `BDB`

### Acceder a Company Data en Scripts

#### 1. Obtener la lista de variables disponibles

```csharp
// Obtener variables
var currentUser = BIMAuthService.CurrentUser;
if (currentUser?.CompanyDataVariables == null || currentUser.CompanyDataVariables.Count == 0)
{
    return "❌ No hay datos de empresa disponibles. Auténtica primero.";
}

// Listar variables disponibles
string variablesList = string.Join(", ", 
    currentUser.CompanyDataVariables.Select(v => $"{v.VariableName} ({v.Status})"));

return $"✅ Variables disponibles: {variablesList}";
```

#### 2. Acceder a los datos de una variable específica

```csharp
using Newtonsoft.Json.Linq;

var currentUser = BIMAuthService.CurrentUser;
var companyData = currentUser?.CompanyData;

if (companyData == null || companyData.Count == 0)
{
    return "❌ No hay datos de empresa.";
}

// Obtener datos de una variable
if (companyData.TryGetValue("Datenbank", out var databaseData))
{
    // El data es un JToken (puede ser array, objeto, etc.)
    var json = JsonConvert.SerializeObject(databaseData, Formatting.Indented);
    
    // Mostrar primeras 500 caracteres
    string preview = json.Length > 500 
        ? json.Substring(0, 500) + "..." 
        : json;
    
    return $"✅ Datos de Datenbank:\n{preview}";
}
else
{
    return "❌ Variable 'Datenbank' no encontrada.";
}
```

#### 3. Iterar sobre los datos (si es un array)

```csharp
using Newtonsoft.Json.Linq;

var currentUser = BIMAuthService.CurrentUser;
var companyData = currentUser?.CompanyData;

if (companyData?.TryGetValue("Datenbank", out var databaseData) != true)
{
    return "❌ Datos no disponibles.";
}

try
{
    // Si los datos son un array
    if (databaseData is JArray array)
    {
        int count = array.Count;
        return $"✅ {count} registros encontrados en Datenbank";
    }
    else if (databaseData is JObject obj)
    {
        // Si son un objeto
        var keys = obj.Properties().Select(p => p.Name).ToList();
        return $"✅ Objeto con propiedades: {string.Join(", ", keys)}";
    }
    else
    {
        return $"✅ Datos: {databaseData}";
    }
}
catch (Exception ex)
{
    return $"❌ Error procesando datos: {ex.Message}";
}
```

#### 4. Usar Company Data para generar elementos

```csharp
using Newtonsoft.Json.Linq;

var currentUser = BIMAuthService.CurrentUser;
var companyData = currentUser?.CompanyData;

if (companyData?.TryGetValue("Datenbank", out var databaseData) != true)
{
    return "❌ Datos de empresa no disponibles.";
}

try
{
    if (databaseData is JArray dataArray)
    {
        int elementosCreados = 0;
        
        foreach (var item in dataArray)
        {
            // Procesar cada registro
            string nombre = item["name"]?.ToString();
            string codigo = item["code"]?.ToString();
            
            if (!string.IsNullOrEmpty(nombre) && !string.IsNullOrEmpty(codigo))
            {
                // Usar los datos para crear/modificar elementos
                // Por ejemplo, crear una etiqueta de texto
                elementosCreados++;
            }
        }
        
        return $"✅ Procesados {elementosCreados} registros de Company Data";
    }
    else
    {
        return "❌ Los datos no están en formato de array.";
    }
}
catch (Exception ex)
{
    return $"❌ Error: {ex.Message}";
}
```

### Tiempo de Disponibilidad

**⚠️ IMPORTANTE:** Después de autenticarte:

- **Primeros 2 segundos**: Los datos pueden no estar completamente descargados
- **2-10 segundos**: El sistema intenta descargar los datos con reintentos automáticos
- **Después de 10 segundos**: Los datos están disponibles o se mostró un error

**Recomendación:** Si accedes a Company Data poco después del login, implementa un pequeño delay:

```csharp
// Si acabas de hacer login
await Task.Delay(3000);  // Esperar 3 segundos para asegurar carga

var currentUser = BIMAuthService.CurrentUser;
var companyData = currentUser?.CompanyData;

if (companyData?.Count > 0)
{
    return "✅ Datos listos para usar";
}
else
{
    return "⚠️ Datos aún no disponibles. Intenta de nuevo.";
}
```

### Estructura de CompanyDataVariable (Modelo)

Cada variable en `CompanyDataVariables` tiene esta estructura:

```csharp
public class CompanyDataVariable
{
    public string VariableName { get; set; }      // "Datenbank"
    public string SheetId { get; set; }           // "1j02RBg7..."
    public string SheetName { get; set; }         // "BDB"
    
    public JToken Data { get; set; }              // Datos descargados
    public string Status { get; set; }            // "Loaded", "Error", "Pending"
    public string ErrorMessage { get; set; }      // Si hay error
    public int SizeInKb { get; set; }             // Tamaño en KB
}
```

**Ejemplo de uso:**

```csharp
var currentUser = BIMAuthService.CurrentUser;

foreach (var variable in currentUser?.CompanyDataVariables ?? new List<CompanyDataVariable>())
{
    string info = $"{variable.VariableName}: {variable.Status}";
    
    if (variable.Status == "Loaded")
    {
        info += $" ({variable.SizeInKb} KB)";
    }
    else if (variable.Status == "Error")
    {
        info += $" - Error: {variable.ErrorMessage}";
    }
    
    TaskDialog.Show("Variable", info);
}
```

### Debugging Company Data

Si los datos no aparecen:

1. **Verifica autenticación**: ¿Mostraste el formulario de login?
   ```csharp
   if (BIMAuthService.CurrentUser == null)
       return "❌ No autenticado. Ejecuta el login primero.";
   ```

2. **Verifica si están en descarga**: 
   ```csharp
   var companyData = BIMAuthService.CurrentUser?.CompanyData;
   if (companyData?.Count == 0)
       return "⚠️ Los datos están descargando. Espera 5 segundos e intenta de nuevo.";
   ```

3. **Verifica el formato de configuración**: El backend debe enviar:
   ```json
   {
     "userData": {
       "extra": {
         "CompanyDataConfig": "Datenbank,1j02RBg7...,BDB"
       }
     }
   }
   ```

4. **Revisa logs**: Ve a Settings → Logs y busca mensajes con `[CompanyData]`

### Almacenamiento en Caché

Los datos se almacenan en:
```
C:\Users\[USERNAME]\AppData\Roaming\RoslynCopilot\company-data-cache\
```

Se guardan en formato JSON comprimido para acceso rápido en la siguiente sesión.

### ⚠️ Limitaciones y Consideraciones

- Los datos se borran al cerrar Revit (se descargan nuevamente al iniciar sesión)
- El tamaño máximo recomendado es 10 MB por variable
- Las variables se descargan en paralelo (máximo 3 reintentos por variable)
- Timeout de 15 segundos por descarga individual
- Los errores de descarga se loguean pero no impiden el resto de operaciones