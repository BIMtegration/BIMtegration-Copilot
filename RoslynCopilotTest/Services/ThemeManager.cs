using System;
using System.Windows;
using System.Windows.Media;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.UI;
using WpfApp = System.Windows.Application;

namespace RoslynCopilotTest.Services
{
    /// <summary>
    /// Gestor de temas para la aplicación
    /// </summary>
    public static class ThemeManager
    {
        public static event EventHandler ThemeChanged;
        
        private static UIApplication _uiApp;
        
        /// <summary>
        /// Inicializa el ThemeManager con la aplicación de Revit
        /// </summary>
        public static void Initialize(UIApplication uiApp)
        {
            _uiApp = uiApp;
            DetectAndApplyRevitTheme();
        }
        
        /// <summary>
        /// Detecta si Revit/Windows está en modo oscuro
        /// </summary>
        private static bool IsRevitInDarkMode()
        {
            try
            {
                // Método principal: detectar tema de Windows usando el registro
                // Esto funciona cuando Revit usa "Usar configuración del sistema"
                try
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                    {
                        if (key != null)
                        {
                            var appsUseLightTheme = key.GetValue("AppsUseLightTheme");
                            if (appsUseLightTheme != null)
                            {
                                bool isLight = (int)appsUseLightTheme == 1;
                                bool isDark = !isLight;
                                System.Diagnostics.Debug.WriteLine($"=== Windows Theme: {(isDark ? "DARK" : "LIGHT")} ===");
                                System.Diagnostics.Debug.WriteLine($"AppsUseLightTheme registry value: {appsUseLightTheme}");
                                return isDark;
                            }
                        }
                    }
                }
                catch (Exception regEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Registry read error: {regEx.Message}");
                }
                
                // Método alternativo: revisar el color de fondo de los controles de sistema
                var backgroundColor = SystemColors.WindowColor;
                var brightness = (backgroundColor.R + backgroundColor.G + backgroundColor.B) / 3.0;
                System.Diagnostics.Debug.WriteLine($"Fallback - System Window Color: R={backgroundColor.R}, G={backgroundColor.G}, B={backgroundColor.B}");
                System.Diagnostics.Debug.WriteLine($"Brightness: {brightness}");
                
                bool isDarkByColor = brightness < 128;
                System.Diagnostics.Debug.WriteLine($"=== DETECTED: {(isDarkByColor ? "DARK" : "LIGHT")} MODE (by color) ===");
                
                return isDarkByColor;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR detecting theme: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Detecta y aplica el tema basado en Revit (siempre automático)
        /// </summary>
        public static void DetectAndApplyRevitTheme()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine(">>> DetectAndApplyRevitTheme called");
                var isDarkTheme = IsRevitInDarkMode();
                System.Diagnostics.Debug.WriteLine($">>> Applying theme: {(isDarkTheme ? "DARK" : "LIGHT")}");
                ApplyTheme(isDarkTheme);
                ThemeChanged?.Invoke(null, EventArgs.Empty);
                System.Diagnostics.Debug.WriteLine(">>> Theme applied and event fired");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($">>> ERROR in DetectAndApplyRevitTheme: {ex.Message}");
                // Si hay error, usar tema claro por defecto
                ApplyTheme(false);
            }
        }
        
        /// <summary>
        /// Aplica el tema especificado
        /// </summary>
        private static void ApplyTheme(bool isDark)
        {
            // Actualizar recursos de la aplicación
            UpdateApplicationResources(isDark);
        }
        
        /// <summary>
        /// Actualiza los recursos de la aplicación con el tema
        /// </summary>
        private static void UpdateApplicationResources(bool isDark)
        {
            var app = WpfApp.Current;
            if (app?.Resources == null) return;
            
            // Colores para tema oscuro
            if (isDark)
            {
                app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
                app.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(241, 241, 241));
                app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(63, 63, 70));
                app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 122, 204));
                app.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
                app.Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(62, 62, 64));
                app.Resources["SelectedBrush"] = new SolidColorBrush(Color.FromRgb(51, 153, 255));
                
                // Colores específicos para ComboBox en tema oscuro
                app.Resources["ComboBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // Fondo blanco
                app.Resources["ComboBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Texto negro
                app.Resources["ComboBoxBorderBrush"] = new SolidColorBrush(Color.FromRgb(122, 122, 122)); // Borde gris
            }
            // Colores para tema claro
            else
            {
                app.Resources["BackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                app.Resources["ForegroundBrush"] = new SolidColorBrush(Color.FromRgb(33, 33, 33));
                app.Resources["BorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204));
                app.Resources["AccentBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                app.Resources["SecondaryBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(248, 248, 248));
                app.Resources["HoverBrush"] = new SolidColorBrush(Color.FromRgb(229, 229, 229));
                app.Resources["SelectedBrush"] = new SolidColorBrush(Color.FromRgb(0, 120, 215));
                
                // Colores específicos para ComboBox en tema claro
                app.Resources["ComboBoxBackgroundBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 255)); // Fondo blanco
                app.Resources["ComboBoxForegroundBrush"] = new SolidColorBrush(Color.FromRgb(0, 0, 0)); // Texto negro
                app.Resources["ComboBoxBorderBrush"] = new SolidColorBrush(Color.FromRgb(204, 204, 204)); // Borde gris claro
            }
        }
        
        /// <summary>
        /// Obtiene el color de fondo actual
        /// </summary>
        public static Brush GetBackgroundBrush()
        {
            var app = WpfApp.Current;
            if (app?.Resources?.Contains("BackgroundBrush") == true)
            {
                return (Brush)app.Resources["BackgroundBrush"];
            }
            
            return SystemColors.WindowBrush;
        }
        
        /// <summary>
        /// Obtiene el color de texto actual
        /// </summary>
        public static Brush GetForegroundBrush()
        {
            var app = WpfApp.Current;
            if (app?.Resources?.Contains("ForegroundBrush") == true)
            {
                return (Brush)app.Resources["ForegroundBrush"];
            }
            
            return SystemColors.WindowTextBrush;
        }
        
        /// <summary>
        /// Obtiene el color de fondo para ComboBox
        /// </summary>
        public static Brush GetComboBoxBackgroundBrush()
        {
            var app = WpfApp.Current;
            if (app?.Resources?.Contains("ComboBoxBackgroundBrush") == true)
            {
                return (Brush)app.Resources["ComboBoxBackgroundBrush"];
            }
            
            return new SolidColorBrush(Color.FromRgb(255, 255, 255));
        }
        
        /// <summary>
        /// Obtiene el color de texto para ComboBox
        /// </summary>
        public static Brush GetComboBoxForegroundBrush()
        {
            var app = WpfApp.Current;
            if (app?.Resources?.Contains("ComboBoxForegroundBrush") == true)
            {
                return (Brush)app.Resources["ComboBoxForegroundBrush"];
            }
            
            return new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }
        
        /// <summary>
        /// Obtiene el color de borde para ComboBox
        /// </summary>
        public static Brush GetComboBoxBorderBrush()
        {
            var app = WpfApp.Current;
            if (app?.Resources?.Contains("ComboBoxBorderBrush") == true)
            {
                return (Brush)app.Resources["ComboBoxBorderBrush"];
            }
            
            return new SolidColorBrush(Color.FromRgb(204, 204, 204));
        }
    }
}