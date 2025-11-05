using System;
using System.Windows;
using Autodesk.Revit.UI;

namespace RoslynCopilotTest.UI
{
    /// <summary>
    /// Provider que crea el contenido del DockablePane
    /// </summary>
    public class ScriptPanelProvider : IDockablePaneProvider
    {
        private UIControlledApplication _uiApp;
        private ScriptPanel _scriptPanel;

        /// <summary>
        /// ID del panel para referencia global
        /// </summary>
        public static DockablePaneId PanelId { get; set; }

        /// <summary>
        /// Inicializa la UIApplication estática para uso de scripts
        /// </summary>
        public static void InitializeUIApplication(UIApplication uiApp)
        {
            Application.CurrentUIApplication = uiApp;
        }

        public ScriptPanelProvider(UIControlledApplication uiApp)
        {
            _uiApp = uiApp;
        }

        /// <summary>
        /// Configuración del panel
        /// </summary>
        public void SetupDockablePane(DockablePaneProviderData data)
        {
            data.FrameworkElement = GetPanelContent();
            data.InitialState = new DockablePaneState
            {
                DockPosition = DockPosition.Tabbed,
                TabBehind = DockablePanes.BuiltInDockablePanes.ProjectBrowser
            };
            
            // No mostrar el panel automáticamente - que el usuario lo active manualmente
            data.VisibleByDefault = false;
        }

        /// <summary>
        /// Crea el contenido del panel
        /// </summary>
        private FrameworkElement GetPanelContent()
        {
            if (_scriptPanel == null)
            {
                // Crear el panel de scripts
                _scriptPanel = new ScriptPanel();
            }

            return _scriptPanel;
        }

        /// <summary>
        /// Muestra el panel de scripts
        /// </summary>
        public static void ShowPanel(UIApplication uiApp)
        {
            // Asegurar que la UIApplication esté inicializada
            InitializeUIApplication(uiApp);
            
            if (PanelId != null)
            {
                try
                {
                    var dockablePane = uiApp.GetDockablePane(PanelId);
                    if (dockablePane != null)
                    {
                        if (dockablePane.IsShown())
                        {
                            dockablePane.Hide();
                        }
                        else
                        {
                            dockablePane.Show();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Unable to show panel: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Verifica si el panel está visible
        /// </summary>
        public static bool IsPanelVisible()
        {
            if (PanelId != null && Application.CurrentUIApplication != null)
            {
                try
                {
                    var dockablePane = Application.CurrentUIApplication.GetDockablePane(PanelId);
                    return dockablePane?.IsShown() ?? false;
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }
    }
}