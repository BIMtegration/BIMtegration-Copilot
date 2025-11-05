using System;
using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.ApplicationServices;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Gestor de localización para la aplicación
    /// </summary>
    public static class LocalizationManager
    {
        public static event EventHandler LanguageChanged;
        
        private static string _currentLanguage = "en-US";
        private static Dictionary<string, Dictionary<string, string>> _translations;
        
        static LocalizationManager()
        {
            InitializeTranslations();
        }
        
        public static string CurrentLanguage => _currentLanguage;
        
        /// <summary>
        /// Detecta y establece el idioma basado en Revit
        /// </summary>
        public static void DetectRevitLanguage(ControlledApplication revitApp)
        {
            try
            {
                // Detectar el idioma de Revit
                var revitLanguage = GetRevitLanguage(revitApp);
                SetLanguage(revitLanguage);
            }
            catch
            {
                // Si hay error, usar inglés por defecto
                SetLanguage("en-US");
            }
        }

        /// <summary>
        /// Detecta y establece el idioma basado en Revit
        /// </summary>
        public static void DetectRevitLanguage(Application revitApp)
        {
            try
            {
                // Detectar el idioma de Revit
                var revitLanguage = GetRevitLanguageFromApplication(revitApp);
                SetLanguage(revitLanguage);
            }
            catch
            {
                // Si hay error, usar inglés por defecto
                SetLanguage("en-US");
            }
        }
        
        /// <summary>
        /// Establece el idioma de la aplicación
        /// </summary>
        public static void SetLanguage(string languageCode)
        {
            _currentLanguage = languageCode;
            LanguageChanged?.Invoke(null, EventArgs.Empty);
        }
        
        /// <summary>
        /// Obtiene una cadena traducida
        /// </summary>
        public static string GetString(string key, string defaultValue = null)
        {
            if (_translations?.ContainsKey(_currentLanguage) == true &&
                _translations[_currentLanguage]?.ContainsKey(key) == true)
            {
                return _translations[_currentLanguage][key];
            }
            
            // Intentar con inglés como fallback
            if (_currentLanguage != "en-US" && 
                _translations?.ContainsKey("en-US") == true &&
                _translations["en-US"]?.ContainsKey(key) == true)
            {
                return _translations["en-US"][key];
            }
            
            return defaultValue ?? key;
        }
        
        /// <summary>
        /// Detecta el idioma de Revit desde ControlledApplication
        /// </summary>
        private static string GetRevitLanguage(ControlledApplication app)
        {
            try
            {
                // Para ControlledApplication, usamos la configuración por defecto
                // ya que no tiene acceso directo a Language
                return "en-US"; // Por defecto
            }
            catch
            {
                return "en-US";
            }
        }

        /// <summary>
        /// Detecta el idioma de Revit desde Application
        /// </summary>
        private static string GetRevitLanguageFromApplication(Application app)
        {
            try
            {
                // Para Application, no hay acceso directo a Language
                // Usamos la cultura del sistema como fallback
                return CultureInfo.CurrentUICulture.Name.StartsWith("es") ? "es-ES" : "en-US";
            }
            catch
            {
                // Fallback: detectar por cultura del sistema
                var culture = CultureInfo.CurrentUICulture.Name;
                return culture;
            }
        }
        
        /// <summary>
        /// Inicializa las traducciones
        /// </summary>
        private static void InitializeTranslations()
        {
            _translations = new Dictionary<string, Dictionary<string, string>>
            {
                ["en-US"] = new Dictionary<string, string>
                {
                    // General
                    ["Scripts"] = "Scripts",
                    ["Buttons"] = "Buttons",
                    ["Settings"] = "Settings",
                    ["Search"] = "Search",
                    ["Filter"] = "Filter",
                    ["Clear"] = "Clear",
                    ["Favorites"] = "Favorites",
                    ["Execute"] = "Execute",
                    ["Edit"] = "Edit",
                    ["Delete"] = "Delete",
                    ["Add"] = "Add",
                    ["Save"] = "Save",
                    ["Cancel"] = "Cancel",
                    ["Loading"] = "Loading...",
                    ["NoScripts"] = "No scripts found matching the current filters.",
                    
                    // Themes
                    ["Theme"] = "Theme",
                    ["Light"] = "Light",
                    ["Dark"] = "Dark",
                    ["Auto"] = "Auto (Follow Revit)",
                    
                    // Language
                    ["Language"] = "Language",
                    ["AutoDetect"] = "Auto-detect from Revit",
                    
                    // AI
                    ["AIAssist"] = "AI Assist",
                    ["GenerateCode"] = "Generate Code with AI"
                },
                
                ["es-ES"] = new Dictionary<string, string>
                {
                    // General
                    ["Scripts"] = "Scripts",
                    ["Buttons"] = "Botones",
                    ["Settings"] = "Configuración",
                    ["Search"] = "Buscar",
                    ["Filter"] = "Filtrar",
                    ["Clear"] = "Limpiar",
                    ["Favorites"] = "Favoritos",
                    ["Execute"] = "Ejecutar",
                    ["Edit"] = "Editar",
                    ["Delete"] = "Eliminar",
                    ["Add"] = "Agregar",
                    ["Save"] = "Guardar",
                    ["Cancel"] = "Cancelar",
                    ["Loading"] = "Cargando...",
                    ["NoScripts"] = "No se encontraron scripts que coincidan con los filtros actuales.",
                    
                    // Themes
                    ["Theme"] = "Tema",
                    ["Light"] = "Claro",
                    ["Dark"] = "Oscuro",
                    ["Auto"] = "Automático (Seguir Revit)",
                    
                    // Language
                    ["Language"] = "Idioma",
                    ["AutoDetect"] = "Detectar automáticamente desde Revit",
                    
                    // AI
                    ["AIAssist"] = "Asistente IA",
                    ["GenerateCode"] = "Generar Código con IA"
                }
            };
        }
        
        /// <summary>
        /// Obtiene los idiomas disponibles
        /// </summary>
        public static Dictionary<string, string> GetAvailableLanguages()
        {
            return new Dictionary<string, string>
            {
                ["en-US"] = "English",
                ["es-ES"] = "Español"
            };
        }
    }
}