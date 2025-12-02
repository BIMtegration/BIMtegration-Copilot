# BIMtegration Copilot - AI Scripts Instructions

## Table of Contents
1. [Context and Available Variables](#1-context-and-available-variables)
2. [Basic Script Structure](#2-basic-script-structure)
3. [Asynchronous Operations](#3-asynchronous-operations)
4. [Validation Best Practices](#4-validation-best-practices)
5. [Global Error Handling](#5-global-error-handling)
6. [ExternalEvent for Model Changes](#6-externalevent-for-model-changes)
7. [WinForms Dialog Classes](#7-winforms-dialog-classes)
8. [Type Casting](#8-type-casting)
9. [Internationalization](#9-internationalization)
10. [Google Sheets API Integration](#10-google-sheets-api-integration)
11. [Premium Buttons](#11-premium-buttons)
12. [Debug Logs System](#12-debug-logs-system)
13. [Company Data Variables](#13-company-data-variables)
14. [Authentication Token Access](#14-authentication-token-access)
15. [Classic Browser and Online Catalogs](#15-classic-browser-and-online-catalogs)
16. [Future Compatibility](#16-future-compatibility)

---

## 1. Context and Available Variables

BIMtegration Copilot executes C# scripts compiled dynamically with Roslyn, within a Revit transaction.

### Automatic Context Variables
**Do not declare or redefine these variables:**

- `doc` : `Autodesk.Revit.DB.Document` - Active Revit document
- `uidoc` : `Autodesk.Revit.UI.UIDocument` - UI and selection interface
- `app` : `Autodesk.Revit.ApplicationServices.Application` - Revit application
- `uiapp` : `Autodesk.Revit.UI.UIApplication` - Application UI

**Usage Examples:**
```csharp
// Get elements
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();

// Work with selection
var selection = uidoc.Selection.GetElementIds();

// Show dialogs
TaskDialog.Show("Info", "Script executed successfully");

// Access properties
string projectName = doc.Title;
string version = app.VersionName;
```

**Pre-loaded References:**
- System.Net.Http, System.IO, System.Linq
- OfficeOpenXml, CsvHelper, Newtonsoft.Json
- Autodesk.Revit.DB, Autodesk.Revit.UI

**Critical Rules:**
- Do not declare manual transactions
- Do not configure encoding or licenses
- Do not overwrite context variables

---

## 2. Basic Script Structure

```csharp
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
// ... other usings as needed

try {
    // Main logic using doc, uidoc, app, uiapp
    var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();
    TaskDialog.Show("Result", $"Found {walls.Count} walls");
    return $"✅ Process completed: {walls.Count} elements processed";
} catch (Exception ex) {
    TaskDialog.Show("Error", ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

---

## 3. Asynchronous Operations

**When to Use:**
- HTTP requests (APIs, downloads)
- Reading/writing large files
- Long calculations or processes

**Correct Example:**
```csharp
using System.Net.Http;
try {
    using (var client = new HttpClient()) {
        string url = "https://api.example.com/data";
        string response = await client.GetStringAsync(url); // ✅ ASYNC
        TaskDialog.Show("Result", response);
        return "✅ Request completed";
    }
} catch (Exception ex) {
    return $"❌ Error: {ex.Message}";
}
```

**Errors to Avoid:**
- ❌ Do not use `.Result` or `.Wait()` (freezes Revit)
- ❌ Do not mix sync and async code incorrectly

---

## 4. Validation Best Practices

**Validate File Path:**
```csharp
string path = form.FilePath;
if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
    return "❌ File path is not valid or file does not exist.";
```

**Validate Numeric Value:**
```csharp
int value;
if (!int.TryParse(form.txtValue.Text, out value) || value < 0)
    return "❌ Value must be a positive integer.";
```

**Validate Element Selection:**
```csharp
var selection = uidoc.Selection.GetElementIds();
if (selection.Count == 0)
    return "❌ You must select at least one element.";
```

**Validate Parameters:**
```csharp
Parameter p = el.LookupParameter("NUMBER");
if (p == null)
    return "❌ The 'NUMBER' parameter does not exist on this element.";
if (p.IsReadOnly)
    return "❌ The 'NUMBER' parameter is read-only.";
```

---

## 5. Global Error Handling

**Basic Try-Catch:**
```csharp
try {
    // ... main logic ...
    return "✅ Process completed";
} catch (Exception ex) {
    TaskDialog.Show("Error", ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

**Custom Logging:**
```csharp
public static void Log(string message) {
    File.AppendAllText(@"C:\Temp\copilot_log.txt", 
        DateTime.Now + ": " + message + Environment.NewLine);
}

try {
    // ... logic ...
    Log("Script executed successfully");
    return "✅ Ok";
} catch (Exception ex) {
    Log("Error: " + ex.Message);
    return $"❌ Error: {ex.Message}";
}
```

---

## 6. ExternalEvent for Model Changes

**For persistent changes to the model:**
```csharp
// Define handler and event
var handler = new GenericExternalEventHandler();
var externalEvent = ExternalEvent.Create(handler);

// Define action that modifies the model
handler.ActionToExecute = (uiapp) => {
    var doc = uiapp.ActiveUIDocument.Document;
    var el = doc.GetElement(someId);
    var p = el.LookupParameter("NUMBER");
    if (p != null && !p.IsReadOnly)
        p.Set("NewValue");
    // ... more logic ...
};

// Execute in safe Revit context
externalEvent.Raise();
```

**With Reflection (if required):**
```csharp
var actionProp = handlerObj.GetType().GetProperty("ActionToExecute");
actionProp.SetValue(handlerObj, (Action<UIApplication>)action);
var raiseMethod = externalEvent.GetType().GetMethod("Raise");
raiseMethod.Invoke(externalEvent, null);
```

---

## 7. WinForms Dialog Classes

**Complete Example:**
```csharp
using System.Windows.Forms;

public class NumbererForm : Form
{
    public string Parameter { get; private set; }
    public int StartValue { get; private set; }
    public string Prefix { get; private set; }
    TextBox txtParameter, txtStartValue, txtPrefix;
    
    public NumbererForm()
    {
        Text = "Number Elements";
        Width = 300; Height = 200;
        
        Label lbl1 = new Label { Text = "Parameter:", Top = 20, Left = 10, Width = 80 };
        txtParameter = new TextBox { Top = 20, Left = 100, Width = 150, Text = "NUMBER" };
        
        Label lbl2 = new Label { Text = "Start Value:", Top = 60, Left = 10, Width = 80 };
        txtStartValue = new TextBox { Top = 60, Left = 100, Width = 150, Text = "1" };
        
        Label lbl3 = new Label { Text = "Prefix:", Top = 100, Left = 10, Width = 80 };
        txtPrefix = new TextBox { Top = 100, Left = 100, Width = 150, Text = "" };
        
        Button btnOK = new Button { Text = "Number", Top = 140, Left = 100, Width = 80 };
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

// Usage in main script
NumbererForm form = new NumbererForm();
if (form.ShowDialog() != DialogResult.OK)
    return "Operation cancelled by user.";

string parameter = form.Parameter;
int counter = form.StartValue;
string prefix = form.Prefix;
// ... use values in main logic ...
```

---

## 8. Type Casting

**Common Cases:**
```csharp
// Element to specific type
Element el = doc.GetElement(id);
Wall wall = el as Wall;
if (wall != null) {
    double height = wall.get_Parameter(BuiltInParameter.WALL_USER_HEIGHT_PARAM)?.AsDouble() ?? 0;
}

// FamilyInstance
FamilyInstance fi = el as FamilyInstance;
if (fi != null) {
    string type = fi.Symbol.Name;
}

// Parameters
Parameter p = el.LookupParameter("NUMBER");
string value = p.AsString() ?? p.AsValueString();

// Generic lists
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).Cast<Wall>().ToList();
```

---

## 9. Internationalization

**With Dictionary:**
```csharp
Dictionary<string, string> messages = new Dictionary<string, string> {
    { "es", "Process completed" },
    { "de", "Vorgang abgeschlossen" },
    { "en", "Process completed" }
};
string language = "en"; // Selected by user
TaskDialog.Show("Info", messages[language]);
```

**In WinForms:**
```csharp
public class MenuForm : Form {
    public MenuForm(string language) {
        if (language == "de") {
            Text = "Menü";
            // ... other controls in German
        } else if (language == "en") {
            Text = "Menu";
            // ... other controls in English
        }
    }
}
```

---

## 10. Google Sheets API Integration

**Correct JSON Deserialization:**
```csharp
using Newtonsoft.Json.Linq;

string rawJson = await client.GetStringAsync(url);
var obj = JObject.Parse(rawJson);
if ((string)obj["status"] != "ok")
    return "❌ Error: " + (string)obj["message"];

var data = obj["data"] as JArray;
if (data == null)
    return "❌ The 'data' property was not found in the response.";

// Use data as list of records
```

**Sending Data (POST):**
```csharp
using System.Net.Http;
using System.Text;
using Newtonsoft.Json.Linq;

var payload = new {
    field1 = "value1",
    field2 = "value2"
};
var json = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
var content = new StringContent(json, Encoding.UTF8, "application/json");

using (var client = new HttpClient()) {
    var response = await client.PostAsync("https://your-endpoint.com/api", content);
    var rawJson = await response.Content.ReadAsStringAsync();
    var obj = JObject.Parse(rawJson);
    if ((string)obj["status"] != "ok")
        return "❌ Error: " + (string)obj["message"];
    return "✅ Data sent successfully";
}
```

---

## 11. Premium Buttons

### What are Premium Buttons?

**Premium buttons** are custom scripts stored in Google Drive that are automatically downloaded when you authenticate with a premium account in BIMtegration Copilot. They are grouped by company and appear in the **Advanced** tab.

### Premium Button Features:

- ✅ **Automatic Download**: Downloaded when you login with a premium account
- ✅ **Local Cache**: Saved locally to avoid re-downloading
- ✅ **Grouping**: Organized by company (from "Company" field in metadata)
- ✅ **Versioning**: Each download updates the local version
- ✅ **Manual Download**: Download premium scripts for later import

### Where They Appear:

Premium buttons appear in the **Advanced** tab under company sections:

```
🔒 PREMIUM BUTTONS
  🏢 MyCompany
    📌 Premium Script 1  [✓ cached] [▶️ Run] [💾 Download]
    📌 Premium Script 2  [✓ cached] [▶️ Run] [💾 Download]
  🏢 AnotherCompany
    📌 Premium Script 3  [❌ Error] [🔄 Retry]
```

**Available States:**
- `✓ cached` - Script downloaded and ready in cache
- `⏳ downloading` - Download in progress
- `❌ Error` - Download failed, show retry button
- `✓ downloaded` - Downloaded in this session

### Configuration Format

Premium buttons are configured in **Google Sheets** in metadata format:

```
name1,url1;name2,url2;name3,url3,company3
```

**Fields:**
- `name`: Script name (max 100 characters)
- `url`: Public Google Drive URL of the JSON file with the script
- `company`: (optional) Company name for grouping

**Google Drive URL Format:**
```
https://drive.usercontent.google.com/u/0/uc?id=[FILE_ID]&export=download
```

### Premium Script JSON Structure

Premium scripts are stored as JSON files in Google Drive with this structure:

```json
{
  "id": "premium-script-001",
  "name": "Export to XML",
  "description": "Exports selected elements to XML format",
  "code": "using Autodesk.Revit.DB;...",
  "category": "🔒 [MyCompany]",
  "tags": ["export", "xml", "premium"],
  "version": "1.0",
  "author": "My Team"
}
```

### Manual Download of Premium Buttons

If your premium subscription expires, you can download already cached premium buttons for later manual import:

1. Before subscription expires, click **💾 Download** on the premium button
2. Save the JSON file to a safe location
3. After expiration, in **Scripts** tab → **Import Selection**, load the JSON file
4. The script will be added to your local script list

### Cache and Storage

- **Location**: `C:\Users\[Username]\AppData\Roaming\RoslynCopilot\premium-buttons-cache\`
- **Duration**: Current Revit session (cleared when Revit restarts)
- **Size**: Depends on number and size of premium scripts (typically 1-10 MB)

### Troubleshooting Download Issues

**If a button shows ❌ Error:**

1. Check your internet connection
2. Click **🔄 Retry** to retry the download
3. If the issue persists:
   - Restart Revit (clears cache)
   - Log out and log back in
   - Contact administrator if problem continues

**Debugging Information:**
- Open Debug Console in Visual Studio (Debug → Windows → Output)
- Look for messages with `[Premium]` prefix for download details
- Common errors: network timeout, invalid URL, corrupted file

---

## 12. Debug Logs System

### Overview

BIMtegration Copilot has an **integrated logging system** that allows:
- ✅ Automatic event recording
- ✅ Real-time log viewing in the UI
- ✅ Premium buttons debugging
- ✅ User action auditing
- ✅ Persistent history storage

### Log Location

**In File System:**
```
C:\Users\[USERNAME]\AppData\Roaming\RoslynCopilot\
└── premium-buttons-debug.log
```

**In Interface:**
```
BIMtegration Copilot
  └── ⚙️ Settings (Tab)
       └── 📋 Logs (TextArea)
```

### Viewing Logs in the UI

**Step 1:** Open BIMtegration Copilot in Revit
- Revit → Add-ins → BIMtegration Copilot

**Step 2:** Go to the **Settings** tab

**Step 3:** Find the **Logs** section

**Step 4:** Read the logs (most recent at the bottom)

### Adding Logs to Code

**Option 1:** Use `LogToFile()` in BIMLoginWindow.cs

```csharp
LogToFile($"[MyClass] My debug message");
```

**Option 2:** Create your own LogToFile function

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
    catch { /* Ignore logging errors */ }
}
```

### Recommended Log Format

```
[timestamp] [ClassName.MethodName] message
```

**Useful Patterns:**

#### ✅ Success:
```csharp
LogToFile($"[BIMLoginWindow] ✅ Login successful - User: {user}");
```

#### ❌ Error:
```csharp
LogToFile($"[Premium] ❌ Download failed: {ex.Message}");
```

#### ⚠️ Warning:
```csharp
LogToFile($"[Cache] ⚠️ File not found in cache");
```

#### ℹ️ Information:
```csharp
LogToFile($"[Premium] ℹ️ Starting download from: {url}");
```

---

## 13. Company Data Variables

### What are Company Data Variables?

**Company Data Variables** are business information (classifications, zones, departments, etc.) loaded at login time from a central data server. They are cached locally during the Revit session and can be accessed by scripts for faster processing.

### Key Features:

- ✅ **Loaded at Login**: Downloaded automatically after successful authentication
- ✅ **Background Download**: Non-blocking, doesn't delay login window close
- ✅ **Session Cache**: Live for the entire Revit session
- ✅ **Status Tracking**: Visual indicators show loading status
- ✅ **Error Handling**: Automatic retry with exponential backoff
- ✅ **Accessible in Scripts**: Use `doc.Title` or context variables

### Where They Appear:

Company data variables appear in the **Advanced** tab in a dedicated section:

```
📊 COMPANY DATA VARIABLES
  ├─ Classifications [✅ Loaded] (1.2 KB)
  ├─ Zones [✅ Loaded] (2.5 KB)
  └─ Departments [⏳ Loading...]
  
  [🔄 Refresh Company Data]
```

**Status Indicators:**
- `✅ Loaded` - Data successfully downloaded and available
- `⏳ Loading...` - Download in progress
- `❌ Error` - Download failed, reason shown below
- `🔄 Refresh` - Click to logout/login to refresh data

### Configuration Format

Company data variables are defined in Google Sheets in this format:

```
VariableName,SheetId,SheetName;VariableName2,SheetId2,SheetName2
```

**Fields:**
- `VariableName`: Name of the variable (e.g., "Classifications", "Zones")
- `SheetId`: Google Sheets ID containing the data
- `SheetName`: Sheet name within the spreadsheet

**Example:**
```
Classifications,1ABC23XYZ,Sheets;Zones,2DEF45UVW,Zones;Departments,3GHI67JKL,HR
```

### How to Access in Scripts

**Get Company Data in your scripts:**

```csharp
// The company data is stored in the current user context
// Access it through the authentication service

// Note: These variables are automatically injected into script context
var companyData = CompanyData; // Dictionary<string, object>

// Access specific variables
var classifications = companyData["Classifications"]; // JArray or List<dynamic>
var zones = companyData["Zones"];

// Iterate through data
if (classifications is Newtonsoft.Json.Linq.JArray classArray)
{
    foreach (var item in classArray)
    {
        string code = item["code"].ToString();
        string description = item["description"].ToString();
        // Use the data...
    }
}

return $"✅ Loaded {companyData.Count} company data variables";
```

### Company Data Cache

- **Location**: `C:\Users\[Username]\AppData\Roaming\RoslynCopilot\company-data-cache\`
- **Duration**: Current Revit session (cleared when Revit closes)
- **Refresh Policy**: Only refreshed on new login (doesn't refresh during session)
- **Expiration**: Data expires on Revit close, requires new login to refresh

### Download Behavior

The company data download process:

1. **Triggered**: After successful login
2. **Background**: Uses `Task.Run()` to not block UI
3. **Async**: Each variable downloaded with `HttpClient` asynchronously
4. **Retry Logic**: 3 attempts with exponential backoff (1s, 2s, 4s delays)
5. **Timeout**: 30 seconds per variable
6. **Result**: Stored in `UserInfo.CompanyData` dictionary

### Troubleshooting Company Data

**If company data doesn't load:**

1. Check internet connection
2. Verify Google Sheets IDs are correct in configuration
3. Ensure sheets are publicly accessible
4. Log out and log back in to retry
5. Check logs in Settings → Logs for error details

**Common Errors:**
- `❌ Timeout` - Data server didn't respond in time (>30 seconds)
- `❌ Unauthorized` - Invalid or expired authentication token
- `❌ Not Found` - Google Sheet or sheet name doesn't exist
- `❌ Invalid JSON` - Data format issue in the sheet

### Example: Creating a Script Using Company Data

```csharp
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Newtonsoft.Json.Linq;

try
{
    // Get classifications from company data
    if (CompanyData != null && CompanyData.ContainsKey("Classifications"))
    {
        var classifications = CompanyData["Classifications"];
        
        if (classifications is JArray classArray)
        {
            TaskDialog.Show(
                "Company Classifications",
                $"Total classifications: {classArray.Count}"
            );
            
            // Apply first classification to selected elements
            var selection = uidoc.Selection.GetElementIds();
            if (selection.Count > 0)
            {
                var firstClass = classArray[0];
                string classCode = firstClass["code"].ToString();
                TaskDialog.Show("Applied", $"Applied class: {classCode}");
            }
        }
    }
    else
    {
        return "❌ Company data not loaded. Please log in again.";
    }
    
    return "✅ Script completed";
}
catch (Exception ex)
{
    return $"❌ Error: {ex.Message}";
}
```

---

## 14. Authentication Token Access

**Save and access authentication token from scripts:**

The authentication tokens are stored encrypted on disk using DPAPI. To access the token from your scripts, interact with the authentication service available in the context.

### Getting the Current Token

```csharp
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

// Helper function to load the token from storage
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

// Use the token in your script
var token = GetStoredToken();
if (string.IsNullOrEmpty(token))
{
    return "❌ No stored token. Please authenticate first.";
}

// Now you can use the token in HTTP requests
using (var client = new HttpClient())
{
    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
    var response = await client.GetAsync("https://api.bimtegration.com/data");
    var data = await response.Content.ReadAsStringAsync();
    return $"✅ Data obtained: {data}";
}
```

### Getting User Information

```csharp
string GetUserInfo()
{
    string tokenFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoslynCopilot",
        "bim_auth.dat"
    );
    
    if (!File.Exists(tokenFilePath))
        return "Not authenticated";

    try
    {
        byte[] entropy = Encoding.UTF8.GetBytes("BIMtegration2025");
        var encryptedData = File.ReadAllBytes(tokenFilePath);
        var jsonBytes = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);
        var json = Encoding.UTF8.GetString(jsonBytes);
        
        dynamic tokenData = JsonConvert.DeserializeObject(json);
        return $"User: {tokenData.Usuario}, Plan: {tokenData.Plan}";
    }
    catch
    {
        return "Error reading authentication data";
    }
}

var userInfo = GetUserInfo();
TaskDialog.Show("User Information", userInfo);
return $"✅ {userInfo}";
```

### Token Validation

```csharp
async Task<bool> IsTokenValid()
{
    var token = GetStoredToken();
    if (string.IsNullOrEmpty(token))
        return false;

    const string AUTH_SERVER_URL = "https://script.google.com/macros/s/YOUR_AUTH_SERVER/exec";
    
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
        return true; // Fail-safe: assume valid if network error
    }
}

// Use before premium function
if (!await IsTokenValid())
{
    return "❌ Token invalid or expired. Please authenticate again.";
}

// Proceed with premium function
return "✅ Token validated. Proceeding...";
```

### ⚠️ Important Considerations

1. **Encryption**: Token is encrypted with DPAPI, readable only by the Windows user who created it
2. **Secure Location**: Stored in `%APPDATA%\RoslynCopilot\bim_auth.dat`
3. **Fixed Entropy**: Entropy is `"BIMtegration2025"` - **do not change it**
4. **Periodic Validation**: Revalidate tokens occasionally to detect expiration
5. **Error Handling**: If decryption fails, user needs to re-authenticate

---

## 15. Classic Browser and Online Catalogs

You can display web pages, online catalogs, and download families directly from Copilot scripts using the classic WinForms WebBrowser control.

### Example: Display Online Catalog

```csharp
using System.Windows.Forms;

var form = new Form();
form.Text = "Online Catalog";
form.Width = 900;
form.Height = 600;

var browser = new WebBrowser();
browser.Dock = DockStyle.Fill;
browser.Url = new System.Uri("https://yourcatalog.com");
form.Controls.Add(browser);

form.ShowDialog();

return "✅ Catalog displayed";
```

### Example: Insert Family from URL

```csharp
using System.Windows.Forms;
using System.Net;

string familyUrl = Microsoft.VisualBasic.Interaction.InputBox("Family .rfa URL:", "Insert Family", "https://yourcatalog.com/family.rfa");
string localPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "familia_temp.rfa");

using (var client = new WebClient())
{
    client.DownloadFile(familyUrl, localPath);
}

Autodesk.Revit.DB.Family family;
if (doc.LoadFamily(localPath, out family))
{
    TaskDialog.Show("Success", "✅ Family inserted correctly.");
}
else
{
    TaskDialog.Show("Error", "❌ Error inserting family.");
}

return "✅ Insert process completed";
```

You can combine both examples to create interactive flows with web catalogs and automate family insertion in Revit.

---

## 16. Future Compatibility

**Best Practices:**
- Use only public and documented Revit APIs
- Avoid internal classes or obsolete methods
- Validate parameter names and categories
- Keep scripts modular

**Recommended APIs:**
```csharp
// ✅ Recommended
var walls = new FilteredElementCollector(doc).OfClass(typeof(Wall)).ToList();

// ✅ Validate parameters
Parameter p = el.LookupParameter("NUMBER");
if (p == null)
    return "❌ The parameter does not exist";
```

---

## Final Structure Requirements

1. Use standard context variables
2. Handle errors with try-catch
3. Return descriptive string result
4. Validate inputs before operations
5. Use async/await for long operations
6. Follow specific patterns for external APIs

**These instructions contain all the information needed to generate functional and robust code in BIMtegration Copilot.**
