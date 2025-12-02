using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Manages downloading, caching, and retrieving company data from the Data Server API
    /// </summary>
    public static class CompanyDataCacheManager
    {
        private static readonly string CacheDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoslynCopilot",
            "company-data-cache"
        );

        /// <summary>
        /// Downloads company data variables from the Data Server API
        /// </summary>
        /// <param name="configString">Format: "var1,sheetId1,sheetName1;var2,sheetId2,sheetName2"</param>
        /// <param name="token">JWT authentication token</param>
        /// <param name="logCallback">Callback for logging messages</param>
        /// <returns>Dictionary mapping variable names to CompanyDataVariable objects</returns>
        public static async Task<Dictionary<string, CompanyDataVariable>> DownloadCompanyDataAsync(
            string configString,
            string token,
            Action<string> logCallback = null)
        {
            var result = new Dictionary<string, CompanyDataVariable>();

            logCallback?.Invoke($"[CompanyData] ===== INICIO DESCARGA =====");
            logCallback?.Invoke($"[CompanyData] 🔍 Method called with configString: '{configString}'");
            logCallback?.Invoke($"[CompanyData] configString: '{configString}'");
            logCallback?.Invoke($"[CompanyData] configString is null: {configString == null}");
            logCallback?.Invoke($"[CompanyData] configString is empty: {string.IsNullOrWhiteSpace(configString)}");
            logCallback?.Invoke($"[CompanyData] configString length: {configString?.Length ?? 0}");
            logCallback?.Invoke($"[CompanyData] token present: {!string.IsNullOrEmpty(token)}");

            if (string.IsNullOrWhiteSpace(configString))
            {
                logCallback?.Invoke("[CompanyData] ❌ No company data config provided - exiting");
                return result;
            }

            try
            {
                logCallback?.Invoke("[CompanyData] Starting company data download...");

                // Parse config string
                var variables = ParseCompanyDataConfig(configString, logCallback);

                if (variables.Count == 0)
                {
                    logCallback?.Invoke("[CompanyData] No variables to download");
                    return result;
                }

                logCallback?.Invoke($"[CompanyData] Downloading {variables.Count} company data variables");

                // Download each variable
                foreach (var variable in variables)
                {
                    try
                    {
                        logCallback?.Invoke($"[CompanyData] Downloading: {variable.VariableName}");

                        var data = await DownloadSingleVariableAsync(
                            variable.VariableName,
                            variable.SheetId,
                            variable.SheetName,
                            token,
                            logCallback
                        );

                        if (data != null)
                        {
                            // Calculate size
                            var json = JsonConvert.SerializeObject(data);
                            var sizeInKb = Encoding.UTF8.GetByteCount(json) / 1024;

                            variable.MarkAsLoaded(data, sizeInKb);
                            logCallback?.Invoke($"[CompanyData] ✅ {variable.VariableName} loaded successfully ({sizeInKb} KB)");
                        }
                        else
                        {
                            variable.MarkAsError("No data returned from server");
                            logCallback?.Invoke($"[CompanyData] ❌ {variable.VariableName} returned empty data");
                        }
                    }
                    catch (Exception ex)
                    {
                        variable.MarkAsError(ex.Message);
                        logCallback?.Invoke($"[CompanyData] ❌ Error downloading {variable.VariableName}: {ex.Message}");
                    }

                    result[variable.VariableName] = variable;
                }

                logCallback?.Invoke($"[CompanyData] Download complete. {result.Count(x => x.Value.Status == "Loaded")} of {result.Count} variables loaded");
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"[CompanyData] ❌ Error in DownloadCompanyDataAsync: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Downloads a single variable from the Data Server API with retry logic
        /// </summary>
        private static async Task<object> DownloadSingleVariableAsync(
            string variableName,
            string sheetId,
            string sheetName,
            string token,
            Action<string> logCallback)
        {
            const string DATA_SERVER_URL = "https://script.google.com/macros/s/AKfycbx8asivIkpIZcTmbyuPFbcitKi6h8YjbfQP9q0P0I8qnJpdtfQ4WIQnGdXPN6KH0S8pHg/exec";
            const int MAX_RETRIES = 3;
            const int INITIAL_RETRY_DELAY_MS = 1000;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(15);

                        // Build request URL
                        var url = $"{DATA_SERVER_URL}?token={Uri.EscapeDataString(token)}&sheetId={Uri.EscapeDataString(sheetId)}&sheetName={Uri.EscapeDataString(sheetName)}";

                        logCallback?.Invoke($"[CompanyData] Attempt {attempt}/{MAX_RETRIES}: GET {variableName}");

                        var response = await client.GetAsync(url);
                        var responseBody = await response.Content.ReadAsStringAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            logCallback?.Invoke($"[CompanyData] Attempt {attempt} failed: HTTP {response.StatusCode}");

                            if (attempt < MAX_RETRIES)
                            {
                                var delay = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                                await Task.Delay(delay);
                                continue;
                            }
                            else
                            {
                                return null;
                            }
                        }

                        // Parse response
                        var jObject = JObject.Parse(responseBody);
                        var status = jObject["status"]?.Value<string>();

                        if (status != "ok")
                        {
                            var error = jObject["error"]?.Value<string>() ?? "Unknown error";
                            logCallback?.Invoke($"[CompanyData] Server error: {error}");
                            return null;
                        }

                        // Extract data array
                        var data = jObject["data"];
                        if (data == null)
                        {
                            logCallback?.Invoke($"[CompanyData] No 'data' field in response");
                            return null;
                        }

                        return data;
                    }
                }
                catch (TaskCanceledException)
                {
                    logCallback?.Invoke($"[CompanyData] Attempt {attempt} timeout");

                    if (attempt < MAX_RETRIES)
                    {
                        var delay = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                        await Task.Delay(delay);
                        continue;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (HttpRequestException ex)
                {
                    logCallback?.Invoke($"[CompanyData] Attempt {attempt} network error: {ex.Message}");

                    if (attempt < MAX_RETRIES)
                    {
                        var delay = INITIAL_RETRY_DELAY_MS * (int)Math.Pow(2, attempt - 1);
                        await Task.Delay(delay);
                        continue;
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    logCallback?.Invoke($"[CompanyData] Attempt {attempt} error: {ex.Message}");
                    return null;
                }
            }

            return null;
        }

        /// <summary>
        /// Parses the company data config string into CompanyDataVariable objects
        /// Format: "var1,sheetId1,sheetName1;var2,sheetId2,sheetName2"
        /// </summary>
        private static List<CompanyDataVariable> ParseCompanyDataConfig(string configString, Action<string> logCallback = null)
        {
            var result = new List<CompanyDataVariable>();

            if (string.IsNullOrWhiteSpace(configString))
            {
                logCallback?.Invoke("[CompanyData] ParseCompanyDataConfig: config is empty");
                return result;
            }

            try
            {
                logCallback?.Invoke($"[CompanyData] Parsing config string: {configString}");

                // Split by semicolon (separates variables)
                var items = configString.Split(';');
                logCallback?.Invoke($"[CompanyData] Found {items.Length} items after split by ';'");

                int validCount = 0;
                foreach (var item in items)
                {
                    var trimmed = item.Trim();
                    if (string.IsNullOrWhiteSpace(trimmed))
                    {
                        logCallback?.Invoke("[CompanyData]   - Item is empty, skipping");
                        continue;
                    }

                    // Split by comma (variable name, sheet ID, sheet name)
                    var parts = trimmed.Split(',');
                    logCallback?.Invoke($"[CompanyData]   - Item: '{trimmed}' -> {parts.Length} parts");

                    if (parts.Length < 3)
                    {
                        logCallback?.Invoke($"[CompanyData]     ❌ Invalid format (expected 3 parts, got {parts.Length})");
                        continue;
                    }

                    var variableName = parts[0].Trim();
                    var sheetId = parts[1].Trim();
                    var sheetName = parts[2].Trim();

                    logCallback?.Invoke($"[CompanyData]     Name='{variableName}', ID='{sheetId}', Sheet='{sheetName}'");

                    if (!string.IsNullOrWhiteSpace(variableName) &&
                        !string.IsNullOrWhiteSpace(sheetId) &&
                        !string.IsNullOrWhiteSpace(sheetName))
                    {
                        result.Add(new CompanyDataVariable(variableName, sheetId, sheetName));
                        logCallback?.Invoke($"[CompanyData]     ✅ Added to result");
                        validCount++;
                    }
                }

                logCallback?.Invoke($"[CompanyData] Parsed {validCount} valid company data variables");
            }
            catch (Exception ex)
            {
                logCallback?.Invoke($"[CompanyData] ❌ Error parsing config: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[CompanyData] Error parsing config: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Clear all cached company data
        /// </summary>
        public static void ClearCache()
        {
            try
            {
                if (Directory.Exists(CacheDirectory))
                {
                    Directory.Delete(CacheDirectory, true);
                    System.Diagnostics.Debug.WriteLine("[CompanyData] Cache cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CompanyData] Error clearing cache: {ex.Message}");
            }
        }
    }
}
