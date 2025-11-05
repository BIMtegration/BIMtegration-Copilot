using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;
using RoslynCopilotTest.UI;

namespace RoslynCopilotTest
{
    /// <summary>
    /// Comando para mostrar/ocultar el panel de scripts dinámicos
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public class ShowScriptPanelCommand : IExternalCommand
    {
        private static bool _firstExecution = true;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Establecer la UIApplication estática para uso global (inicialización completa)
                Application.CurrentUIApplication = commandData.Application;
                
                // Inicializar ThemeManager con la UIApplication
                ThemeManager.Initialize(commandData.Application);
                
                // También inicializar en ScriptPanelProvider
                ScriptPanelProvider.InitializeUIApplication(commandData.Application);

                // En la primera ejecución, siempre mostrar el panel
                if (_firstExecution)
                {
                    _firstExecution = false;
                    ScriptPanelProvider.ShowPanel(commandData.Application);
                    
                    // No text toggling. Keep icon-only button.
                }
                else
                {
                    // En ejecuciones posteriores, hacer toggle normal
                    bool wasVisible = ScriptPanelProvider.IsPanelVisible();
                    ScriptPanelProvider.ShowPanel(commandData.Application);
                    bool isNowVisible = ScriptPanelProvider.IsPanelVisible();

                    // No text toggling. Keep icon-only button.
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error showing panel: {ex.Message}";
                return Result.Failed;
            }
        }

        public void ImportScripts()
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
                Title = "Select file to import"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string filePath = openFileDialog.FileName;
                    string jsonContent = File.ReadAllText(filePath);
                    
                    List<ScriptDefinition> scriptsToImport = null;
                    
                    try
                    {
                        // Intentar formato de exportación (con metadata)
                        var exportData = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (exportData != null && exportData["Scripts"] != null)
                        {
                            scriptsToImport = exportData["Scripts"].ToObject<List<ScriptDefinition>>();
                        }
                        else
                        {
                            // Intentar formato directo de lista de scripts
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                        }
                    }
                    catch (Exception)
                    {
                        // Intentar formato directo de lista de scripts como fallback
                        try
                        {
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Error parsing JSON file:\n\n{ex.Message}", "Format Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                    }
                    
                    if (scriptsToImport == null || !scriptsToImport.Any())
                    {
                        MessageBox.Show("The selected file doesn't contain valid scripts or is malformed.", "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    var importSelectionWindow = new ImportSelectionWindow(scriptsToImport, Path.GetFileName(filePath));
                    if (importSelectionWindow.ShowDialog() == true)
                    {
                        var selectedScripts = importSelectionWindow.SelectedScripts;
                        // --- Lógica de Fusión (estable por Nombre + Categoría) ---
                        var existingScripts = ScriptManager.LoadAllScriptsFlat();
                        var scriptsToSave = new List<ScriptDefinition>(existingScripts);
                        int updatedCount = 0;
                        int addedCount = 0;

                        foreach (var scriptToImport in selectedScripts)
                        {
                            // Asegurar que los scripts importados aparezcan como botón
                            scriptToImport.ShowAsButton = true;

                            // Asegurar que tiene un Id válido
                            if (string.IsNullOrWhiteSpace(scriptToImport.Id))
                            {
                                scriptToImport.Id = Guid.NewGuid().ToString();
                            }

                            // Buscar coincidencia por Nombre + Categoría (ignorando mayúsculas/minúsculas)
                            var existingScript = scriptsToSave.FirstOrDefault(s =>
                                (!string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(scriptToImport.Name) &&
                                 s.Name.Equals(scriptToImport.Name, StringComparison.OrdinalIgnoreCase)) &&
                                ((s.Category ?? string.Empty).Equals(scriptToImport.Category ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                            );

                            if (existingScript != null)
                            {
                                // Actualizar script existente
                                int index = scriptsToSave.IndexOf(existingScript);

                                // Preservar fechas y actualizar última modificación
                                scriptToImport.CreatedDate = existingScript.CreatedDate;
                                scriptToImport.LastModified = DateTime.Now;

                                // Reemplazar por la versión importada normalizada
                                scriptsToSave[index] = scriptToImport;
                                updatedCount++;
                            }
                            else
                            {
                                // Agregar nuevo script
                                // Normalizar fechas para nuevos
                                if (scriptToImport.CreatedDate == default(DateTime))
                                    scriptToImport.CreatedDate = DateTime.Now;
                                scriptToImport.LastModified = DateTime.Now;
                                scriptsToSave.Add(scriptToImport);
                                addedCount++;
                            }
                        }
                        
                        bool success = ScriptManager.SaveAllScripts(scriptsToSave);
                        
                        if (success)
                        {
                            MessageBox.Show($"Import completed.\n\n➕ New: {addedCount}\n🔄 Updated: {updatedCount}", "Import Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // Aquí deberías refrescar la UI
                        }
                        else
                        {
                            MessageBox.Show("An error occurred while saving the imported scripts.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An unexpected error occurred during import: {ex.Message}", "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}