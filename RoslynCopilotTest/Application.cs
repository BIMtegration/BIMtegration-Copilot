using System;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using RoslynCopilotTest.UI;
using RoslynCopilotTest.Services;

namespace RoslynCopilotTest
{
    /// <summary>
    /// Clase principal de la aplicación que se carga cuando se inicia Revit
    /// </summary>
    public class Application : IExternalApplication
    {
        /// <summary>
        /// Referencia estática a la UIApplication actual
        /// </summary>
        public static UIApplication CurrentUIApplication { get; set; }

        /// <summary>
        /// Referencia estática al botón del panel para cambiar su texto
        /// </summary>
        public static PushButton PanelToggleButton { get; set; }

        /// <summary>
        /// Referencia estática al ControlledApplication para configuraciones
        /// </summary>
        public static Autodesk.Revit.ApplicationServices.ControlledApplication CurrentControlledApplication { get; set; }

        /// <summary>
        /// Handler genérico para scripts dinámicos y botones personalizados
        /// </summary>
        public static GenericExternalEventHandler SharedExternalEventHandler = new GenericExternalEventHandler();
        public static ExternalEvent SharedExternalEvent = ExternalEvent.Create(SharedExternalEventHandler);

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Inicializar servicios de tema e idioma
                InitializeThemeAndLocalization(application);
                
                // Guardar referencia a ControlledApplication
                CurrentControlledApplication = application.ControlledApplication;
                
                // Registrar DockablePane para el panel de scripts dinámicos
                RegisterScriptPanel(application);
                
                // Registrar evento para inicializar UIApplication cuando esté disponible
                application.ControlledApplication.DocumentOpened += OnDocumentOpened;
                application.Idling += OnApplicationIdling;

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("BIMtegration - Load error", $"Error: {ex.Message}");
                return Result.Failed;
            }
        }
        
        /// <summary>
        /// Evento que se ejecuta cuando se abre un documento
        /// </summary>
        private void OnDocumentOpened(object sender, Autodesk.Revit.DB.Events.DocumentOpenedEventArgs e)
        {
            try
            {
                // Obtener UIApplication del documento abierto
                if (CurrentUIApplication == null && e.Document?.Application != null)
                {
                    // Crear UIApplication a partir de la Application del documento
                    var application = e.Document.Application;
                    CurrentUIApplication = new UIApplication(application);
                    
                    // Inicializar ThemeManager con la UIApplication
                    ThemeManager.Initialize(CurrentUIApplication);
                    
                    // También inicializar en ScriptPanelProvider
                    ScriptPanelProvider.InitializeUIApplication(CurrentUIApplication);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnDocumentOpened: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Evento Idling para inicializar UIApplication en el primer ciclo
        /// </summary>
        private void OnApplicationIdling(object sender, Autodesk.Revit.UI.Events.IdlingEventArgs e)
        {
            try
            {
                // Desregistrar el evento después del primer uso
                if (sender is UIControlledApplication uiApp)
                {
                    uiApp.Idling -= OnApplicationIdling;
                }
                
                // El panel se mostrará automáticamente cuando se ejecute el primer comando
                // o cuando se abra un documento, no necesitamos hacer nada aquí
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error en OnApplicationIdling: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Inicializa UIApplication si está disponible
        /// </summary>
        private void InitializeUIApplicationIfNeeded()
        {
            // Este método se mantendrá vacío por ahora
            // La UIApplication se inicializará cuando se ejecute el primer comando
        }

        /// <summary>
        /// Muestra el panel automáticamente si hay una UIApplication disponible
        /// </summary>
        private void ShowPanelAutomatically()
        {
            try
            {
                // Solo mostrar si tenemos una UIApplication válida
                if (CurrentUIApplication != null)
                {
                    // Verificar si el panel ya está visible para evitar duplicados
                    if (!ScriptPanelProvider.IsPanelVisible())
                    {
                        // Mostrar el panel
                        ScriptPanelProvider.ShowPanel(CurrentUIApplication);
                        
                        // Actualizar el texto del botón
                        // No text toggling; keep icon-only button
                    }
                }
            }
            catch (Exception ex)
            {
                // Silenciar errores para evitar interrumpir el flujo de Revit
                System.Diagnostics.Debug.WriteLine($"Error mostrando panel automáticamente: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicializa los servicios de tema e idioma
        /// </summary>
        private void InitializeThemeAndLocalization(UIControlledApplication application)
        {
            try
            {
                // Guardar referencia para uso posterior
                CurrentControlledApplication = application.ControlledApplication;
                
                // Detectar y aplicar el idioma de Revit
                LocalizationManager.DetectRevitLanguage(application.ControlledApplication);
                
                // Detectar y aplicar el tema de Revit
                ThemeManager.DetectAndApplyRevitTheme();
            }
            catch (Exception ex)
            {
                // Si hay error, usar configuración por defecto
                System.Diagnostics.Debug.WriteLine($"Error inicializando tema/idioma: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra el panel acoplable de scripts dinámicos
        /// </summary>
        private void RegisterScriptPanel(UIControlledApplication application)
        {
            try
            {
                // Crear un ID único para el panel
                DockablePaneId panelId = new DockablePaneId(new Guid("a7b5c9d2-4e8f-1a3c-b6e9-5d7f2c8a9b1e"));

                // Registrar el panel acoplable
                application.RegisterDockablePane(
                    panelId,
                    "BIMtegration",  // Title shown on the panel tab
                    new ScriptPanelProvider(application)  // Provider que crea el contenido
                );

                // Crear botón en la pestaña Add-ins nativa
                var showPanelButton = new PushButtonData(
                    "ShowCodeAssistant",
                        "Copilot",
                    System.Reflection.Assembly.GetExecutingAssembly().Location,
                    "RoslynCopilotTest.ShowScriptPanelCommand"
                );

                showPanelButton.ToolTip = "Show/Hide dynamic scripts panel";
                showPanelButton.LongDescription = "Panel with dynamic buttons organized by categories to execute custom C# scripts.";
                
                // Establecer icono personalizado con "B" azul y "BIMtegration"
                showPanelButton.Image = CreateBIMtegrationIcon(16);
                showPanelButton.LargeImage = CreateBIMtegrationIcon(32);

                // Crear nuestro panel personalizado
                RibbonPanel addinPanel = application.CreateRibbonPanel("BIMtegration");

                if (addinPanel != null)
                {
                    var pushButton = addinPanel.AddItem(showPanelButton) as PushButton;
                    
                    // Guardar referencia para cambiar texto dinámicamente
                    PanelToggleButton = pushButton;
                }

                // Guardar el ID del panel para uso posterior
                ScriptPanelProvider.PanelId = panelId;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("BIMtegration - Panel registration error", $"Could not register the scripts panel: {ex.Message}");
            }
        }

        /// <summary>
            /// Crea un icono personalizado con una "B" azul grande
        /// </summary>
        private BitmapSource CreateBIMtegrationIcon(int size)
        {
            try
            {
                // Crear un DrawingVisual para dibujar el icono
                var drawingVisual = new DrawingVisual();
                using (DrawingContext dc = drawingVisual.RenderOpen())
                {
                    // Fondo blanco
                    dc.DrawRectangle(
                        Brushes.White,
                        null,
                        new System.Windows.Rect(0, 0, size, size)
                    );

                        // Configurar tamaño de la "B" - ocupa 70% del icono
                        double bFontSize = size * 0.7;
                    
                    // Crear la "B" grande en azul
                    var bText = new FormattedText(
                        "B",
                        System.Globalization.CultureInfo.CurrentCulture,
                        System.Windows.FlowDirection.LeftToRight,
                        new Typeface(new System.Windows.Media.FontFamily("Arial"), 
                                    System.Windows.FontStyles.Normal, 
                                    System.Windows.FontWeights.Bold, 
                                    System.Windows.FontStretches.Normal),
                        bFontSize,
                        Brushes.DodgerBlue,
                        96  // DPI
                    );

                        // Centrar la "B" completamente en el icono
                    double bX = (size - bText.Width) / 2;
                        double bY = (size - bText.Height) / 2;
                    dc.DrawText(bText, new System.Windows.Point(bX, bY));
                }

                // Renderizar a bitmap
                var renderBitmap = new RenderTargetBitmap(
                    size, size, 96, 96, PixelFormats.Pbgra32
                );
                renderBitmap.Render(drawingVisual);

                return renderBitmap;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creando icono: {ex.Message}");
                return null;
            }
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            // Limpiar la sesión de autenticación al cerrar Revit
            try
            {
                var authService = new BIMAuthService();
                authService.Logout();
                System.Diagnostics.Debug.WriteLine("Sesión de BIMtegration limpiada al cerrar Revit");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error limpiando sesión al cerrar: {ex.Message}");
            }
            
            return Result.Succeeded;
        }

        /// <summary>
        /// Proporciona acceso global al handler y evento para scripts dinámicos
        /// </summary>
        public static GenericExternalEventHandler GetSharedHandler() => SharedExternalEventHandler;
        public static ExternalEvent GetSharedExternalEvent() => SharedExternalEvent;

    /// <summary>
    /// Ruta seleccionada por el diálogo Katalog para ser procesada por el handler
    /// </summary>
    public static string SharedKatalogSelectionPath { get; set; }
    }
}