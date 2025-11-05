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