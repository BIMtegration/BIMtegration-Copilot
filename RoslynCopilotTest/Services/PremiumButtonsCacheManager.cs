using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Diagnostics;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Resultado de descarga de un botón premium con información de error
    public class PremiumDownloadResult
    {
        public string ButtonId { get; set; }
        public string ButtonName { get; set; }
        public string Company { get; set; }
        public ScriptDefinition Script { get; set; }
        public bool Success { get; set; }
        public bool FromCache { get; set; }
        public string ErrorReason { get; set; }
    }

    /// <summary>
    /// Gestiona descarga y caché de botones premium desde URLs de Google Drive
    /// El caché expira al reiniciar Revit
    /// </summary>
    public class PremiumButtonsCacheManager
    {
        private static readonly string CachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot",
            "premium-buttons-cache"
        );

        private static readonly string ManifestPath = Path.Combine(CachePath, "manifest.json");

        /// <summary>
        /// Descarga scripts JSON desde URLs con información detallada de resultados (incluyendo errores)
        /// </summary>
        public static async Task<List<PremiumDownloadResult>> DownloadPremiumButtonsWithDetailsAsync(
            List<PremiumButtonInfo> buttonInfos,
            Action<string> onProgress = null)
        {
            var results = new List<PremiumDownloadResult>();

            if (buttonInfos == null || buttonInfos.Count == 0)
                return results;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Iniciando descarga detallada de {buttonInfos.Count} botones premium");
                LogToFile($"[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] Iniciando descarga de {buttonInfos.Count} botones premium");

                // Asegurar que existe directorio de caché
                Directory.CreateDirectory(CachePath);

                // Cargar manifest actual
                var manifest = LoadManifest();

                // Crear lista de descargas
                var tasksToRun = new List<Task<(PremiumButtonInfo info, ScriptDefinition script, bool fromCache, string error)>>();

                using (var semaphore = new System.Threading.SemaphoreSlim(5)) // Máximo 5 descargas paralelas
                {
                    foreach (var buttonInfo in buttonInfos)
                    {
                        await semaphore.WaitAsync();

                        var task = DownloadSingleButtonAsync(buttonInfo, manifest, onProgress)
                            .ContinueWith(t =>
                            {
                                semaphore.Release();
                                return t.Result;
                            });

                        tasksToRun.Add(task);
                    }

                    // Esperar a que todas terminen
                    var allResults = await Task.WhenAll(tasksToRun);

                    // Convertir a PremiumDownloadResult
                    foreach (var (info, script, fromCache, error) in allResults)
                    {
                        var result = new PremiumDownloadResult
                        {
                            ButtonId = info.id,
                            ButtonName = info.name,
                            Company = info.company,
                            Script = script,
                            Success = script != null,
                            FromCache = fromCache,
                            ErrorReason = error
                        };
                        results.Add(result);

                        if (script != null)
                        {
                            string source = fromCache ? "cached" : "downloaded";
                            onProgress?.Invoke($"✓ {info.name} ({source})");
                        }
                        else
                        {
                            onProgress?.Invoke($"✗ {info.name} - {error}");
                        }
                    }
                }

                // Guardar manifest actualizado
                SaveManifest(manifest);
                int successCount = results.Count(r => r.Success);
                int errorCount = results.Count(r => !r.Success);
                System.Diagnostics.Debug.WriteLine($"[Premium] Descarga detallada completada: {successCount} exitosas, {errorCount} con error");
                LogToFile($"[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] ✅ Descarga completada: {successCount} exitosas, {errorCount} con error");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Error crítico en descarga detallada: {ex.GetType().Name} - {ex.Message}");
                LogToFile($"[PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync] ❌ Error crítico: {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}");
            }

            return results;
        }

        /// <summary>
        /// Descarga scripts JSON desde URLs en paralelo (máx 5 simultáneos)
        /// Utiliza caché local para evitar re-descargas
        /// </summary>
        public static async Task<List<ScriptDefinition>> DownloadPremiumButtonsAsync(
            List<PremiumButtonInfo> buttonInfos,
            Action<string> onProgress = null)
        {
            var result = new List<ScriptDefinition>();

            if (buttonInfos == null || buttonInfos.Count == 0)
                return result;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Iniciando descarga de {buttonInfos.Count} botones premium");

                // Asegurar que existe directorio de caché
                Directory.CreateDirectory(CachePath);

                // Cargar manifest actual
                var manifest = LoadManifest();

                // Crear lista de descargas (con prioridad a los que ya están cacheados)
                var tasksToRun = new List<Task<(PremiumButtonInfo info, ScriptDefinition script, bool fromCache, string error)>>();

                using (var semaphore = new System.Threading.SemaphoreSlim(5)) // Máximo 5 descargas paralelas
                {
                    foreach (var buttonInfo in buttonInfos)
                    {
                        await semaphore.WaitAsync();

                        var task = DownloadSingleButtonAsync(buttonInfo, manifest, onProgress)
                            .ContinueWith(t =>
                            {
                                semaphore.Release();
                                return t.Result;
                            });

                        tasksToRun.Add(task);
                    }

                    // Esperar a que todas terminen
                    var allResults = await Task.WhenAll(tasksToRun);

                    // Procesar resultados
                    int successCount = 0;
                    int errorCount = 0;
                    int cacheCount = 0;

                    foreach (var (info, script, fromCache, error) in allResults)
                    {
                        if (script != null)
                        {
                            result.Add(script);
                            string source = fromCache ? "cached" : "downloaded";
                            if (fromCache) cacheCount++;
                            else successCount++;
                            
                            onProgress?.Invoke($"✓ {info.name} ({source})");
                            System.Diagnostics.Debug.WriteLine($"[Premium] ✓ {info.name} desde {source}");

                        }
                        else
                        {
                            errorCount++;
                            onProgress?.Invoke($"✗ {info.name} - Error: {error}");
                            System.Diagnostics.Debug.WriteLine($"[Premium] ✗ {info.name} - {error}");
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"[Premium] RESUMEN: {successCount} descargados, {cacheCount} desde caché, {errorCount} errores");
                }

                // Guardar manifest actualizado
                SaveManifest(manifest);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Error crítico descargando botones premium: {ex.GetType().Name} - {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Descarga un botón individual con reintentos, caché y logging mejorado
        /// </summary>
        private static async Task<(PremiumButtonInfo info, ScriptDefinition script, bool fromCache, string error)> 
            DownloadSingleButtonAsync(PremiumButtonInfo buttonInfo, CacheManifest manifest, Action<string> onProgress)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Procesando botón: {buttonInfo.name} (ID: {buttonInfo.id}) | Empresa: {buttonInfo.company}");

                // Intentar cargar desde caché
                var cachedScript = TryLoadFromCache(buttonInfo.id);
                if (cachedScript != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Premium] ✓ Cache HIT para {buttonInfo.name}");
                    return (buttonInfo, cachedScript, true, null);
                }

                System.Diagnostics.Debug.WriteLine($"[Premium] Cache MISS para {buttonInfo.name} - iniciando descarga desde: {buttonInfo.url}");

                // Descargar desde URL
                onProgress?.Invoke($"⏳ Downloading {buttonInfo.name}...");

                var script = await DownloadFromUrlAsync(buttonInfo.url, maxRetries: 3);

                if (script != null)
                {
                    // Guardar en caché
                    SaveToCache(buttonInfo.id, script);
                    System.Diagnostics.Debug.WriteLine($"[Premium] Guardado en caché: {buttonInfo.name}");

                    // Actualizar manifest
                    if (manifest.scripts != null)
                    {
                        var existing = manifest.scripts.FirstOrDefault(s => s.id == buttonInfo.id);
                        if (existing != null)
                            manifest.scripts.Remove(existing);

                        manifest.scripts.Add(new CacheEntry
                        {
                            id = buttonInfo.id,
                            name = buttonInfo.name,
                            url = buttonInfo.url,
                            company = buttonInfo.company,
                            cached_at = DateTime.UtcNow,
                            cached = true
                        });
                    }

                    return (buttonInfo, script, false, null);
                }
                else
                {
                    string errorMsg = $"Failed to download after retries from {buttonInfo.url}";
                    System.Diagnostics.Debug.WriteLine($"[Premium] ❌ Error: {errorMsg}");
                    return (buttonInfo, null, false, errorMsg);
                }
            }
            catch (Exception ex)
            {
                string errorMsg = $"Exception: {ex.GetType().Name} - {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[Premium] ❌ Excepción en DownloadSingleButtonAsync: {errorMsg}");
                return (buttonInfo, null, false, errorMsg);
            }
        }

        /// <summary>
        /// Descarga JSON desde URL con reintentos y logging mejorado
        /// </summary>
        private static async Task<ScriptDefinition> DownloadFromUrlAsync(string url, int maxRetries = 3)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                LogToFile("[Premium] URL vacía o nula, descarga cancelada");
                return null;
            }

            LogToFile($"[Premium] Iniciando descarga desde: {url}");
            int attempt = 0;
            
            while (attempt < maxRetries)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);
                        LogToFile($"[Premium] Intento {attempt + 1}/{maxRetries}: Descargando...");
                        
                        var json = await client.GetStringAsync(url);
                        LogToFile($"[Premium] Intento {attempt + 1}: Respuesta recibida ({json.Length} caracteres)");
                        
                        // LOG: Ver primeros caracteres del JSON
                        string jsonPreview = json.Length > 200 ? json.Substring(0, 200) : json;
                        LogToFile($"[Premium] JSON Preview: {jsonPreview}");
                        
                        // LOG: Ver JSON completo para diagnóstico
                        LogToFile($"[Premium] JSON COMPLETO:\n{json}\n[FIN JSON]");

                        // Intentar parsear como ScriptDefinition
                        // Primero intenta parsear directo
                        var script = JsonConvert.DeserializeObject<ScriptDefinition>(json);
                        
                        // Si falla (Id/Name null), intenta estructura envuelta
                        if (script == null || string.IsNullOrEmpty(script.Id) || string.IsNullOrEmpty(script.Name))
                        {
                            LogToFile($"[Premium] Estructura directa no funcionó (Id={script?.Id}, Name={script?.Name}), intentando estructura envuelta...");
                            try
                            {
                                // Intenta parsear como wrapper
                                dynamic wrapper = JsonConvert.DeserializeObject(json);
                                if (wrapper != null && wrapper["Scripts"] != null)
                                {
                                    var scriptsArray = wrapper["Scripts"] as Newtonsoft.Json.Linq.JArray;
                                    if (scriptsArray != null && scriptsArray.Count > 0)
                                    {
                                        var firstScriptJson = scriptsArray[0].ToString();
                                        script = JsonConvert.DeserializeObject<ScriptDefinition>(firstScriptJson);
                                        LogToFile($"[Premium] ✓ Estructura envuelta detectada y deserializada");
                                    }
                                }
                            }
                            catch (Exception unwrapEx)
                            {
                                LogToFile($"[Premium] Error al desenvuelver: {unwrapEx.Message}");
                            }
                        }
                        
                        LogToFile($"[Premium] Después deserializar - script: {(script == null ? "NULL" : "OK")}, Id: {(script?.Id ?? "NULL")}, Name: {(script?.Name ?? "NULL")}");
                        LogToFile($"[Premium] Code length: {(script?.Code?.Length ?? 0)} caracteres");
                        
                        // Si Code está vacío pero tenemos Name, extraer manualmente desde wrapper
                        if (script != null && string.IsNullOrEmpty(script.Code))
                        {
                            LogToFile($"[Premium] ⚠️ Code está vacío, intentando extraer manualmente desde JSON...");
                            try
                            {
                                dynamic wrapper = JsonConvert.DeserializeObject(json);
                                string codeExtraido = null;
                                
                                // Intenta extraer del wrapper directo
                                if (wrapper["Code"] != null)
                                {
                                    codeExtraido = (string)wrapper["Code"];
                                }
                                // Si no, intenta desde Scripts[0]
                                else if (wrapper["Scripts"] != null)
                                {
                                    var scriptsArray = wrapper["Scripts"] as Newtonsoft.Json.Linq.JArray;
                                    if (scriptsArray != null && scriptsArray.Count > 0 && scriptsArray[0]["Code"] != null)
                                    {
                                        codeExtraido = (string)scriptsArray[0]["Code"];
                                    }
                                }
                                
                                if (!string.IsNullOrEmpty(codeExtraido))
                                {
                                    script.Code = codeExtraido;
                                    LogToFile($"[Premium] ✓ Code extraído manualmente: {codeExtraido.Length} caracteres");
                                }
                                else
                                {
                                    LogToFile($"[Premium] ❌ No se pudo extraer Code del JSON");
                                }
                            }
                            catch (Exception codeEx)
                            {
                                LogToFile($"[Premium] Error extrayendo Code: {codeEx.Message}");
                            }
                        }
                        
                        if (script != null)
                        {
                            LogToFile($"[Premium] ✓ Descarga exitosa: {script.Name} (ID: {script.Id}), Code: {(script.Code?.Length ?? 0)} chars");
                            return script;
                        }
                        else
                        {
                            LogToFile($"[Premium] Error: JSON no es válido como ScriptDefinition");
                            attempt++;
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    LogToFile($"[Premium] Intento {attempt + 1}/{maxRetries} falló (HttpRequestException): {ex.Message}");
                    attempt++;

                    if (attempt < maxRetries)
                    {
                        int delayMs = 1000 * (int)Math.Pow(2, attempt);
                        LogToFile($"[Premium] Esperando {delayMs}ms antes de reintentar...");
                        await Task.Delay(delayMs);
                    }
                }
                catch (TaskCanceledException ex)
                {
                    LogToFile($"[Premium] Intento {attempt + 1}/{maxRetries} falló (Timeout después de 15s): {ex.Message}");
                    attempt++;

                    if (attempt < maxRetries)
                    {
                        int delayMs = 1000 * (int)Math.Pow(2, attempt);
                        LogToFile($"[Premium] Esperando {delayMs}ms antes de reintentar...");
                        await Task.Delay(delayMs);
                    }
                }
                catch (Exception ex)
                {
                    LogToFile($"[Premium] Error descargando desde {url}: {ex.GetType().Name} - {ex.Message}");
                    return null;
                }
            }

            LogToFile($"[Premium] ❌ Descarga fallida después de {maxRetries} intentos: {url}");
            return null;
        }

        /// <summary>
        /// Intenta cargar script desde caché con logging mejorado
        /// </summary>
        private static ScriptDefinition TryLoadFromCache(string scriptId)
        {
            try
            {
                string cachePath = Path.Combine(CachePath, $"{scriptId}.json");
                if (File.Exists(cachePath))
                {
                    string json = File.ReadAllText(cachePath);
                    var script = JsonConvert.DeserializeObject<ScriptDefinition>(json);
                    System.Diagnostics.Debug.WriteLine($"[Premium] Cargado desde caché: {scriptId} ({json.Length} bytes)");
                    return script;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Premium] Archivo caché no existe: {cachePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Error cargando caché para {scriptId}: {ex.GetType().Name} - {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Guarda script en caché
        /// </summary>
        private static void SaveToCache(string scriptId, ScriptDefinition script)
        {
            try
            {
                Directory.CreateDirectory(CachePath);
                string cachePath = Path.Combine(CachePath, $"{scriptId}.json");
                string json = JsonConvert.SerializeObject(script, Formatting.Indented);
                File.WriteAllText(cachePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving cache for {scriptId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Carga manifest de caché
        /// </summary>
        private static CacheManifest LoadManifest()
        {
            try
            {
                if (File.Exists(ManifestPath))
                {
                    string json = File.ReadAllText(ManifestPath);
                    return JsonConvert.DeserializeObject<CacheManifest>(json) ?? new CacheManifest();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading manifest: {ex.Message}");
            }

            return new CacheManifest();
        }

        /// <summary>
        /// Guarda manifest de caché
        /// </summary>
        private static void SaveManifest(CacheManifest manifest)
        {
            try
            {
                Directory.CreateDirectory(CachePath);
                manifest.cached_at = DateTime.UtcNow;
                string json = JsonConvert.SerializeObject(manifest, Formatting.Indented);
                File.WriteAllText(ManifestPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving manifest: {ex.Message}");
            }
        }

        /// <summary>
        /// Limpia todo el caché (se ejecuta al reiniciar Revit)
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                if (Directory.Exists(CachePath))
                {
                    Directory.Delete(CachePath, recursive: true);
                    System.Diagnostics.Debug.WriteLine("Premium buttons cache cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing cache: {ex.Message}");
            }
        }

        #region Helper Classes

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
                
                Debug.WriteLine(message);
            }
            catch { /* Ignorar errores de logging */ }
        }

        public class CacheManifest
        {
            public DateTime cached_at { get; set; } = DateTime.UtcNow;
            public List<CacheEntry> scripts { get; set; } = new List<CacheEntry>();
        }

        public class CacheEntry
        {
            public string id { get; set; }
            public string name { get; set; }
            public string url { get; set; }
            public string company { get; set; }
            public DateTime cached_at { get; set; }
            public bool cached { get; set; }
        }

        #endregion
    }
}
