using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Servicio para gestionar la carga y guardado de scripts
    /// </summary>
    public static class ScriptManager
    {
        // (Eliminado) WebView2 no es compatible con scripts Roslyn Copilot. Usa WebBrowser clásico en scripts.
        // New canonical scripts path (branding: BIMtegration)
        private static readonly string NewScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "Revit",
            "Addins",
            "2025",
            "BIMtegration",
            "scripts",
            "my-scripts.json"
        );

        // Legacy path kept for backward compatibility/migration
        private static readonly string LegacyScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Autodesk",
            "Revit",
            "Addins",
            "2025",
            "BIMtegration Copilot",
            "scripts",
            "my-scripts.json"
        );

        // Backup path solo para migración inicial si es necesario
        private static readonly string BackupScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "RoslynCopilotTest", 
            "my-scripts.json"
        );
        
        // Global (ProgramData) path used by the installer for example scripts. Not the runtime storage.
        private static readonly string GlobalScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "RoslynCopilot",
            "Scripts",
            "my-scripts.json"
        );

        /// <summary>
        /// Carga todos los scripts desde el archivo JSON
        /// </summary>
        /// <returns>Lista de scripts organizados por categoría</returns>
        public static List<ScriptCategory> LoadScripts()
        {
            try
            {
                // Prefer AppData\Autodesk\Revit\Addins\2025\BIMtegration\scripts\my-scripts.json
                // If migrating from older versions, fall back to legacy location automatically
                string scriptsPath = ResolveScriptsPathForRead();

                // Si no existe, crear directorio y archivo inicial
                if (!File.Exists(scriptsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(scriptsPath));
                    CreateInitialScriptsFile(scriptsPath);
                }

                string jsonContent = File.ReadAllText(scriptsPath);
                
                // Try to deserialize as wrapped structure first (ExportDate, Version, Scripts)
                List<ScriptDefinition> scripts = null;
                try
                {
                    var wrappedData = JsonConvert.DeserializeObject<dynamic>(jsonContent);
                    if (wrappedData != null && wrappedData["Scripts"] != null)
                    {
                        // It's a wrapped structure
                        var scriptsJson = JsonConvert.SerializeObject(wrappedData["Scripts"]);
                        scripts = JsonConvert.DeserializeObject<List<ScriptDefinition>>(scriptsJson);
                    }
                }
                catch
                {
                    // If wrapped deserialization fails, try direct deserialization
                    scripts = null;
                }
                
                // If wrapped deserialization failed, try direct deserialization
                if (scripts == null)
                {
                    scripts = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                }
                
                var scriptsList = scripts ?? new List<ScriptDefinition>();
                
                // Actualizar el estado de favoritos
                FavoritesManager.UpdateFavoriteStatus(scriptsList);

                return OrganizeScriptsByCategory(scriptsList);
            }
            catch (Exception ex)
            {
                // Si hay error, devolver scripts de ejemplo básicos
                return GetFallbackScripts(ex);
            }
        }

        /// <summary>
        /// Guarda un nuevo script en el archivo JSON
        /// </summary>
        public static bool SaveScript(ScriptDefinition newScript)
        {
            try
            {
                var allScripts = LoadAllScriptsFlat();
                
                // Si ya existe un script con el mismo ID, lo reemplaza
                var existingIndex = allScripts.FindIndex(s => s.Id == newScript.Id);
                if (existingIndex >= 0)
                {
                    allScripts[existingIndex] = newScript;
                }
                else
                {
                    allScripts.Add(newScript);
                }

                // Asegurar que existe la carpeta
                var targetPath = EnsureNewPathAndMigrateIfNeeded();
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                // Guardar todos los scripts
                string jsonContent = JsonConvert.SerializeObject(allScripts, Formatting.Indented);
                File.WriteAllText(targetPath, jsonContent);

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Guarda una lista completa de scripts en el archivo JSON
        /// </summary>
        public static bool SaveAllScripts(List<ScriptDefinition> scripts)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ScriptManager: Iniciando SaveAllScripts con {scripts.Count} scripts.");
                string scriptsPath = EnsureNewPathAndMigrateIfNeeded();
                Directory.CreateDirectory(Path.GetDirectoryName(scriptsPath));
                string jsonContent = JsonConvert.SerializeObject(scripts, Formatting.Indented);
                File.WriteAllText(scriptsPath, jsonContent);

                System.Diagnostics.Debug.WriteLine("[DEBUG] ScriptManager: SaveAllScripts completado exitosamente.");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] ScriptManager: ERROR en SaveAllScripts - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Desmarca todos los scripts para que no aparezcan como botones y guarda el cambio.
        /// </summary>
        public static void ClearAllButtonScripts()
        {
            try
            {
                var allScripts = LoadAllScriptsFlat();
                foreach (var s in allScripts)
                {
                    s.ShowAsButton = false;
                }
                var json = JsonConvert.SerializeObject(allScripts, Formatting.Indented);
                string targetPath = EnsureNewPathAndMigrateIfNeeded();
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                File.WriteAllText(targetPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al limpiar scripts de botones: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Busca un script por su ID
        /// </summary>
        public static ScriptDefinition FindScript(string id)
        {
            var allScripts = LoadAllScriptsFlat();
            return allScripts.FirstOrDefault(s => s.Id == id);
        }

        /// <summary>
        /// Alterna el estado de favorito de un script
        /// </summary>
        public static bool ToggleFavorite(string scriptId)
        {
            return FavoritesManager.ToggleFavorite(scriptId);
        }

        /// <summary>
        /// Verifica si un script es favorito
        /// </summary>
        public static bool IsFavorite(string scriptId)
        {
            return FavoritesManager.IsFavorite(scriptId);
        }

        /// <summary>
        /// Carga solo los scripts marcados como favoritos
        /// </summary>
        public static List<ScriptCategory> LoadFavoriteScripts()
        {
            try
            {
                var allScripts = LoadAllScriptsFlat();
                var favoriteScripts = FavoritesManager.FilterFavorites(allScripts);
                return OrganizeScriptsByCategory(favoriteScripts);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading favorite scripts: {ex.Message}");
                return new List<ScriptCategory>();
            }
        }

        /// <summary>
        /// Elimina un script del archivo
        /// </summary>
        public static bool DeleteScript(string id)
        {
            try
            {
                var allScripts = LoadAllScriptsFlat();
                var scriptToRemove = allScripts.FirstOrDefault(s => s.Id == id);
                
                if (scriptToRemove != null)
                {
                    allScripts.Remove(scriptToRemove);
                    
                    // Remover también de favoritos si estaba marcado
                    FavoritesManager.RemoveFromFavorites(id);
                    
                    string jsonContent = JsonConvert.SerializeObject(allScripts, Formatting.Indented);
                    var targetPath = EnsureNewPathAndMigrateIfNeeded();
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.WriteAllText(targetPath, jsonContent);
                    
                    return true;
                }
                
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Organiza una lista plana de scripts en categorías
        /// </summary>
        private static List<ScriptCategory> OrganizeScriptsByCategory(List<ScriptDefinition> scripts)
        {
            var categories = new List<ScriptCategory>();

            var groupedScripts = scripts.GroupBy(s => s.Category ?? "Sin Categoría");

            foreach (var group in groupedScripts.OrderBy(g => g.Key))
            {
                var category = new ScriptCategory(group.Key);
                category.Scripts.AddRange(group.OrderBy(s => s.Name));
                categories.Add(category);
            }

            return categories;
        }

        /// <summary>
        /// Carga todos los scripts como lista plana
        /// </summary>
        public static List<ScriptDefinition> LoadAllScriptsFlat()
        {
            System.Diagnostics.Debug.WriteLine("[DEBUG] ScriptManager: Iniciando LoadAllScriptsFlat.");
            var categories = LoadScripts();
            var allScripts = new List<ScriptDefinition>();
            
            foreach (var category in categories)
            {
                allScripts.AddRange(category.Scripts);
            }
            
            System.Diagnostics.Debug.WriteLine($"[DEBUG] ScriptManager: LoadAllScriptsFlat cargó {allScripts.Count} scripts en total.");
            return allScripts;
        }

        /// <summary>
        /// Carga todos los scripts como lista plana (método privado mantenido para compatibilidad)
        /// </summary>
        private static List<ScriptDefinition> LoadAllScriptsFlatPrivate()
        {
            var categories = LoadScripts();
            var allScripts = new List<ScriptDefinition>();
            
            foreach (var category in categories)
            {
                allScripts.AddRange(category.Scripts);
            }
            
            return allScripts;
        }

        /// <summary>
        /// Crea un archivo de scripts por defecto si no existe
        /// </summary>
        private static void CreateDefaultScriptsFile()
        {
            var projectInfoCode = @"try
{
    var info = new System.Text.StringBuilder();
    info.AppendLine(""📋 INFORMACIÓN DEL PROYECTO"");
    info.AppendLine(""═══════════════════════════════════════"");
    info.AppendLine();
    
    // Información básica del proyecto
    info.AppendLine(""🏗️ PROYECTO:"");
    info.AppendLine($""   • Nombre: {doc.Title}"");
    info.AppendLine($""   • Ruta: {(doc.IsWorkshared ? doc.GetWorksharingCentralModelPath() : doc.PathName)}"");
    info.AppendLine($""   • Guardado: {(doc.IsSaved ? ""✅ Sí"" : ""❌ No guardado"")}"");
    info.AppendLine($""   • Modificado: {(doc.IsModified ? ""✅ Sí"" : ""❌ No"")}"");
    info.AppendLine($""   • Trabajo compartido: {(doc.IsWorkshared ? ""✅ Sí"" : ""❌ No"")}"");
    info.AppendLine();
    
    // Información del autor y organización
    var projectInfo = doc.ProjectInformation;
    info.AppendLine(""👤 AUTORÍA:"");
    info.AppendLine($""   • Autor: {projectInfo.Author}"");
    info.AppendLine($""   • Organización: {projectInfo.OrganizationName}"");
    info.AppendLine($""   • Descripción: {projectInfo.OrganizationDescription}"");
    info.AppendLine($""   • Cliente: {projectInfo.ClientName}"");
    info.AppendLine();
    
    // Información de ubicación
    info.AppendLine(""🌍 UBICACIÓN:"");
    info.AppendLine($""   • Dirección: {projectInfo.Address}"");
    info.AppendLine($""   • Número de proyecto: {projectInfo.Number}"");
    info.AppendLine($""   • Nombre del proyecto: {projectInfo.Name}"");
    
    var siteLocation = doc.SiteLocation;
    if (siteLocation != null)
    {
        info.AppendLine($""   • Latitud: {siteLocation.Latitude:F6}°"");
        info.AppendLine($""   • Longitud: {siteLocation.Longitude:F6}°"");
        info.AppendLine($""   • Zona horaria: {siteLocation.TimeZone}"");
    }
    info.AppendLine();
    
    // Unidades del proyecto
    var units = doc.GetUnits();
    info.AppendLine(""📏 UNIDADES:"");
    info.AppendLine($""   • Longitud: {units.GetFormatOptions(SpecTypeId.Length).GetUnitTypeId().TypeId}"");
    info.AppendLine($""   • Área: {units.GetFormatOptions(SpecTypeId.Area).GetUnitTypeId().TypeId}"");
    info.AppendLine($""   • Volumen: {units.GetFormatOptions(SpecTypeId.Volume).GetUnitTypeId().TypeId}"");
    info.AppendLine($""   • Ángulo: {units.GetFormatOptions(SpecTypeId.Angle).GetUnitTypeId().TypeId}"");
    info.AppendLine();
    
    // Estadísticas del modelo
    var elements = new FilteredElementCollector(doc).WhereElementIsNotElementType().ToElements();
    var views = new FilteredElementCollector(doc).OfClass(typeof(View)).ToElements().Count;
    var levels = new FilteredElementCollector(doc).OfClass(typeof(Level)).ToElements().Count;
    var materials = new FilteredElementCollector(doc).OfClass(typeof(Material)).ToElements().Count;
    
    info.AppendLine(""📊 ESTADÍSTICAS:"");
    info.AppendLine($""   • Total elementos: {elements.Count:N0}"");
    info.AppendLine($""   • Vistas: {views:N0}"");
    info.AppendLine($""   • Niveles: {levels:N0}"");
    info.AppendLine($""   • Materiales: {materials:N0}"");
    info.AppendLine();
    
    // Información de fases
    var phases = new FilteredElementCollector(doc).OfClass(typeof(Phase)).ToElements();
    if (phases.Any())
    {
        info.AppendLine(""🔄 FASES:"");
        foreach (Phase phase in phases)
        {
            info.AppendLine($""   • {phase.Name}"");
        }
        info.AppendLine();
    }
    
    // Versión de Revit
    info.AppendLine(""⚙️ APLICACIÓN:"");
    info.AppendLine($""   • Versión de Revit: {app.VersionName}"");
    info.AppendLine($""   • Número de build: {app.VersionBuild}"");
    info.AppendLine($""   • Idioma: {app.Language}"");
    
    TaskDialog.Show(""Información del Proyecto"", info.ToString());
    return ""✅ Información del proyecto mostrada correctamente."";
}
catch (Exception ex)
{
    TaskDialog.Show(""Error"", $""Error al obtener información del proyecto: {ex.Message}"");
    return $""❌ Error: {ex.Message}"";
}";

            var defaultScripts = new List<ScriptDefinition>
            {
                new ScriptDefinition("project-info", "Información del Proyecto", "Muestra información detallada del proyecto activo de Revit", "Inicio", "info.png", projectInfoCode)
            };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(NewScriptsPath));
                string jsonContent = JsonConvert.SerializeObject(defaultScripts, Formatting.Indented);
                File.WriteAllText(NewScriptsPath, jsonContent);
            }
            catch (Exception)
            {
                // Ignorar errores al crear archivo por defecto
            }
        }

        /// <summary>
        /// Determina qué ruta usar para leer scripts, priorizando la nueva y usando la antigua si existe.
        /// </summary>
        private static string ResolveScriptsPathForRead()
        {
            try
            {
                if (File.Exists(NewScriptsPath)) return NewScriptsPath;
                if (File.Exists(LegacyScriptsPath)) return LegacyScriptsPath;
            }
            catch { /* ignore */ }
            return NewScriptsPath;
        }

        /// <summary>
        /// Garantiza la ruta nueva y migra contenido desde la ruta antigua si aplica.
        /// </summary>
        private static string EnsureNewPathAndMigrateIfNeeded()
        {
            try
            {
                // Si ya existe la nueva, solo devolverla
                if (File.Exists(NewScriptsPath)) return NewScriptsPath;

                // Si existe la antigua pero no la nueva, migrar
                if (File.Exists(LegacyScriptsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(NewScriptsPath));
                    File.Copy(LegacyScriptsPath, NewScriptsPath, overwrite: true);
                    return NewScriptsPath;
                }

                // Ninguna existe: crear carpeta para la nueva
                Directory.CreateDirectory(Path.GetDirectoryName(NewScriptsPath));
                return NewScriptsPath;
            }
            catch
            {
                // En caso de error, intentar al menos devolver la nueva ruta
                return NewScriptsPath;
            }
        }
        
        /// <summary>
        /// Si no existe un archivo de scripts en la carpeta de usuario (AppData), pero existe
        /// una copia global instalada por el instalador en ProgramData, copia esa copia a AppData.
        /// Esto permite que cada usuario tenga su propia copia editable y evita borrar la
        /// copia global durante las actualizaciones.
        /// </summary>
        private static void TryMigrateGlobalScriptsToUser(string userScriptsPath)
        {
            try
            {
                // Si ya existe el archivo en AppData, nada que hacer
                if (File.Exists(userScriptsPath)) return;

                // Si existe la copia global en ProgramData, copiarla a AppData
                if (File.Exists(GlobalScriptsPath))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(userScriptsPath));
                    File.Copy(GlobalScriptsPath, userScriptsPath, overwrite: false);
                }
            }
            catch
            {
                // No bloquear el inicio si hay cualquier error; seguimos sin migrar.
            }
        }

        /// <summary>
        /// Devuelve scripts de emergencia si falla la carga
        /// </summary>
        private static List<ScriptCategory> GetFallbackScripts(Exception error)
        {
            var fallbackCategory = new ScriptCategory("Error");
            fallbackCategory.Scripts.Add(new ScriptDefinition(
                "error-info", 
                "Error de Carga", 
                "Información sobre el error al cargar scripts",
                "Error", 
                "error.png",
                $"return \"❌ Error al cargar scripts:\\n{error.Message}\\n\\nUsa el editor para crear scripts nuevos.\";"
            ));

            return new List<ScriptCategory> { fallbackCategory };
        }

        /// <summary>
        /// Carga todos los scripts como lista plana (alias para compatibilidad)
        /// </summary>
        public static List<ScriptDefinition> LoadAllScripts()
        {
            return LoadAllScriptsFlat();
        }

        /// <summary>
        /// Obtiene la ruta efectiva del archivo JSON de scripts, priorizando la carpeta del repositorio si existe
        /// </summary>
        public static string GetScriptsFilePath()
        {
            // Devolver la ruta efectiva considerando migración (nueva o legado si existe)
            return ResolveScriptsPathForRead();
        }

        

        #region Gestión de Scripts de Botones

        private static readonly string ButtonScriptsPath = Path.Combine(
            Path.GetDirectoryName(typeof(ScriptManager).Assembly.Location),
            "Scripts",
            "button-scripts.json"
        );

        private static readonly string BackupButtonScriptsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), 
            "RoslynCopilotTest", 
            "button-scripts.json"
        );

        /// <summary>
        /// Carga los scripts que deben mostrarse como botones en la pestaña básica
        /// </summary>
        public static List<ScriptDefinition> LoadButtonScripts()
        {
            try
            {
                // Cargar todos los scripts y filtrar los que están marcados para botones
                var allScripts = LoadAllScriptsFlat();
                var buttonScripts = allScripts.Where(s => s.ShowAsButton).ToList();
                return buttonScripts;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al cargar scripts de botones: {ex.Message}");
                return new List<ScriptDefinition>(); // Devolver lista vacía en caso de error
            }
        }


            // Ya no se usa lógica de raíz de repo, solo AppData

            // Ya no se usa, solo AppData

            // Ya no se usa, solo AppData
        /// <summary>
        /// Guarda los scripts que deben mostrarse como botones
        /// </summary>
        public static void SaveButtonScripts(List<ScriptDefinition> buttonScripts)
        {
            try
            {
                // Cargar todos los scripts existentes
                var allScripts = LoadAllScriptsFlat();
                
                // Primero, marcar todos como no-botón
                foreach (var script in allScripts)
                {
                    script.ShowAsButton = false;
                }
                
                // Luego marcar los seleccionados como botones
                var buttonIds = new HashSet<string>(buttonScripts.Select(s => s.Id));
                foreach (var script in allScripts.Where(s => buttonIds.Contains(s.Id)))
                {
                    script.ShowAsButton = true;
                }
                
                // Guardar todos los scripts actualizados
                SaveAllScripts(allScripts);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al guardar configuración de botones: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea un archivo inicial de scripts con algunos ejemplos básicos
        /// </summary>
        private static void CreateInitialScriptsFile(string scriptsPath)
        {
            var initialScripts = new List<ScriptDefinition>
            {
                new ScriptDefinition
                {
                    Id = "info-proyecto-inicial",
                    Name = "Información del Proyecto",
                    Description = "Muestra información básica del proyecto actual",
                    Category = "Información",
                    Icon = null,
                    Code = @"try
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($""📂 Proyecto: {doc.Title}"");
    sb.AppendLine($""📍 Ruta: {doc.PathName}"");
    sb.AppendLine($""🔧 Revit: {app.VersionName}"");
    sb.AppendLine($""📅 Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}"");
    return sb.ToString();
}
catch (Exception ex)
{
    return $""❌ Error: {ex.Message}"";
}",
                    ShowAsButton = true,
                    IsFavorite = false,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                },
                new ScriptDefinition
                {
                    Id = "contar-muros-inicial",
                    Name = "Contar Muros",
                    Description = "Cuenta el total de muros en el proyecto",
                    Category = "Información",
                    Icon = null,
                    Code = @"int wallCount = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .GetElementCount();

return $""Total de muros: {wallCount}"";",
                    ShowAsButton = true,
                    IsFavorite = false,
                    CreatedDate = DateTime.Now,
                    LastModified = DateTime.Now
                }
            };

            var json = JsonConvert.SerializeObject(initialScripts, Formatting.Indented);
            File.WriteAllText(scriptsPath, json);
        }

        /// <summary>
        /// Integra scripts premium descargados en la lista de scripts locales
        /// Marca cada script con la categoría "🔒 [NombreEmpresa]"
        /// Respeta cambios locales si el script ya existe
        /// </summary>
        public static bool MergePremiumButtons(List<ScriptDefinition> premiumScripts)
        {
            if (premiumScripts == null || premiumScripts.Count == 0)
                return true;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Premium Merge] Iniciando merge de {premiumScripts.Count} scripts premium");

                // Cargar scripts existentes
                var allScripts = LoadAllScriptsFlat();

                // Marcar y procesar cada script premium
                int merged = 0;
                int updated = 0;
                int added = 0;

                foreach (var premiumScript in premiumScripts)
                {
                    // Determinar categoría con marca de premium y empresa
                    string company = premiumScript.Category?.Contains("🔒") == true 
                        ? premiumScript.Category 
                        : $"🔒 {premiumScript.Category ?? "Premium"}";
                    
                    premiumScript.Category = company;
                    premiumScript.ShowAsButton = true; // Los botones premium se muestran por defecto

                    // Buscar si ya existe este script (por ID)
                    var existingScript = allScripts.FirstOrDefault(s => s.Id == premiumScript.Id);

                    if (existingScript != null)
                    {
                        // Script ya existe: actualizar solo código, nombre, descripción, e icono
                        // Preservar: IsFavorite, ShowAsButton si fue modificado localmente
                        existingScript.Code = premiumScript.Code;
                        existingScript.Name = premiumScript.Name;
                        existingScript.Description = premiumScript.Description;
                        existingScript.Icon = premiumScript.Icon;
                        existingScript.Category = company;
                        existingScript.LastModified = DateTime.Now;
                        
                        updated++;
                        System.Diagnostics.Debug.WriteLine($"[Premium Merge] ✓ Actualizado: {premiumScript.Name}");
                    }
                    else
                    {
                        // Nuevo script premium
                        premiumScript.CreatedDate = DateTime.Now;
                        premiumScript.LastModified = DateTime.Now;
                        allScripts.Add(premiumScript);
                        
                        added++;
                        System.Diagnostics.Debug.WriteLine($"[Premium Merge] ➕ Agregado: {premiumScript.Name}");
                    }

                    merged++;
                }

                // Guardar todos los scripts (incluyendo los nuevos)
                bool saved = SaveAllScripts(allScripts);

                if (saved)
                {
                    System.Diagnostics.Debug.WriteLine($"[Premium Merge] ✅ Merge completado: +{added} nuevos, ~{updated} actualizados (Total: {merged} procesados)");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[Premium Merge] ⚠️ Error guardando scripts después del merge");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium Merge] ❌ Error: {ex.Message}");
                return false;
            }
        }

        #endregion
    }
}