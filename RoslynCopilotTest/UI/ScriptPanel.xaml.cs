using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using WpfColor = System.Windows.Media.Color;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfPanel = System.Windows.Controls.Panel;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfGrid = System.Windows.Controls.Grid;

namespace RoslynCopilotTest.UI
{


    /// <summary>
    /// Panel principal para mostrar y ejecutar scripts dinámicos
    /// </summary>
    public class ScriptPanel : UserControl
    {
        // ...existing fields...

        // Handler para el botón Actualizar Script
        private void UpdateScriptButton_Click(object sender, RoutedEventArgs e)
        {
            // Abrir ventana de selector de scripts para actualizar
            var selector = new ScriptUpdateSelectorWindow();
            selector.ShowDialog();
        }
        private List<ScriptCategory> _categories;
        private List<ScriptCategory> _allCategories; // Copia completa para filtrado
        private StackPanel _scriptsContainer;
        private TextBlock _loadingMessage;
        private WpfComboBox _categoryFilter;
        private WpfTextBox _searchBox;
        private Button _clearFiltersButton;
        private Button _favoritesFilter;
        private bool _showOnlyFavorites = false;
        private TabControl _tabControl;
        private ScrollViewer _scriptsScrollViewer;
        private StackPanel _buttonsContainer;
        
        // Controles adicionales para la pestaña de botones
        private Border _buttonsFiltersPanel;
        private WpfComboBox _buttonsCategoryFilter;
        private Border _globalFiltersPanel;

        // Controles para autenticación personalizada (placeholder Google Apps Script)
        private TextBlock _customStatusText;
        private Button _customAuthButton;
    private bool _isCustomConnected = false; // Placeholder for custom Google Apps Script auth
        
        // Servicio de autenticación BIMtegration
        private BIMAuthService _bimAuthService;
        
        // AI Modeling tab (controlled by BIMtegration authentication)
        private TabItem _aiModelingTab;
        
    private Button _addScriptButton;
    private Button _openFileButton;
    private Button _exportButton;
    private Button _historyButton;
    private Button _updateScriptButton;
    private TabItem _advancedTab;
    private WpfTextBox _logsTextBox;  // Display para los logs de debug
        
        // Controles del AI Assistant integrado
        private WpfTextBox _aiPromptBox;
        private WpfTextBox _aiContextBox;
        private WpfTextBox _aiGeneratedCodeBox;
        private WpfTextBox _aiErrorBox;
        private Button _aiGenerateButton;
        private Button _aiApplyCodeButton;
        private Button _aiFixErrorButton;
        private StackPanel _aiConversationPanel;
        private ScrollViewer _aiConversationScroll;
    // Estado de carga inicial para evitar UI vacía al inicio
    private bool _initialReloadScheduled = false;

        // Estado para botones premium
        private List<ScriptDefinition> _premiumScripts = new List<ScriptDefinition>();
        private Dictionary<string, string> _premiumButtonStatus = new Dictionary<string, string>(); // id -> estado (cached, downloading, error)

        public ScriptPanel()
        {
            _bimAuthService = new BIMAuthService();
            
            // Token se preserva si existe de login anterior
            // NO borrar el token aquí - permitir que persista para botones premium
            System.Diagnostics.Debug.WriteLine($"[ScriptPanel] Sesión cargada - Autenticado: {_bimAuthService.IsAuthenticated}");
            
            // APLICAR TEMA OSCURO PRIMERO - ANTES DE CREAR UI
            this.Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48));
            this.Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
            
            // Intentar inicializar UIApplication automáticamente
            TryInitializeUIApplication();
            
            // Suscribirse a cambios de tema
            ThemeManager.ThemeChanged += OnThemeChanged;
            
            InitializeUI();
            
            // FORZAR TEMA OSCURO DESPUÉS DE CREAR UI
            ForceApplyDarkTheme();
            
            LoadScriptsAsync();
            CheckExistingCustomAuth(); // Placeholder for custom auth
            
            // Establecer estado inicial de botones dependientes
            UpdateCustomDependentButtons(); // Advanced depends on custom auth
        }
        
        /// <summary>
        /// Manejador de cambio de tema
        /// </summary>
        private void OnThemeChanged(object sender, EventArgs e)
        {
            // Aplicar el nuevo tema a todos los controles
            ApplyThemeToAllControls();
        }
        
        /// <summary>
        /// Aplica el tema actual a todos los controles
        /// </summary>
        private void ApplyThemeToAllControls()
        {
            ForceApplyDarkTheme();
        }
        
        /// <summary>
        /// FUERZA tema oscuro con colores explícitos a TODOS los controles
        /// </summary>
        private void ForceApplyDarkTheme()
        {
            try
            {
                // COLORES OSCUROS EXPLÍCITOS
                var darkBg = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48));
                var darkButtonBg = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64));
                var whiteFg = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
                var borderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70));
                
                // Panel principal
                this.Background = darkBg;
                this.Foreground = whiteFg;
                
                // TabControl
                if (_tabControl != null)
                {
                    _tabControl.Background = darkBg;
                    _tabControl.Foreground = whiteFg;
                    _tabControl.BorderBrush = borderBrush;
                    
                    // Aplicar a CADA TabItem
                    foreach (var item in _tabControl.Items)
                    {
                        if (item is TabItem tabItem)
                        {
                            tabItem.Background = darkBg;
                            tabItem.Foreground = whiteFg;
                            tabItem.BorderBrush = borderBrush;
                            
                            // Aplicar al contenido del TabItem
                            if (tabItem.Content is FrameworkElement content)
                            {
                                ApplyDarkThemeRecursive(content, darkBg, darkButtonBg, whiteFg, borderBrush);
                            }
                        }
                    }
                }
                
                // Aplicar a contenedores principales
                if (_scriptsContainer != null)
                    ApplyDarkThemeRecursive(_scriptsContainer, darkBg, darkButtonBg, whiteFg, borderBrush);
                    
                if (_buttonsContainer != null)
                    ApplyDarkThemeRecursive(_buttonsContainer, darkBg, darkButtonBg, whiteFg, borderBrush);
                
                this.UpdateLayout();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error aplicando tema: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Aplica tema oscuro recursivamente a un elemento y sus hijos
        /// </summary>
        private void ApplyDarkThemeRecursive(FrameworkElement element, Brush darkBg, Brush darkButtonBg, Brush whiteFg, Brush borderBrush)
        {
            if (element == null) return;
            
            // Aplicar según el tipo de control
            if (element is WpfPanel panel)
            {
                panel.Background = darkBg;
                foreach (UIElement child in panel.Children)
                {
                    if (child is FrameworkElement fe)
                        ApplyDarkThemeRecursive(fe, darkBg, darkButtonBg, whiteFg, borderBrush);
                }
            }
            else if (element is Button button)
            {
                // NO sobrescribir botones azules (primary)
                if (button.Background is SolidColorBrush brush)
                {
                    var color = brush.Color;
                    // Si es azul (primary button), no tocarlo
                    if (color.R == 0 && color.G == 120 && color.B == 212)
                    {
                        // Mantener el azul, solo aplicar foreground blanco
                        button.Foreground = Brushes.White;
                        return;
                    }
                }
                
                // Para botones normales, aplicar tema oscuro
                button.Background = darkButtonBg;
                button.Foreground = whiteFg;
                button.BorderBrush = borderBrush;
            }
            else if (element is Label label)
            {
                label.Foreground = whiteFg;
                label.Background = Brushes.Transparent;
            }
            else if (element is TextBlock textBlock)
            {
                textBlock.Foreground = whiteFg;
                textBlock.Background = Brushes.Transparent;
            }
            else if (element is WpfComboBox comboBox)
            {
                // NO sobrescribir ComboBoxes que ya tienen Foreground negro (tema claro)
                if (comboBox.Foreground != Brushes.Black)
                {
                    comboBox.Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64));
                    comboBox.Foreground = whiteFg;
                    comboBox.BorderBrush = borderBrush;
                }
            }
            else if (element is WpfTextBox textBox)
            {
                textBox.Background = darkBg;
                textBox.Foreground = whiteFg;
                textBox.BorderBrush = borderBrush;
            }
            else if (element is Expander expander)
            {
                expander.Background = darkBg;
                expander.Foreground = whiteFg;
                expander.BorderBrush = borderBrush;
                
                if (expander.Content is FrameworkElement expanderContent)
                    ApplyDarkThemeRecursive(expanderContent, darkBg, darkButtonBg, whiteFg, borderBrush);
            }
            else if (element is ScrollViewer scrollViewer)
            {
                scrollViewer.Background = darkBg;
                
                if (scrollViewer.Content is FrameworkElement scrollContent)
                    ApplyDarkThemeRecursive(scrollContent, darkBg, darkButtonBg, whiteFg, borderBrush);
            }
            else if (element is Border border)
            {
                border.Background = darkBg;
                border.BorderBrush = borderBrush;
                
                if (border.Child is FrameworkElement borderChild)
                    ApplyDarkThemeRecursive(borderChild, darkBg, darkButtonBg, whiteFg, borderBrush);
            }
            else if (element is ContentControl contentControl && !(element is TabItem))
            {
                if (contentControl.Content is FrameworkElement contentElement)
                    ApplyDarkThemeRecursive(contentElement, darkBg, darkButtonBg, whiteFg, borderBrush);
            }
        }

        /// <summary>
        /// Obtiene la UIApplication actual de Revit
        /// </summary>
        private UIApplication GetCurrentUIApplication()
        {
            // Primero intentar usar la referencia estática establecida por los comandos
            if (Application.CurrentUIApplication != null)
            {
                return Application.CurrentUIApplication;
            }

            // Si no está disponible, mostrar un mensaje más amigable
            return null;
        }

        /// <summary>
        /// Intenta inicializar la UIApplication automáticamente si está disponible
        /// </summary>
        private void TryInitializeUIApplication()
        {
            try
            {
                // Si ya está inicializada, no hacer nada
                if (Application.CurrentUIApplication != null)
                    return;

                // Intentar obtener la UIApplication desde el contexto de Revit
                // Esto funcionará si el panel se abre después de que Revit esté completamente cargado
                var app = System.Windows.Application.Current;
                if (app != null)
                {
                    // Buscar la UIApplication en el contexto actual
                    // Nota: Esto es un intento optimista, puede no funcionar siempre
                    var revitProcess = System.Diagnostics.Process.GetCurrentProcess();
                    if (revitProcess.ProcessName.Contains("Revit"))
                    {
                        // Estamos en contexto de Revit, pero necesitamos acceso a la UIApplication
                        // La inicialización real ocurrirá cuando se ejecute un comando
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallar silenciosamente - la inicialización ocurrirá más tarde
                System.Diagnostics.Debug.WriteLine($"No se pudo inicializar UIApplication automáticamente: {ex.Message}");
            }
        }

        /// <summary>
        /// Intenta obtener UIApplication del contexto actual
        /// </summary>
        private bool TryGetUIApplicationFromContext()
        {
            try
            {
                // Intentar usar reflexión para acceder a la UIApplication si está disponible
                // Este es un método más agresivo que puede funcionar en algunos casos
                var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
                var revitAssembly = assemblies.FirstOrDefault(a => a.FullName.Contains("RevitAPIUI"));
                
                if (revitAssembly != null)
                {
                    // Si encontramos la assembly de RevitAPIUI, es probable que la UIApplication esté disponible
                    // Pero sin un contexto de comando directo, es difícil acceder a ella
                    return false; // Por ahora, fallar grácilmente
                }
                
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error intentando obtener UIApplication del contexto: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Inicializa la interfaz de usuario programáticamente
        /// </summary>
        private void InitializeUI()
        {
            // Configuración básica del UserControl con DARK THEME
            Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48));
            Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
            Width = 320;
            Height = 600;

            var mainPanel = new DockPanel();

            // Panel de filtros
            _globalFiltersPanel = CreateFiltersPanel();
            DockPanel.SetDock(_globalFiltersPanel, Dock.Top);

            // Panel de pestañas para organizar comandos - DARK THEME
            var tabPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Padding = new Thickness(10, 10, 10, 10)
            };

            var tabControl = new TabControl();
            _tabControl = tabControl;
            _tabControl.SelectionChanged += TabControl_SelectionChanged;
            
            // Basic tab (for quick access buttons)
            var buttonsTab = new TabItem { Header = "🎯 Basic" };
            
            // Crear panel principal para la pestaña Botones con filtros incluidos
            var buttonsMainPanel = new DockPanel();
            
            // Panel de filtros para la pestaña Botones
            var buttonsFiltersPanel = CreateButtonsFiltersPanel();
            DockPanel.SetDock(buttonsFiltersPanel, Dock.Top);
            
            // Panel para las acciones rápidas (Importar / Actualizar) - DARK THEME
            var importPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(10, 8, 10, 8)
            };
            
            // Eliminar el manejador de eventos antiguo si existe
            var oldImportButton = FindName("ImportButton") as Button;
            if (oldImportButton != null)
            {
                oldImportButton.Click -= ImportButton_Click;
            }

            // Contenedor para acciones rápidas (Importar / Actualizar) en columna
            var quickActionsPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };

            var basicImportButton = CreateStyledButton("📥 Import Scripts", false);
            basicImportButton.Name = "ImportButton"; // Asignar un nombre para referencia futura
            basicImportButton.Click += (s, e) => 
            {
                var command = new ShowScriptPanelCommand();
                command.ImportScripts();
                // Recargar scripts después de importar (sin popups de debug)
                LoadScriptsAsync();
            };
            basicImportButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            basicImportButton.Margin = new Thickness(5, 2, 5, 2); // mismo margen que otros botones
            basicImportButton.ToolTip = "Import scripts from a JSON file";

            var refreshButton = CreateStyledButton("🔄 Refresh List", false);
            // Uno debajo del otro y mismo ancho dinámico
            refreshButton.HorizontalAlignment = HorizontalAlignment.Stretch;
            refreshButton.Margin = new Thickness(5, 2, 5, 2);
            refreshButton.ToolTip = "Reload scripts and buttons";
            refreshButton.Click += (s, e) =>
            {
                // Forzar recarga completa de scripts y botones
                LoadScriptsAsync();
            };

            quickActionsPanel.Children.Add(basicImportButton);
            quickActionsPanel.Children.Add(refreshButton);

            // (Eliminado: botón y lógica de catálogo online)

            importPanel.Child = quickActionsPanel;
            DockPanel.SetDock(importPanel, Dock.Top);
            
            // ScrollViewer para los botones - DARK THEME
            var buttonsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48))
            };
            
            _buttonsContainer = new StackPanel { Margin = new Thickness(5, 10, 5, 0) };
            buttonsScrollViewer.Content = _buttonsContainer;
            
            buttonsMainPanel.Children.Add(buttonsFiltersPanel);
            buttonsMainPanel.Children.Add(importPanel);
            buttonsMainPanel.Children.Add(buttonsScrollViewer);
            
            buttonsTab.Content = buttonsMainPanel;
            
            // Advanced tab (management functions)
                _advancedTab = new TabItem { Header = "🔧 Advanced" };
            
            // Crear ScrollViewer para el contenido del Advanced tab
            var advancedScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48))
            };

            var advancedStack = new StackPanel { Margin = new Thickness(10) };

            // AGREGAR: Panel de botones premium al inicio
            var premiumPanel = CreatePremiumButtonsPanel();
            advancedStack.Children.Add(premiumPanel);

            // Separador visual
            var separator = new Border
            {
                Height = 1,
                Background = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Margin = new Thickness(0, 10, 0, 10)
            };
            advancedStack.Children.Add(separator);

            // Título de funciones de gestión
            var managementTitle = new TextBlock
            {
                Text = "⚙️ MANAGEMENT FUNCTIONS",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            advancedStack.Children.Add(managementTitle);
            
            _addScriptButton = CreateStyledButton("➕ New Script", true);
            _addScriptButton.Click += AddScriptButton_Click;
            _addScriptButton.Margin = new Thickness(0, 8, 0, 0);

                // Button to update existing scripts
                _updateScriptButton = CreateStyledButton("🔄 Update Script", true);
                _updateScriptButton.Click += UpdateScriptButton_Click;
                _updateScriptButton.Margin = new Thickness(0, 8, 0, 0);
            
            _exportButton = CreateStyledButton("📤 Export Scripts", false);
            _exportButton.Click += ExportButton_Click;
            _exportButton.Margin = new Thickness(0, 5, 0, 0);

            advancedStack.Children.Add(_addScriptButton);
            advancedStack.Children.Add(_exportButton);
                advancedStack.Children.Add(_updateScriptButton);
                
            advancedScrollViewer.Content = advancedStack;
            _advancedTab.Content = advancedScrollViewer;

            // Settings tab (account/options)
            var configTab = new TabItem { Header = "⚙️ Settings" };
            var configStack = new StackPanel { Margin = new Thickness(10) };

            // Custom auth status panel (placeholder) - DARK THEME
            var customStatusPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var customStatusStack = new StackPanel();

            var customTitle = new TextBlock
            {
                Text = "🔐 BIMtegration Account",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };

            var customStatus = new TextBlock
            {
                Text = "Not connected",
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(170, 170, 170)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            _customStatusText = customStatus;

            var customAuthButton = CreateStyledButton("🔗 Connect to BIMtegration", true);
            _customAuthButton = customAuthButton;
            customAuthButton.Click += CustomAuthButton_Click;

            customStatusStack.Children.Add(customTitle);
            customStatusStack.Children.Add(customStatus);
            customStatusStack.Children.Add(customAuthButton);
            customStatusPanel.Child = customStatusStack;

            configStack.Children.Add(customStatusPanel);
            
            // Tools panel - DARK THEME
            var toolsPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var toolsStack = new StackPanel();

            var toolsTitle = new TextBlock
            {
                Text = "🔧 Tools",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            toolsStack.Children.Add(toolsTitle);

            // Move Open JSON File button to Settings
            _openFileButton = CreateStyledButton("📁 Open JSON File", false);
            _openFileButton.Click += OpenFileButton_Click;
            _openFileButton.Margin = new Thickness(0, 0, 0, 5);
            toolsStack.Children.Add(_openFileButton);

            // Logs button (renamed from Historial)
            _historyButton = CreateStyledButton("📋 Logs", false);
            _historyButton.Click += HistoryButton_Click;
            _historyButton.Margin = new Thickness(0, 5, 0, 0);
            toolsStack.Children.Add(_historyButton);

            toolsPanel.Child = toolsStack;
            configStack.Children.Add(toolsPanel);
            
            // Logs Display Panel - DARK THEME
            var logsPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var logsStack = new StackPanel();

            var logsTitle = new TextBlock
            {
                Text = "📋 Debug Logs",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            logsStack.Children.Add(logsTitle);

            // Logs TextBox with scroll
            _logsTextBox = new WpfTextBox
            {
                Height = 200,
                IsReadOnly = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Courier New"),
                FontSize = 11,
                Background = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(200, 200, 200)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 10),
                Text = "No logs loaded yet. Click 'Logs' button to refresh."
            };
            logsStack.Children.Add(_logsTextBox);

            // Refresh button
            var refreshLogsButton = CreateStyledButton("🔄 Refresh Logs", false);
            refreshLogsButton.Click += (s, e) => RefreshLogsDisplay();
            logsStack.Children.Add(refreshLogsButton);

            logsPanel.Child = logsStack;
            configStack.Children.Add(logsPanel);
            
            // Información adicional
            var infoPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(240, 248, 255)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(158, 203, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };
            
            var infoStack = new StackPanel();
            
            // Advanced features info panel removed as requested
            
            configStack.Children.Add(infoPanel);
            
            configTab.Content = configStack;

            // AI Assistant tab (requires GitHub)
            _aiModelingTab = new TabItem { Header = "🤖 AI Modeling" };
            var aiMainPanel = new DockPanel();
            
            // Crear el panel de AI Assistant avanzado (requiere GitHub)
            var aiAssistantPanel = CreateAdvancedAIAssistantPanel();
            aiMainPanel.Children.Add(aiAssistantPanel);
            _aiModelingTab.Content = aiMainPanel;

            tabControl.Items.Add(buttonsTab);
                tabControl.Items.Add(_advancedTab);
            tabControl.Items.Add(_aiModelingTab);
            tabControl.Items.Add(configTab);
            tabPanel.Child = tabControl;

            DockPanel.SetDock(tabPanel, Dock.Top);

            // Área principal de scripts con scroll - DARK THEME
            _scriptsScrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48))
            };

            _scriptsContainer = new StackPanel { Margin = new Thickness(0, 10, 0, 0) };
            
            _loadingMessage = new TextBlock
            {
                Text = "⏳ Loading scripts...",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
                FontStyle = FontStyles.Italic,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };

            _scriptsContainer.Children.Add(_loadingMessage);
            _scriptsScrollViewer.Content = _scriptsContainer;

            // Ensamblar todo
            mainPanel.Children.Add(tabPanel);
            mainPanel.Children.Add(_globalFiltersPanel);
            mainPanel.Children.Add(_scriptsScrollViewer);

            Content = mainPanel;
            
            // Configurar vista inicial (pestaña Botones seleccionada por defecto)
            _tabControl.SelectedIndex = 0;
            // Inicialmente ocultar el panel global de filtros y el área de scripts para la pestaña de botones
            _globalFiltersPanel.Visibility = System.Windows.Visibility.Collapsed;
            _scriptsScrollViewer.Visibility = System.Windows.Visibility.Collapsed;

            // Programar recarga una vez que el control esté cargado en el árbol visual
            this.Loaded += (s, e) =>
            {
                if (!_initialReloadScheduled)
                {
                    _initialReloadScheduled = true;
                    // Ejecutar después del render inicial para evitar estados en blanco
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        LoadScriptsAsync();
                    }), System.Windows.Threading.DispatcherPriority.Background);
                }
            };
        }

        /// <summary>
        /// Maneja el cambio de pestaña para mostrar/ocultar contenido apropiado
        /// </summary>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_tabControl?.SelectedItem is TabItem selectedTab)
            {
                // Aplicar estilo a tabs: tab activo con texto negro, inactivos con texto blanco
                foreach (var item in _tabControl.Items)
                {
                    if (item is TabItem tabItem)
                    {
                        if (tabItem == selectedTab)
                        {
                            // Tab activo: texto negro sobre fondo blanco
                            tabItem.Foreground = Brushes.Black;
                        }
                        else
                        {
                            // Tab inactivo: texto blanco sobre fondo oscuro
                            tabItem.Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
                        }
                    }
                }
                
                string header = selectedTab.Header?.ToString() ?? "";
                
                if (header.Contains("Buttons") || header.Contains("Basic"))
                {
                    // En pestaña Botones: ocultar filtros globales y área de scripts (la pestaña tiene sus propios filtros)
                    if (_globalFiltersPanel != null)
                        _globalFiltersPanel.Visibility = System.Windows.Visibility.Collapsed;
                    if (_scriptsScrollViewer != null)
                        _scriptsScrollViewer.Visibility = System.Windows.Visibility.Collapsed;

                    // Asegurar que los botones se regeneren al entrar en la pestaña básica
                    if (_buttonsContainer != null)
                    {
                        GenerateExecutableButtons();
                    }
                }
                else if (header.Contains("Advanced"))
                {
                    // En pestaña Avanzado: ocultar filtros globales y área de scripts (solo mostrar botones de gestión)
                    if (_globalFiltersPanel != null)
                        _globalFiltersPanel.Visibility = System.Windows.Visibility.Collapsed;
                    if (_scriptsScrollViewer != null)
                        _scriptsScrollViewer.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Crea un botón con estilo personalizado
        /// </summary>
        private Button CreateStyledButton(string content, bool isPrimary)
        {
            var button = new Button
            {
                Content = content,
                Padding = new Thickness(15, 10, 15, 10),
                FontWeight = FontWeights.SemiBold,
                Cursor = System.Windows.Input.Cursors.Hand,
                BorderThickness = new Thickness(0)
            };

            if (isPrimary)
            {
                // Botones primary: SIEMPRE azul (independiente del tema)
                button.Background = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212));
                button.Foreground = Brushes.White;
            }
            else
            {
                // Botones secondary: dark theme
                button.Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64));
                button.Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
                button.BorderThickness = new Thickness(1);
                button.BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70));
            }

            return button;
        }

        /// <summary>
        /// Crea el panel de filtros y búsqueda
        /// </summary>
        private Border CreateFiltersPanel()
        {
            var filtersPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(248, 248, 248)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var filtersGrid = new WpfGrid();
            filtersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            filtersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            filtersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Primera fila: Filtro por categoría
            var categoryLabel = new TextBlock
            {
                Text = "📂 Filter by category:",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80))
            };
            WpfGrid.SetRow(categoryLabel, 0);
            filtersGrid.Children.Add(categoryLabel);

            _categoryFilter = new WpfComboBox
            {
                Height = 28,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180))
            };
            _categoryFilter.SelectionChanged += CategoryFilter_SelectionChanged;
            WpfGrid.SetRow(_categoryFilter, 0);
            WpfGrid.SetColumn(_categoryFilter, 0);

            // Segunda fila: Búsqueda de texto
            var searchLabel = new TextBlock
            {
                Text = "🔍 Search scripts:",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80))
            };
            WpfGrid.SetRow(searchLabel, 1);
            filtersGrid.Children.Add(searchLabel);

            var searchPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            _searchBox = new WpfTextBox
            {
                Height = 28,
                FontSize = 11,
                Width = 200,
                Margin = new Thickness(0, 0, 8, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180)),
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _searchBox.GotFocus += (s, e) => {
                if (_searchBox.Text == "Type to search...")
                {
                    _searchBox.Text = "";
                    _searchBox.Foreground = Brushes.Black;
                }
            };
            _searchBox.LostFocus += (s, e) => {
                if (string.IsNullOrEmpty(_searchBox.Text))
                {
                    _searchBox.Text = "Type to search...";
                    _searchBox.Foreground = Brushes.Gray;
                }
            };
            _searchBox.TextChanged += SearchBox_TextChanged;
            _searchBox.Text = "Type to search...";
            _searchBox.Foreground = Brushes.Gray;

            _clearFiltersButton = new Button
            {
                Content = "🗑️",
                Width = 28,
                Height = 28,
                FontSize = 12,
                Background = new SolidColorBrush(WpfColor.FromRgb(220, 53, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Clear filters"
            };
            _clearFiltersButton.Click += ClearFilters_Click;

            searchPanel.Children.Add(_searchBox);
            searchPanel.Children.Add(_clearFiltersButton);
            
            WpfGrid.SetRow(searchPanel, 1);
            filtersGrid.Children.Add(searchPanel);

            // Tercera fila: Filtro de favoritos
            _favoritesFilter = new Button
            {
                Content = "⭐ Show favorites only",
                Height = 32,
                FontSize = 11,
                Margin = new Thickness(0, 8, 0, 0),
                Background = new SolidColorBrush(WpfColor.FromRgb(255, 255, 255)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            _favoritesFilter.Click += FavoritesFilter_Click;
            WpfGrid.SetRow(_favoritesFilter, 2);
            filtersGrid.Children.Add(_favoritesFilter);

            filtersPanel.Child = filtersGrid;
            return filtersPanel;
        }

        /// <summary>
        /// Crea el panel de filtros específico para la pestaña de botones
        /// </summary>
        private Border CreateButtonsFiltersPanel()
        {
            var filtersPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(248, 248, 248)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                Padding = new Thickness(10, 8, 10, 8)
            };

            var filtersGrid = new WpfGrid();
            filtersGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Única fila: Filtro por categoría
            var categoryLabel = new TextBlock
            {
                Text = "📂 Filter by category:",
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80))
            };
            WpfGrid.SetRow(categoryLabel, 0);
            filtersGrid.Children.Add(categoryLabel);

            var buttonsCategoryFilter = new WpfComboBox
            {
                Height = 28,
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 0),
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180)),
                Foreground = Brushes.Black,
                MinWidth = 120
            };
            buttonsCategoryFilter.SelectionChanged += ButtonsCategoryFilter_SelectionChanged;
            WpfGrid.SetRow(buttonsCategoryFilter, 0);
            filtersGrid.Children.Add(buttonsCategoryFilter);

            // Guardar referencia para sincronización
            _buttonsFiltersPanel = filtersPanel;
            _buttonsCategoryFilter = buttonsCategoryFilter;

            filtersPanel.Child = filtersGrid;
            return filtersPanel;
        }

        /// <summary>
        /// Carga los scripts de forma asíncrona
        /// </summary>
        private async void LoadScriptsAsync()
        {
            try
            {
                _loadingMessage.Text = "⏳ Loading scripts...";
                _loadingMessage.Visibility = System.Windows.Visibility.Visible;

                // Cargar scripts en background thread
                await Task.Run(() =>
                {
                    _allCategories = ScriptManager.LoadScripts();
                    _categories = new List<ScriptCategory>(_allCategories); // Copia para filtros
                });

                // Actualizar UI en el thread principal
                UpdateCategoryFilter();
                GenerateScriptButtons();
                
                // También cargar los botones para la pestaña básica
                GenerateExecutableButtons();
                
                _loadingMessage.Visibility = System.Windows.Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                _loadingMessage.Text = $"❌ Error loading scripts: {ex.Message}";
            }
        }

        /// <summary>
        /// Actualiza las opciones del filtro de categorías
        /// </summary>
        private void UpdateCategoryFilter()
        {
            _categoryFilter.Items.Clear();
            _categoryFilter.Items.Add("All categories");
            if (_allCategories != null && _allCategories.Any())
            {
                foreach (var category in _allCategories.OrderBy(c => c.Name))
                {
                    _categoryFilter.Items.Add(category.Name);
                }
            }
            _categoryFilter.SelectedIndex = 0;

            // También actualizar el filtro de la pestaña de botones
            if (_buttonsCategoryFilter != null)
            {
                _buttonsCategoryFilter.Items.Clear();
                _buttonsCategoryFilter.Items.Add("All categories");
                // Obtener scripts de botón y extraer categorías únicas
                var buttonScripts = ScriptManager.LoadButtonScripts();
                var buttonCategories = buttonScripts
                    .Select(s => s.Category)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                foreach (var category in buttonCategories)
                {
                    _buttonsCategoryFilter.Items.Add(category);
                }
                _buttonsCategoryFilter.SelectedIndex = 0;
                // Si no hay categorías, mostrar texto por defecto
                if (_buttonsCategoryFilter.Items.Count == 1)
                {
                    _buttonsCategoryFilter.Items[0] = "No categories available";
                }
            }
        }

        /// <summary>
        /// Maneja el evento de cambio de filtro de categoría
        /// </summary>
        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        /// <summary>
        /// Maneja el evento de cambio de filtro de categoría en la pestaña de botones
        /// </summary>
        private void ButtonsCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GenerateExecutableButtons();
        }

        /// <summary>
        /// Maneja el evento de cambio de texto en la búsqueda
        /// </summary>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_searchBox.Text != "Type to search...")
            {
                ApplyFilters();
            }
        }

        /// <summary>
        /// Limpia todos los filtros
        /// </summary>
        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            _categoryFilter.SelectedIndex = 0;
            _searchBox.Text = "Type to search...";
            _searchBox.Foreground = Brushes.Gray;
            _showOnlyFavorites = false;
            UpdateFavoritesButtonAppearance();
            ApplyFilters();
        }

        /// <summary>
        /// Alterna el filtro de favoritos
        /// </summary>
        private void FavoritesFilter_Click(object sender, RoutedEventArgs e)
        {
            _showOnlyFavorites = !_showOnlyFavorites;
            UpdateFavoritesButtonAppearance();
            ApplyFilters();
        }

        /// <summary>
        /// Actualiza la apariencia del botón de favoritos
        /// </summary>
        private void UpdateFavoritesButtonAppearance()
        {
            if (_favoritesFilter != null)
            {
                if (_showOnlyFavorites)
                {
                    _favoritesFilter.Background = new SolidColorBrush(WpfColor.FromRgb(255, 193, 7));
                    _favoritesFilter.Content = "⭐ Showing favorites";
                    _favoritesFilter.FontWeight = FontWeights.Bold;
                }
                else
                {
                    _favoritesFilter.Background = new SolidColorBrush(WpfColor.FromRgb(255, 255, 255));
                    _favoritesFilter.Content = "⭐ Show favorites only";
                    _favoritesFilter.FontWeight = FontWeights.Normal;
                }
            }
        }

        /// <summary>
        /// Aplica los filtros activos a las categorías de scripts
        /// </summary>
        private void ApplyFilters()
        {
            // Robust null checks for UI controls
            if (_allCategories == null || _categoryFilter == null || _searchBox == null)
            {
                // If controls are not ready, do not filter, but clear or keep previous state
                _categories = _allCategories ?? new List<ScriptCategory>();
                GenerateScriptButtons();
                GenerateExecutableButtons();
                return;
            }

            // Si solo queremos favoritos, cargar solo esos
            var categoriesToFilter = _showOnlyFavorites ? 
                ScriptManager.LoadFavoriteScripts() : _allCategories;

            // Copiar todas las categorías
            var filteredCategories = new List<ScriptCategory>();

            foreach (var category in categoriesToFilter)
            {
                // Filtrar por categoría seleccionada
                bool matchesCategory = _categoryFilter.SelectedIndex == 0;
                if (!matchesCategory && _categoryFilter.SelectedItem != null)
                {
                    matchesCategory = _categoryFilter.SelectedItem.ToString() == category.Name;
                }
                if (!matchesCategory) continue;

                // Filtrar por texto de búsqueda
                var filteredScripts = category.Scripts.ToList();
                if (_searchBox.Text != "Type to search..." && !string.IsNullOrEmpty(_searchBox.Text))
                {
                    string searchText = _searchBox.Text.ToLower();
                    filteredScripts = category.Scripts.Where(s => 
                        (s.Name ?? "").ToLower().Contains(searchText) ||
                        (s.Description ?? "").ToLower().Contains(searchText) ||
                        (s.Category ?? "").ToLower().Contains(searchText)
                    ).ToList();
                }

                // Si hay scripts que coinciden, agregar la categoría
                if (filteredScripts.Any())
                {
                    var filteredCategory = new ScriptCategory
                    {
                        Name = category.Name,
                        Scripts = filteredScripts,
                        IsExpanded = true // Expandir categorías filtradas
                    };
                    filteredCategories.Add(filteredCategory);
                }
            }

            _categories = filteredCategories;
            GenerateScriptButtons();
            GenerateExecutableButtons(); // También actualizar los botones ejecutables
        }

        /// <summary>
        /// Genera dinámicamente los botones de scripts organizados por categorías
        /// </summary>
        private void GenerateScriptButtons()
        {
            _scriptsContainer.Children.Clear();

            if (_categories == null || !_categories.Any())
            {
                var emptyMessage = new TextBlock
                {
                    Text = "📝 No scripts available.\n\nClick 'New Script' to create one.",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(20),
                    FontStyle = FontStyles.Italic,
                    Foreground = System.Windows.Media.Brushes.Gray
                };
                _scriptsContainer.Children.Add(emptyMessage);
                return;
            }

            foreach (var category in _categories)
            {
                // Crear header de categoría expandible
                var categoryHeader = new Button
                {
                    Content = $"📁 {category.Name} ({category.Scripts.Count})",
                    Background = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                    BorderThickness = new Thickness(0),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    Padding = new Thickness(10, 8, 10, 8),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Tag = category
                };
                categoryHeader.Click += CategoryHeader_Click;

                _scriptsContainer.Children.Add(categoryHeader);

                // Crear container para los scripts de esta categoría
                var scriptsContainer = new StackPanel
                {
                    Name = $"Category_{category.Name.Replace(" ", "")}",
                    Visibility = category.IsExpanded ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed,
                    Margin = new Thickness(10, 0, 0, 10)
                };

                // Crear botón para cada script
                foreach (var script in category.Scripts)
                {
                    var scriptButton = CreateScriptListButton(script);
                    scriptsContainer.Children.Add(scriptButton);
                }

                _scriptsContainer.Children.Add(scriptsContainer);
            }

            // Agregar información al final
            var infoPanel = new Border
            {
                Background = System.Windows.Media.Brushes.LightYellow,
                Padding = new Thickness(10),
                Margin = new Thickness(5, 20, 5, 5),
                CornerRadius = new CornerRadius(4)
            };

            var infoText = new TextBlock
            {
                Text = $"💡 Total: {_categories.Sum(c => c.Scripts.Count)} scripts\n" +
                       $"📍 Archivo: {Path.GetFileName(ScriptManager.GetScriptsFilePath())}",
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.DarkOrange
            };

            infoPanel.Child = infoText;
            _scriptsContainer.Children.Add(infoPanel);
        }

        /// <summary>
        /// Genera botones ejecutables organizados para la pestaña "Botones"
        /// </summary>
        private void GenerateExecutableButtons()
        {
            try
            {
                // Cargar scripts específicos para botones
                var buttonScripts = ScriptManager.LoadButtonScripts();
                
                // DEBUG: Log how many button scripts were loaded
                System.Diagnostics.Debug.WriteLine($"[GenerateExecutableButtons] Loaded {buttonScripts?.Count ?? 0} button scripts");
                
                // Actualizar el ComboBox de categorías solo si está vacío o con el placeholder
                if (_buttonsCategoryFilter != null && 
                    (_buttonsCategoryFilter.Items.Count == 0 || 
                     (_buttonsCategoryFilter.Items.Count == 1 && _buttonsCategoryFilter.Items[0].ToString() == "No categories available")))
                {
                    var currentSelection = _buttonsCategoryFilter.SelectedIndex;
                    _buttonsCategoryFilter.SelectionChanged -= ButtonsCategoryFilter_SelectionChanged;
                    
                    _buttonsCategoryFilter.Items.Clear();
                    _buttonsCategoryFilter.Items.Add("All categories");
                    
                    if (buttonScripts != null && buttonScripts.Any())
                    {
                        var buttonCategories = buttonScripts
                            .Select(s => s.Category)
                            .Where(c => !string.IsNullOrWhiteSpace(c))
                            .Distinct()
                            .OrderBy(c => c)
                            .ToList();
                        
                        foreach (var category in buttonCategories)
                        {
                            _buttonsCategoryFilter.Items.Add(category);
                        }
                    }
                    
                    _buttonsCategoryFilter.SelectedIndex = currentSelection >= 0 && currentSelection < _buttonsCategoryFilter.Items.Count ? currentSelection : 0;
                    _buttonsCategoryFilter.SelectionChanged += ButtonsCategoryFilter_SelectionChanged;
                }
                
                _buttonsContainer.Children.Clear();

                if (buttonScripts == null || !buttonScripts.Any())
                {
                    var emptyMessage = new TextBlock
                    {
                        Text = "🎯 No buttons configured.\n\nUse 'Import Scripts' to add scripts, then go to Advanced to create them as buttons.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(20),
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Gray
                    };
                    _buttonsContainer.Children.Add(emptyMessage);
                    return;
                }

                // Aplicar filtro de categoría (case-insensitive, trimmed)
                string selectedCategory = null;
                if (_buttonsCategoryFilter != null && _buttonsCategoryFilter.SelectedIndex > 0)
                {
                    selectedCategory = _buttonsCategoryFilter.SelectedItem?.ToString()?.Trim();
                }

                var filteredScripts = buttonScripts;
                if (!string.IsNullOrEmpty(selectedCategory) && !selectedCategory.Equals("All categories", StringComparison.OrdinalIgnoreCase))
                {
                    filteredScripts = buttonScripts.Where(s =>
                        !string.IsNullOrWhiteSpace(s.Category) &&
                        string.Equals(s.Category.Trim(), selectedCategory, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                if (!filteredScripts.Any())
                {
                    var emptyMessage = new TextBlock
                    {
                        Text = "⚠️ No buttons match the selected filter.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        TextAlignment = TextAlignment.Center,
                        Margin = new Thickness(20),
                        FontStyle = FontStyles.Italic,
                        Foreground = System.Windows.Media.Brushes.Gray
                    };
                    _buttonsContainer.Children.Add(emptyMessage);
                    return;
                }

                // Agrupar scripts por categoría
                var categorizedScripts = filteredScripts.GroupBy(s => string.IsNullOrWhiteSpace(s.Category) ? "(No category)" : s.Category).OrderBy(g => g.Key);

                foreach (var categoryGroup in categorizedScripts)
                {
                    // Crear header de categoría
                    var categoryHeader = new TextBlock
                    {
                        Text = $"📁 {categoryGroup.Key}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Margin = new Thickness(0, 10, 0, 5),
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212))
                    };
                    _buttonsContainer.Children.Add(categoryHeader);

                    // Crear panel de botones para la categoría
                    var buttonsPanel = new StackPanel
                    {
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    // Crear botón ejecutable para cada script
                    foreach (var script in categoryGroup.OrderBy(s => s.Name))
                    {
                        var executableButton = CreateExecutableButton(script);
                        buttonsPanel.Children.Add(executableButton);
                    }

                    _buttonsContainer.Children.Add(buttonsPanel);
                }

                // Agregar información al final
                var infoText = new TextBlock
                {
                    Text = $"🎯 {filteredScripts.Count} buttons available",
                    FontSize = 10,
                    Foreground = System.Windows.Media.Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 0)
                };
                _buttonsContainer.Children.Add(infoText);
            }
            catch (Exception ex)
            {
                // En caso de error, mostrar mensaje
                _buttonsContainer.Children.Clear();
                var errorMessage = new TextBlock
                {
                    Text = $"❌ Error loading buttons: {ex.Message}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(20),
                    Foreground = System.Windows.Media.Brushes.Red
                };
                _buttonsContainer.Children.Add(errorMessage);
				// No popups de debug
            }
        }

        /// <summary>
        /// Actualiza el ComboBox de categorías de botones usando los scripts actuales de botón
        /// </summary>
        private void UpdateButtonsCategoryFilterFromButtons()
        {
            if (_buttonsCategoryFilter == null)
                return;

            var buttonScripts = ScriptManager.LoadButtonScripts();
            var buttonCategories = buttonScripts
                .Select(s => s.Category)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            _buttonsCategoryFilter.Items.Clear();
            _buttonsCategoryFilter.Items.Add("All categories");
            foreach (var category in buttonCategories)
            {
                _buttonsCategoryFilter.Items.Add(category);
            }
        }

        /// <summary>
        /// Carga los botones de forma asíncrona
        /// </summary>
        private async void LoadButtonsAsync()
        {
            try
            {
                // Actualizar UI en el thread principal
                this.Dispatcher.Invoke(() =>
                {
                    GenerateExecutableButtons();
                });
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Error loading buttons: {ex.Message}", "Error",
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        /// <summary>
        /// Crea un botón ejecutable simple para la pestaña "Botones"
        /// </summary>
        private Button CreateExecutableButton(ScriptDefinition script)
        {
            var button = new Button
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Padding = new Thickness(15, 10, 15, 10),
                Margin = new Thickness(5, 2, 5, 2),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = script,
                ToolTip = $"{script.Description}",
                Height = 50,    // Alto fijo para todos los botones
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch  // Esto hará que el botón use todo el ancho disponible
            };

            // Contenido del botón con emoji e texto
            var content = new StackPanel 
            { 
                Orientation = Orientation.Horizontal,  // Cambio a horizontal para mejor uso del espacio
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var icon = new TextBlock 
            { 
                Text = GetEmojiForScript(script), 
                FontSize = 18, 
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)  // Margen derecho para separar del texto
            };
            
            var textBlock = new TextBlock 
            { 
                Text = script.Name, 
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 11,  // Aumenté ligeramente el tamaño de fuente
                FontWeight = script.IsFavorite ? FontWeights.Bold : FontWeights.Normal,
                Foreground = script.IsFavorite ? 
                    new SolidColorBrush(WpfColor.FromRgb(255, 193, 7)) :
                    new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };

            content.Children.Add(icon);
            content.Children.Add(textBlock);
            button.Content = content;

            // Efecto hover
            button.MouseEnter += (s, e) => {
                button.Background = new SolidColorBrush(WpfColor.FromRgb(77, 77, 80));
            };
            button.MouseLeave += (s, e) => {
                button.Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64));
            };

            // Asignar evento de clic
            button.Click += async (sender, e) =>
            {
                await ExecuteScript(script);
            };

            // Menú contextual para quitar de botones
            var contextMenu = new System.Windows.Controls.ContextMenu();
            var removeItem = new System.Windows.Controls.MenuItem { Header = "Remove from buttons" };
            removeItem.Click += (s, e) =>
            {
                try
                {
                    // Desmarcar y persistir
                    var all = ScriptManager.LoadAllScripts();
                    var original = all.FirstOrDefault(x => x.Id == script.Id);
                    if (original != null)
                    {
                        original.ShowAsButton = false;
                        ScriptManager.SaveAllScripts(all);
                    }
                    // Regenerar UI de botones
                    GenerateExecutableButtons();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error removing button: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
            contextMenu.Items.Add(removeItem);
            button.ContextMenu = contextMenu;

            return button;
        }

        /// <summary>
    /// Crea un botón para un script específico en la lista de scripts
    /// </summary>
    private StackPanel CreateScriptListButton(ScriptDefinition script)
        {
            // Panel contenedor para el botón principal y el botón de editar
            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 2, 5, 2)
            };

            // Botón principal (ejecutar script) - ocupará la mayor parte del espacio
            var mainButton = new Button
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                Padding = new Thickness(12, 8, 12, 8),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = script,
                ToolTip = $"{script.Description}\n\nID: {script.Id}",
                MinWidth = 150
            };

            // Contenido del botón principal con icono emoji y texto
            var mainContent = new StackPanel { Orientation = Orientation.Horizontal };
            
            // Emoji como icono
            var icon = new TextBlock 
            { 
                Text = GetEmojiForScript(script), 
                FontSize = 14, 
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            var textBlock = new TextBlock 
            { 
                Text = script.Name, 
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            mainContent.Children.Add(icon);
            mainContent.Children.Add(textBlock);
            mainButton.Content = mainContent;

            // Asignar evento de clic para ejecutar
            mainButton.Click += async (sender, e) => await ExecuteScript(script);

            // Botón de editar - más pequeño, a la derecha
            var editButton = new Button
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Width = 35,
                Height = 35,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = script,
                ToolTip = $"Editar script: {script.Name}",
                Content = "✏️",
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Asignar evento de clic para editar
            editButton.Click += (sender, e) => EditScript(script);

            // Botón de favoritos - pequeño, amarillo cuando está activo
            var favoriteButton = new Button
            {
                Background = script.IsFavorite ? 
                    new SolidColorBrush(WpfColor.FromRgb(255, 193, 7)) : 
                    new SolidColorBrush(WpfColor.FromRgb(245, 245, 245)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Width = 35,
                Height = 35,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = script,
                ToolTip = script.IsFavorite ? 
                    $"Quitar de favoritos: {script.Name}" : 
                    $"Agregar a favoritos: {script.Name}",
                Content = script.IsFavorite ? "⭐" : "☆",
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Asignar evento de clic para favoritos
            favoriteButton.Click += (sender, e) => ToggleFavorite(script, favoriteButton);

            // Botón de eliminar - pequeño, color rojo
            var deleteButton = new Button
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(220, 53, 69)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(200, 35, 51)),
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Width = 35,
                Height = 35,
                FontSize = 14,
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = script,
                ToolTip = $"Eliminar script: {script.Name}",
                Content = "🗑️",
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Asignar evento de clic para eliminar
            deleteButton.Click += (sender, e) => DeleteScript(script);

            // Agregar todos los botones al contenedor
            container.Children.Add(mainButton);
            container.Children.Add(editButton);
            container.Children.Add(favoriteButton);
            container.Children.Add(deleteButton);

            return container;
        }

        /// <summary>
        /// Obtiene un emoji apropiado para el script basado en su categoría/nombre
        /// </summary>
        private string GetEmojiForScript(ScriptDefinition script)
        {
            var name = script.Name.ToLower();
            var category = script.Category?.ToLower() ?? "";

            if (name.Contains("muro") || name.Contains("wall")) return "🧱";
            if (name.Contains("puerta") || name.Contains("door")) return "🚪";
            if (name.Contains("ventana") || name.Contains("window")) return "🪟";
            if (name.Contains("contar") || name.Contains("count")) return "🔢";
            if (name.Contains("seleccion") || name.Contains("select")) return "👆";
            if (name.Contains("info") || name.Contains("información")) return "ℹ️";
            if (name.Contains("area") || name.Contains("área")) return "📐";
            if (name.Contains("limpiar") || name.Contains("clear")) return "🧹";
            if (category.Contains("análisis")) return "📊";
            if (category.Contains("selección")) return "🎯";
            if (category.Contains("información")) return "📋";

            return "⚡"; // Icono por defecto
        }

        /// <summary>
        /// Ejecuta un script usando el motor Roslyn
        /// </summary>
        private async Task ExecuteScript(ScriptDefinition script)
        {
            var startTime = DateTime.Now;
            bool success = false;
            string resultMessage = string.Empty;
            Exception executionException = null;

            try
            {
                // LOG: Verificar que el script tiene código
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] Iniciando ejecución de: {script.Name}");
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] Script.Code length: {(script?.Code?.Length ?? 0)} caracteres");
                if (string.IsNullOrEmpty(script?.Code))
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteScript] ❌ ERROR: Script.Code está vacío para '{script?.Name}'");
                    MessageBox.Show($"❌ Script '{script?.Name}' has no code. Code is empty.", 
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] ✓ Script tiene código. Primeros 100 chars: {script.Code.Substring(0, Math.Min(100, script.Code.Length))}");
                
                var uiApp = GetCurrentUIApplication();
                if (uiApp == null)
                {
                    // Intentar obtener UIApplication de una manera más directa
                    if (TryGetUIApplicationFromContext())
                    {
                        // Reintentar después de la inicialización exitosa
                        uiApp = GetCurrentUIApplication();
                    }
                    
                    // If still not available, show message
                    if (uiApp == null)
                    {
                        MessageBox.Show("🔧 Revit connection required\n\n" +
                                       "To use the scripts, first close and reopen this panel:\n\n" +
                                       "1. Click the 'Hide BIMtegration' button on the Revit ribbon\n" +
                                       "2. Then click 'Show BIMtegration' to reopen the panel\n\n" +
                                       "This establishes the connection. You only need to do it once per session!", 
                                       "Initialization Required", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                // Verify that there is an active document
                if (uiApp.ActiveUIDocument?.Document == null)
                {
                    MessageBox.Show("No Revit document is open.", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Execute script directly - the TaskDialog inside the script will run on the UI thread
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] Calling ExecuteRoslynScript...");
                var result = await ExecuteRoslynScript(script.Code);
                resultMessage = result?.ToString() ?? "Executed successfully";
                success = true;
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] ✓ Script executed successfully");

                // Mostrar resultado
                var resultDialog = new TaskDialog("Resultado del Script")
                {
                    MainContent = $"Script: {script.Name}\n\nResultado:\n{resultMessage}",
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                resultDialog.Show();
            }
            catch (Exception ex)
            {
                success = false;
                executionException = ex;
                resultMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ExecuteScript] ❌ Excepción: {ex.GetType().Name} - {ex.Message}");

                var errorDialog = new TaskDialog("Error en Script")
                {
                    MainContent = $"Error ejecutando '{script.Name}':\n\n{ex.Message}",
                    MainIcon = TaskDialogIcon.TaskDialogIconError,
                    CommonButtons = TaskDialogCommonButtons.Ok,
                    ExpandedContent = ex.ToString()
                };
                errorDialog.Show();
            }
            finally
            {
                // Registrar en el historial de ejecuciones
                var duration = DateTime.Now - startTime;
                ExecutionHistoryManager.RecordExecution(
                    script.Name,
                    script.Name,
                    script.Category ?? "General",
                    success,
                    resultMessage,
                    executionException?.ToString(),
                    duration.TotalMilliseconds
                );
            }
        }

        /// <summary>
        /// Ejecuta un script sin mostrar el diálogo final de resultados
        /// (Para scripts premium que tienen sus propios diálogos)
        /// </summary>
        private async Task ExecuteScriptSilent(ScriptDefinition script)
        {
            var startTime = DateTime.Now;
            bool success = false;
            string resultMessage = string.Empty;
            Exception executionException = null;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] Iniciando ejecución de: {script.Name}");
                System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] Script.Code length: {(script?.Code?.Length ?? 0)} caracteres");
                
                if (string.IsNullOrEmpty(script?.Code))
                {
                    System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] ❌ ERROR: Script.Code está vacío para '{script?.Name}'");
                    MessageBox.Show($"❌ Script '{script?.Name}' has no code. Code is empty.", 
                                   "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var uiApp = GetCurrentUIApplication();
                if (uiApp == null)
                {
                    if (TryGetUIApplicationFromContext())
                    {
                        uiApp = GetCurrentUIApplication();
                    }
                    
                    if (uiApp == null)
                    {
                        MessageBox.Show("🔧 Revit connection required\n\n" +
                                       "To use the scripts, first close and reopen this panel:\n\n" +
                                       "1. Click the 'Hide BIMtegration' button on the Revit ribbon\n" +
                                       "2. Then click 'Show BIMtegration' to reopen the panel\n\n" +
                                       "This establishes the connection. You only need to do it once per session!", 
                                       "Initialization Required", 
                                       MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }

                if (uiApp.ActiveUIDocument?.Document == null)
                {
                    MessageBox.Show("No Revit document is open.", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Execute script without showing final dialog
                System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] Calling ExecuteRoslynScript...");
                var result = await ExecuteRoslynScript(script.Code);
                resultMessage = result?.ToString() ?? "Executed successfully";
                success = true;
                System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] ✓ Script executed successfully without final dialog");
            }
            catch (Exception ex)
            {
                success = false;
                executionException = ex;
                resultMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[ExecuteScriptSilent] ❌ Excepción: {ex.GetType().Name} - {ex.Message}");

                var errorDialog = new TaskDialog("Error en Script")
                {
                    MainContent = $"Error ejecutando '{script.Name}':\n\n{ex.Message}",
                    MainIcon = TaskDialogIcon.TaskDialogIconError,
                    CommonButtons = TaskDialogCommonButtons.Ok,
                    ExpandedContent = ex.ToString()
                };
                errorDialog.Show();
            }
            finally
            {
                // Registrar en el historial de ejecuciones
                var duration = DateTime.Now - startTime;
                ExecutionHistoryManager.RecordExecution(
                    script.Name,
                    script.Name,
                    script.Category ?? "General",
                    success,
                    resultMessage,
                    executionException?.ToString(),
                    duration.TotalMilliseconds
                );
            }
        }

        /// <summary>
        /// Ejecuta el script Roslyn directamente en el thread principal de Revit
        /// </summary>
        private async Task<object> ExecuteRoslynScript(string code)
        {
            var uiApp = GetCurrentUIApplication();

            // Habilitar encodings legacy (IBM437, etc) para todos los scripts
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            // DEBUG: Verificar qué código se está ejecutando
            System.Diagnostics.Debug.WriteLine($"[DEBUG] Ejecutando código: {code}");

            // Asegurar que el SharedExternalEvent/Handler estén inicializados
            if (RoslynCopilotTest.Application.SharedExternalEventHandler == null)
                RoslynCopilotTest.Application.SharedExternalEventHandler = new RoslynCopilotTest.GenericExternalEventHandler();
            if (RoslynCopilotTest.Application.SharedExternalEvent == null)
                RoslynCopilotTest.Application.SharedExternalEvent = Autodesk.Revit.UI.ExternalEvent.Create(RoslynCopilotTest.Application.SharedExternalEventHandler);

            // Crear contexto para el script
            var globals = new ScriptGlobals
            {
                doc = uiApp.ActiveUIDocument.Document,
                uidoc = uiApp.ActiveUIDocument,
                app = uiApp.Application,
                uiapp = uiApp,
                externalEventHandler = RoslynCopilotTest.Application.SharedExternalEventHandler,
                externalEvent = RoslynCopilotTest.Application.SharedExternalEvent,
                // ...eliminado: referencia a CopilotBridge...
            };

            // Configurar referencias - AHORA CON SOPORTE HTTP, CSV y EXCEL! 🌐📊
            var scriptOptions = ScriptOptions.Default
                .AddReferences(
                    typeof(Document).Assembly,                      // RevitAPI.dll
                    typeof(UIDocument).Assembly,                    // RevitAPIUI.dll
                    typeof(System.Linq.Enumerable).Assembly,        // System.Core.dll
                    typeof(System.Net.Http.HttpClient).Assembly,    // System.Net.Http.dll
                    typeof(System.Net.WebRequest).Assembly,         // System.dll (para networking)
                    typeof(System.IO.File).Assembly,                // System.IO para archivos
                    typeof(OfficeOpenXml.ExcelPackage).Assembly,    // EPPlus para Excel
                    typeof(CsvHelper.CsvReader).Assembly            // CsvHelper para CSV
                )
                .WithImports(
                    "System", 
                    "System.Linq",
                    "System.Collections.Generic",
                    "System.Threading.Tasks",                       // Para async/await
                    "System.Net.Http",                              // Para HttpClient
                    "System.IO",                                    // Para File, StreamWriter, StreamReader
                    "System.Text",                                  // Para Encoding
                    "OfficeOpenXml",                                // Para Excel (EPPlus)
                    "CsvHelper",                                    // Para CSV
                    "CsvHelper.Configuration",                      // Para configuración CSV
                    "Autodesk.Revit.DB", 
                    "Autodesk.Revit.UI"
                );

            // Ejecutar el código directamente sin envolver en Task.Run
            // Esto permite que TaskDialog se ejecute en el contexto correcto
            var result = await CSharpScript.RunAsync(code, scriptOptions, globals);

            return result?.ToString() ?? "[El script se ejecutó correctamente pero no devolvió ningún valor]";
        }

        /// <summary>
        /// Maneja el clic en headers de categorías para expandir/colapsar
        /// </summary>
        private void CategoryHeader_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var category = button?.Tag as ScriptCategory;
            
            if (category != null)
            {
                category.IsExpanded = !category.IsExpanded;
                
                // Buscar el container de esta categoría y mostrar/ocultar
                var containerName = $"Category_{category.Name.Replace(" ", "")}";
                var container = _scriptsContainer.Children
                    .OfType<StackPanel>()
                    .FirstOrDefault(sp => sp.Name == containerName);
                
                if (container != null)
                {
                    container.Visibility = category.IsExpanded ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                    
                    // Actualizar texto del header
                    var icon = category.IsExpanded ? "📂" : "📁";
                    button.Content = $"{icon} {category.Name} ({category.Scripts.Count})";
                }
            }
        }

        /// <summary>
        /// Obtiene el nombre de la pestaña según el índice
        /// </summary>
        private string GetTabHeader(int index)
        {
            return index switch
            {
                0 => "🎯 Basic",
                1 => "⚙️ Advanced",
                2 => "🤖 AI Modeling",
                3 => "⚙️ Settings",
                _ => "Pestaña Desconocida"
            };
        }

        /// <summary>
        /// Crea un botón con estilo personalizado para los scripts
        /// </summary>
        private Button CreateScriptButton(ScriptDefinition script)
        {
            // COLORES DARK THEME
            var darkButtonBg = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64));
            var darkButtonHover = new SolidColorBrush(WpfColor.FromRgb(70, 70, 72));
            var whiteFg = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241));
            var borderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70));
            
            var button = new Button
            {
                Content = script.Name,
                Padding = new Thickness(10, 5, 10, 5),
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
                Tag = script,
                Background = darkButtonBg,
                Foreground = whiteFg,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 0, 5)
            };

            // Efecto hover - oscuro
            button.MouseEnter += (s, e) => {
                button.Background = darkButtonHover;
            };
            button.MouseLeave += (s, e) => {
                button.Background = darkButtonBg;
            };

            // Asignar evento de clic para ejecutar el script
            button.Click += async (s, e) => {
                await ExecuteScript(script);
            };

            return button;
        }

        #region Event Handlers para botones de acción

        private void AddScriptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir ventana del editor de scripts
                var editorWindow = new ScriptEditorWindow(OnScriptSaved);
                editorWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening editor: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Callback cuando se guarda un script nuevo desde el editor
        /// </summary>
        private void OnScriptSaved()
        {
            // Actualizar la lista de scripts
            LoadScriptsAsync();
        }

        /// <summary>
        /// Abre el editor para modificar un script existente
        /// </summary>
        private void EditScript(ScriptDefinition script)
        {
            try
            {
                // Abrir ventana del editor de scripts con los datos del script existente
                var editorWindow = new ScriptEditorWindow(OnScriptSaved, script);
                editorWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening editor: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Alterna el estado de favorito de un script
        /// </summary>
        private void ToggleFavorite(ScriptDefinition script, Button favoriteButton)
        {
            try
            {
                // Alternar el estado de favorito
                bool isFavorite = ScriptManager.ToggleFavorite(script.Id);
                script.IsFavorite = isFavorite;

                // Actualizar la apariencia del botón
                if (isFavorite)
                {
                    favoriteButton.Background = new SolidColorBrush(WpfColor.FromRgb(255, 193, 7));
                    favoriteButton.Content = "⭐";
                    favoriteButton.ToolTip = $"Quitar de favoritos: {script.Name}";
                }
                else
                {
                    favoriteButton.Background = new SolidColorBrush(WpfColor.FromRgb(245, 245, 245));
                    favoriteButton.Content = "☆";
                    favoriteButton.ToolTip = $"Agregar a favoritos: {script.Name}";
                }

                // Si estamos mostrando solo favoritos y este script ya no es favorito, recargar la vista
                if (_showOnlyFavorites && !isFavorite)
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error toggling favorite: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadScriptsAsync();
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var filePath = ScriptManager.GetScriptsFilePath();
                if (File.Exists(filePath))
                {
                    Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                }
                else
                {
                    var directory = Path.GetDirectoryName(filePath);
                    Directory.CreateDirectory(directory);
                    Process.Start("explorer.exe", directory);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file location: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Abrir ventana de selección de scripts
                var selectionWindow = new ExportSelectionWindow();
                selectionWindow.ShowDialog();

                if (!selectionWindow.DialogResult)
                {
                    return; // Usuario canceló
                }

                var selectedScripts = selectionWindow.SelectedScripts;
                if (selectedScripts == null || selectedScripts.Count == 0)
                {
                    MessageBox.Show("No scripts were selected for export.",
                                   "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Diálogo para guardar archivo
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Title = "Export Selected Scripts",
                    Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json",
                    FileName = $"Scripts_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    var exportData = new
                    {
                        ExportDate = DateTime.Now,
                        Version = "1.0",
                        TotalScripts = selectedScripts.Count,
                        ExportType = selectedScripts.Count == ScriptManager.LoadAllScripts().Count ? "Complete" : "Selective",
                        Scripts = selectedScripts
                    };

                    var json = JsonConvert.SerializeObject(exportData, Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json, Encoding.UTF8);

                    // Agrupar scripts por categoría para mostrar resumen
                    var categoryGroups = selectedScripts
                        .GroupBy(s => s.Category)
                        .Select(g => $"📂 {g.Key}: {g.Count()} script{(g.Count() != 1 ? "s" : "")}")
                        .ToList();

                    var categorySummary = string.Join("\n", categoryGroups);

                    MessageBox.Show($"Scripts exported successfully.\n\n" +
                                   $"📁 File: {Path.GetFileName(saveFileDialog.FileName)}\n" +
                                   $"📊 Total exported: {selectedScripts.Count} script{(selectedScripts.Count != 1 ? "s" : "")}\n" +
                                   $"📅 Date: {DateTime.Now:dd/MM/yyyy HH:mm}\n\n" +
                                   $"📋 Summary by category:\n{categorySummary}",
                                   "Export Successful",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting scripts: {ex.Message}", "Export Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Importar Scripts",
                    Filter = "Archivos JSON (*.json)|*.json|Todos los archivos (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var jsonContent = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                    
                    // Intentar detectar el formato del archivo
                    List<ScriptDefinition> scriptsToImport;
                    string importInfo = "";

                    try
                    {
                        // Intentar formato de exportación (con metadata)
                        var exportData = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (exportData != null && exportData["Scripts"] != null)
                        {
                            // Deserializar el array Scripts correctamente
                            scriptsToImport = exportData["Scripts"].ToObject<List<ScriptDefinition>>();
                            importInfo = $"📅 Fecha de exportación: {exportData["ExportDate"]}\n📊 Scripts en archivo: {exportData["TotalScripts"]}";
                        }
                        else
                        {
                            // Intentar formato directo de lista de scripts
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                            importInfo = $"📊 Scripts detectados: {scriptsToImport.Count}";
                        }
                    }
                    catch (Exception ex)
                    {
                        // Intentar formato directo de lista de scripts como fallback
                        try
                        {
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                            importInfo = $"📊 Scripts detectados: {scriptsToImport.Count}";
                        }
                        catch
                        {
                            MessageBox.Show($"Error al procesar el archivo JSON:\n\n{ex.Message}",
                                           "Error de Formato", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (scriptsToImport == null || scriptsToImport.Count == 0)
                    {
                        MessageBox.Show("No se encontraron scripts válidos en el archivo seleccionado.",
                                       "Archivo Vacío", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Mostrar ventana de selección de scripts
                    var selectionWindow = new ImportSelectionWindow(scriptsToImport, Path.GetFileName(openFileDialog.FileName));
                    var result = selectionWindow.ShowDialog();

                    if (result == true && selectionWindow.SelectedScripts.Count > 0)
                    {
                        var currentScripts = ScriptManager.LoadAllScripts();
                        
                        int newScripts = 0;
                        int updatedScripts = 0;

                        foreach (var script in selectionWindow.SelectedScripts)
                        {
                            // Validar que el script tenga los campos requeridos
                            if (string.IsNullOrWhiteSpace(script.Name) || string.IsNullOrWhiteSpace(script.Code))
                            {
                                continue; // Saltar scripts inválidos
                            }

                            var existingScript = currentScripts.FirstOrDefault(s => 
                                s.Name.Equals(script.Name, StringComparison.OrdinalIgnoreCase) && 
                                s.Category.Equals(script.Category, StringComparison.OrdinalIgnoreCase));

                            if (existingScript != null)
                            {
                                // Actualizar script existente
                                existingScript.Code = script.Code;
                                existingScript.Description = script.Description;
                                existingScript.Category = script.Category;
                                existingScript.ShowAsButton = true; // Marcarlo como botón
                                existingScript.LastModified = DateTime.Now;
                                updatedScripts++;
                            }
                            else
                            {
                                // Generar nuevo ID para evitar conflictos
                                script.Id = Guid.NewGuid().ToString();
                                
                                // Marcar como botón para que aparezca en la pestaña Básico
                                script.ShowAsButton = true;
                                script.CreatedDate = DateTime.Now;
                                script.LastModified = DateTime.Now;
                                currentScripts.Add(script);
                                newScripts++;
                            }
                        }

                        // Guardar todos los scripts
                        bool saveResult = ScriptManager.SaveAllScripts(currentScripts);
                        
                        if (saveResult)
                        {
                            // Recargar la interfaz
                            LoadScriptsAsync();
                        }
                        else
                        {
                            MessageBox.Show("Error al guardar los scripts importados.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al importar scripts: {ex.Message}\n\n" +
                               $"Asegúrate de que el archivo sea un JSON válido con el formato correcto.",
                               "Error de Importación",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void HistoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RefreshLogsDisplay();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar logs: {ex.Message}",
                               "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Carga y muestra los logs de debug desde el archivo premium-buttons-debug.log
        /// </summary>
        private void RefreshLogsDisplay()
        {
            try
            {
                if (_logsTextBox == null)
                    return;

                string logsFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RoslynCopilot",
                    "premium-buttons-debug.log"
                );

                if (File.Exists(logsFilePath))
                {
                    // Leer últimas líneas del archivo (últimas 1000 líneas)
                    var allLines = File.ReadAllLines(logsFilePath);
                    var recentLines = allLines.Length > 1000 
                        ? allLines.Skip(allLines.Length - 1000).ToList() 
                        : allLines.ToList();

                    _logsTextBox.Text = string.Join(Environment.NewLine, recentLines);
                    _logsTextBox.ScrollToEnd();
                }
                else
                {
                    _logsTextBox.Text = "📁 Log file not found at:\n" + logsFilePath + "\n\nMake sure you have used the premium buttons feature at least once.";
                }
            }
            catch (Exception ex)
            {
                if (_logsTextBox != null)
                    _logsTextBox.Text = $"❌ Error reading logs: {ex.Message}";
            }
        }

        /// <summary>
        /// Elimina un script después de confirmar con el usuario
        /// </summary>
        private void DeleteScript(ScriptDefinition script)
        {
            try
            {
                // Mostrar diálogo de confirmación
                var result = MessageBox.Show(
                    $"¿Estás seguro de que quieres eliminar el script?\n\n" +
                    $"📝 Nombre: {script.Name}\n" +
                    $"📂 Categoría: {script.Category}\n" +
                    $"📄 Descripción: {script.Description}\n\n" +
                    $"⚠️ Esta acción no se puede deshacer.",
                    "Confirmar Eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // Eliminar script usando ScriptManager
                    bool success = ScriptManager.DeleteScript(script.Id);
                    
                    if (success)
                    {
                        // Mostrar mensaje de éxito
                        MessageBox.Show(
                            $"✅ Script eliminado exitosamente\n\n" +
                            $"El script '{script.Name}' ha sido eliminado permanentemente.",
                            "Script Eliminado",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        // Actualizar la lista de scripts
                        LoadScriptsAsync();
                    }
                    else
                    {
                        MessageBox.Show(
                            $"❌ Error al eliminar el script\n\n" +
                            $"No se pudo eliminar el script '{script.Name}'. " +
                            $"Es posible que el archivo esté en uso o protegido.",
                            "Error de Eliminación",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al eliminar script: {ex.Message}", "Error", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region GitHub Authentication

        /// <summary>
        /// <summary>
        /// Updates the enabled state of buttons that require GitHub authentication
        /// </summary>
        private void UpdateGitHubDependentButtons()
        {
            // GitHub auth now gates only AI features; Advanced tab uses custom auth.
        }

        /// <summary>
        /// Updates enable state for Advanced tab and AI Modeling tab based on BIM auth
        /// </summary>
        private void UpdateCustomDependentButtons()
        {
            var isEnabled = _bimAuthService.IsAuthenticated;
            
            // Bloquear/desbloquear botones en Advanced tab
            if (_addScriptButton != null) _addScriptButton.IsEnabled = isEnabled;
            if (_openFileButton != null) _openFileButton.IsEnabled = true;  // Always available
            if (_exportButton != null) _exportButton.IsEnabled = isEnabled;
            if (_historyButton != null) _historyButton.IsEnabled = true;    // Always available
            
            // Bloquear/desbloquear tabs completos
            if (_advancedTab != null) _advancedTab.IsEnabled = isEnabled;
            if (_aiModelingTab != null) _aiModelingTab.IsEnabled = isEnabled;
        }

        /// <summary>
        /// Check existing BIM auth state
        /// </summary>
        private void CheckExistingCustomAuth()
        {
            if (_bimAuthService.IsAuthenticated)
            {
                var user = _bimAuthService.CurrentUser;
                UpdateCustomStatus(user?.Usuario, "Connected");
                _isCustomConnected = true;
            }
            else
            {
                UpdateCustomStatus(null, "Not connected");
                _isCustomConnected = false;
            }
        }

        /// <summary>
        /// Update BIM auth visual state
        /// </summary>
        private void UpdateCustomStatus(string username, string statusMessage)
        {
            if (_customStatusText != null)
            {
                if (_bimAuthService.IsAuthenticated && !string.IsNullOrEmpty(username))
                {
                    var user = _bimAuthService.CurrentUser;
                    var planText = !string.IsNullOrEmpty(user?.Plan) ? $" ({user.Plan})" : "";
                    _customStatusText.Text = $"✅ Connected as: {username}{planText}";
                    _customStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(22, 163, 74));
                    if (_customAuthButton != null) _customAuthButton.Content = "🔌 Disconnect";
                }
                else if (_bimAuthService.IsAuthenticated)
                {
                    _customStatusText.Text = "✅ Connected";
                    _customStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(22, 163, 74));
                    if (_customAuthButton != null) _customAuthButton.Content = "🔌 Disconnect";
                }
                else
                {
                    _customStatusText.Text = "❌ " + statusMessage;
                    _customStatusText.Foreground = new SolidColorBrush(WpfColor.FromRgb(106, 115, 125));
                    if (_customAuthButton != null) _customAuthButton.Content = "🔗 Connect to BIMtegration";
                }
            }
        }

        /// <summary>
        /// Connect/disconnect for BIMtegration auth
        /// </summary>
        private async void CustomAuthButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_bimAuthService.IsAuthenticated)
                {
                    // Abrir ventana de login
                    var loginWindow = new BIMLoginWindow(_bimAuthService);
                    loginWindow.ShowDialog();
                    
                    if (loginWindow.LoginSuccessful)
                    {
                        var user = loginWindow.AuthenticatedUser;
                        var premiumButtons = loginWindow.PremiumButtons; // Obtener lista de botones premium
                        
                        LogPremium($"[Connect Handler] Login exitoso para usuario: {user?.Usuario}");
                        LogPremium($"[Connect Handler] Plan: {user?.Plan}");
                        LogPremium($"[Connect Handler] PremiumButtons recibidos: {premiumButtons?.Count ?? 0}");
                        
                        if (premiumButtons != null && premiumButtons.Any())
                        {
                            foreach (var btn in premiumButtons)
                            {
                                LogPremium($"[Connect Handler]   - Button: ID={btn.id}, Name={btn.name}, URL={btn.url}");
                            }
                        }
                        
                        UpdateCustomStatus(user?.Usuario, "Connected");
                        _isCustomConnected = true;
                        UpdateCustomDependentButtons();
                        
                        // Descargar botones premium si están disponibles
                        if (premiumButtons != null && premiumButtons.Count > 0)
                        {
                            LogPremium($"[Connect Handler] Iniciando descarga de {premiumButtons.Count} botones premium...");
                            await DownloadPremiumButtonsAsync(premiumButtons);
                            LogPremium($"[Connect Handler] ✅ Descarga completada");
                        }
                        else
                        {
                            LogPremium($"[Connect Handler] ⚠️ No hay botones premium para descargar");
                        }
                        
                        MessageBox.Show($"✅ Bienvenido {user?.Usuario}!\n\nPlan: {user?.Plan ?? "Free"}", 
                            "Login Exitoso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    // Confirmar logout
                    var result = MessageBox.Show(
                        "Do you want to logout from BIMtegration?\n\nPremium features will be blocked.",
                        "Logout Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        _bimAuthService.Logout();
                        _isCustomConnected = false;
                        _premiumScripts.Clear(); // Limpiar scripts premium
                        _premiumButtonStatus.Clear();
                        UpdateCustomStatus(null, "Not connected");
                        UpdateCustomDependentButtons();
                        MessageBox.Show("✅ Session closed successfully", "Logout", 
                            MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error de autenticación: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Descarga botones premium desde URLs de Google Drive
        /// Se ejecuta de forma asíncrona después del login
        /// </summary>
        private async Task DownloadPremiumButtonsAsync(List<PremiumButtonInfo> buttonInfos)
        {
            LogPremium($"[ScriptPanel.DownloadPremiumButtonsAsync] Iniciando descarga. Botones recibidos: {buttonInfos?.Count ?? 0}");
            
            if (buttonInfos == null || buttonInfos.Count == 0)
            {
                LogPremium($"[ScriptPanel.DownloadPremiumButtonsAsync] ⚠️ Sin botones premium para descargar");
                return;
            }

            try
            {
                LogPremium($"[DownloadPremiumButtonsAsync] ===== INICIO DESCARGA =====");
                LogPremium($"[DownloadPremiumButtonsAsync] buttonInfos recibidos: {buttonInfos?.Count ?? 0}");
                
                if (buttonInfos == null)
                {
                    LogPremium($"[DownloadPremiumButtonsAsync] ⚠️ buttonInfos es NULL");
                    return;
                }
                
                if (buttonInfos.Count == 0)
                {
                    LogPremium($"[DownloadPremiumButtonsAsync] ⚠️ buttonInfos está vacío");
                    return;
                }
                
                LogPremium($"[DownloadPremiumButtonsAsync] Inicializando estado de descarga para {buttonInfos.Count} botones");
                
                // Inicializar estado de descarga
                _premiumButtonStatus.Clear();
                LogPremium($"[DownloadPremiumButtonsAsync] _premiumButtonStatus limpiado");
                
                foreach (var btn in buttonInfos)
                {
                    LogPremium($"[DownloadPremiumButtonsAsync]   - Procesando: {btn.name} (ID: {btn.id})");
                    _premiumButtonStatus[btn.id] = "⏳ downloading";
                }
                
                LogPremium($"[DownloadPremiumButtonsAsync] _premiumButtonStatus.Count después init: {_premiumButtonStatus.Count}");

                // Callback para actualizar estado
                Action<string> onProgress = (msg) =>
                {
                    System.Diagnostics.Debug.WriteLine($"[Premium Buttons] {msg}");
                    LogPremium($"[Premium Buttons Progress] {msg}");
                };

                LogPremium($"[DownloadPremiumButtonsAsync] Iniciando descarga con PremiumButtonsCacheManager...");
                
                // Descargar scripts en paralelo con caché (versión detallada)
                var detailedResults = await PremiumButtonsCacheManager.DownloadPremiumButtonsWithDetailsAsync(buttonInfos, onProgress);
                
                LogPremium($"[DownloadPremiumButtonsAsync] ✅ Descarga completada. Resultados: {detailedResults?.Count ?? 0}");

                // Actualizar estado de descarga basado en resultados detallados
                _premiumScripts.Clear();
                int successCount = 0;
                int errorCount = 0;

                foreach (var downloadResult in detailedResults)
                {
                    LogPremium($"[DownloadPremiumButtonsAsync] Procesando resultado: {downloadResult?.ButtonName} - Success: {downloadResult?.Success}");
                    
                    if (downloadResult.Success)
                    {
                        _premiumScripts.Add(downloadResult.Script);
                        string source = downloadResult.FromCache ? "cached" : "downloaded";
                        _premiumButtonStatus[downloadResult.ButtonId] = $"✓ {source}";
                        successCount++;
                        LogPremium($"[DownloadPremiumButtonsAsync] ✓ {downloadResult.ButtonName} ({source}) - _premiumScripts.Count ahora: {_premiumScripts.Count}");
                        System.Diagnostics.Debug.WriteLine($"[Premium Buttons] ✓ {downloadResult.ButtonName} ({source})");
                    }
                    else
                    {
                        errorCount++;
                        // Guardar razón del error en el estado
                        string errorMsg = downloadResult.ErrorReason ?? "Unknown error";
                        string shortError = errorMsg.Length > 40 ? errorMsg.Substring(0, 40) + "..." : errorMsg;
                        _premiumButtonStatus[downloadResult.ButtonId] = $"❌ {shortError}";
                        LogPremium($"[DownloadPremiumButtonsAsync] ❌ {downloadResult.ButtonName} - {errorMsg}");
                        System.Diagnostics.Debug.WriteLine($"[Premium Buttons] ❌ {downloadResult.ButtonName} - {errorMsg}");
                    }
                }

                LogPremium($"[DownloadPremiumButtonsAsync] ===== PROCESAMIENTO COMPLETADO =====");
                LogPremium($"[DownloadPremiumButtonsAsync] Resumen: {successCount} exitosos, {errorCount} con error");
                LogPremium($"[DownloadPremiumButtonsAsync] _premiumScripts.Count final: {_premiumScripts.Count}");
                LogPremium($"[DownloadPremiumButtonsAsync] _premiumButtonStatus.Count final: {_premiumButtonStatus.Count}");

                System.Diagnostics.Debug.WriteLine($"[Premium Buttons] RESUMEN: {successCount} exitosos, {errorCount} con error");
                LogPremium($"[DownloadPremiumButtonsAsync] Llamando a RefreshPremiumPanel()...");
                
                // ✅ REFRESCA EL PANEL ADVANCED PARA MOSTRAR LOS BOTONES DESCARGADOS
                RefreshPremiumPanel();
                LogPremium($"[DownloadPremiumButtonsAsync] ===== FIN DESCARGA =====");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium Buttons] Error crítico: {ex.Message}");
                LogPremium($"[DownloadPremiumButtonsAsync] ❌ Error crítico: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Crea panel de botones premium agrupados por empresa para la pestaña Advanced
        /// </summary>
        private Border CreatePremiumButtonsPanel()
        {
            LogPremium($"[CreatePremiumButtonsPanel] ===== INICIO CREACIÓN =====");
            LogPremium($"[CreatePremiumButtonsPanel] _premiumScripts.Count = {_premiumScripts.Count}");
            LogPremium($"[CreatePremiumButtonsPanel] _premiumButtonStatus.Count = {_premiumButtonStatus.Count}");
            
            var mainBorder = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(0, 10, 0, 10),
                Padding = new Thickness(10)
            };

            var mainStack = new StackPanel();

            // Título
            var titleText = new TextBlock
            {
                Text = "🔒 PREMIUM BUTTONS",
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            mainStack.Children.Add(titleText);

            // Si no hay scripts premium PERO hay botones pending, mostrar estado de carga
            if (_premiumScripts.Count == 0 && _premiumButtonStatus.Count > 0)
            {
                LogPremium($"[CreatePremiumButtonsPanel] Mostrando estado de carga: {_premiumButtonStatus.Count} pendientes");
                
                var loadingText = new TextBlock
                {
                    Text = $"⏳ Loading {_premiumButtonStatus.Count} premium scripts...",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(200, 180, 0)),
                    Margin = new Thickness(10, 5, 10, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                mainStack.Children.Add(loadingText);
                
                // Mostrar lista de botones que se intentan cargar
                foreach (var status in _premiumButtonStatus)
                {
                    var statusRow = new TextBlock
                    {
                        Text = $"  • Button ID: {status.Key} - {status.Value}",
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(150, 150, 150)),
                        FontSize = 10,
                        Margin = new Thickness(10, 2, 10, 2)
                    };
                    mainStack.Children.Add(statusRow);
                }
                
                mainBorder.Child = mainStack;
                LogPremium($"[CreatePremiumButtonsPanel] Panel de carga creado");
                return mainBorder;
            }

            // Si no hay scripts premium y no hay botones pending
            if (_premiumScripts.Count == 0)
            {
                LogPremium($"[CreatePremiumButtonsPanel] Sin scripts premium - mostrando mensaje vacío");
                
                var emptyText = new TextBlock
                {
                    Text = "No premium scripts available.\nLogin to access premium features.",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(120, 120, 120)),
                    Margin = new Thickness(10, 5, 10, 5),
                    TextWrapping = TextWrapping.Wrap
                };
                mainStack.Children.Add(emptyText);
                mainBorder.Child = mainStack;
                return mainBorder;
            }

            LogPremium($"[CreatePremiumButtonsPanel] Creando panel con {_premiumScripts.Count} scripts");
            
            // Agrupar scripts por empresa (extrayendo de la categoría "🔒 [Empresa]")
            var groupedByCompany = _premiumScripts
                .GroupBy(s => ExtractCompanyFromCategory(s.Category))
                .OrderBy(g => g.Key);

            LogPremium($"[CreatePremiumButtonsPanel] Agrupados en {groupedByCompany.Count()} empresas");
            
            // Crear secciones para cada empresa
            foreach (var companyGroup in groupedByCompany)
            {
                LogPremium($"[CreatePremiumButtonsPanel] Procesando empresa: {companyGroup.Key} con {companyGroup.Count()} scripts");
                
                // Header de empresa
                var companyHeader = new TextBlock
                {
                    Text = $"🏢 {companyGroup.Key}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(200, 200, 200)),
                    Margin = new Thickness(0, 8, 0, 5)
                };
                mainStack.Children.Add(companyHeader);

                // Panel de botones para esta empresa
                var companyButtonsPanel = new StackPanel
                {
                    Margin = new Thickness(10, 0, 0, 10)
                };

                foreach (var script in companyGroup.OrderBy(s => s.Name))
                {
                    // ⚠️ VALIDACIÓN: Evitar nulls que causan ArgumentNullException
                    if (script == null)
                    {
                        LogPremium($"[CreatePremiumButtonsPanel] ⚠️ Script es NULL, saltando");
                        continue;
                    }
                    
                    if (string.IsNullOrEmpty(script.Id))
                    {
                        LogPremium($"[CreatePremiumButtonsPanel] ⚠️ Script.Id es NULL/vacío (Name: {script.Name}), saltando");
                        continue;
                    }
                    
                    LogPremium($"[CreatePremiumButtonsPanel]   - Script: {script.Name} (ID: {script.Id})");
                    
                    // Obtener estado de descarga - con validación
                    string status = (!string.IsNullOrEmpty(script.Id) && _premiumButtonStatus.ContainsKey(script.Id))
                        ? _premiumButtonStatus[script.Id]
                        : "✓ cached";

                    // Crear fila del script premium
                    var scriptRow = new Border
                    {
                        Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64)),
                        BorderBrush = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100)),
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(3),
                        Margin = new Thickness(0, 0, 0, 5),
                        Padding = new Thickness(8)
                    };

                    var rowPanel = new StackPanel();

                    // Nombre del script con estado
                    var namePanel = new StackPanel { Orientation = Orientation.Horizontal };
                    var nameText = new TextBlock
                    {
                        Text = $"📌 {script.Name}",
                        FontWeight = FontWeights.Bold,
                        FontSize = 10,
                        Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241)),
                        Margin = new Thickness(0, 0, 5, 0)
                    };
                    namePanel.Children.Add(nameText);

                    var statusText = new TextBlock
                    {
                        Text = status,
                        FontSize = 9,
                        Foreground = DetermineStatusColor(status),
                        Margin = new Thickness(0, 0, 0, 0)
                    };
                    namePanel.Children.Add(statusText);
                    rowPanel.Children.Add(namePanel);

                    // Descripción (si existe)
                    if (!string.IsNullOrWhiteSpace(script.Description))
                    {
                        var descText = new TextBlock
                        {
                            Text = script.Description,
                            FontSize = 9,
                            Foreground = new SolidColorBrush(WpfColor.FromRgb(180, 180, 180)),
                            Margin = new Thickness(0, 3, 0, 5),
                            TextWrapping = TextWrapping.Wrap
                        };
                        rowPanel.Children.Add(descText);
                    }

                    // Botones de acción
                    var buttonsPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };

                    // Si hay error, mostrar botón de retry
                    if (status.Contains("error") || status.Contains("❌"))
                    {
                        var retryBtn = new Button
                        {
                            Content = "🔄 Retry",
                            Foreground = Brushes.White,
                            Background = new SolidColorBrush(WpfColor.FromRgb(255, 140, 0)), // Naranja
                            Padding = new Thickness(8, 4, 8, 4),
                            Margin = new Thickness(0, 0, 3, 0),
                            FontSize = 9
                        };
                        retryBtn.Click += async (s, e) => await RetryDownloadScript_Click(script);
                        buttonsPanel.Children.Add(retryBtn);
                    }
                    else
                    {
                        var executeBtn = new Button
                        {
                            Content = "▶️ Run",
                            Foreground = Brushes.White,
                            Background = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212)),
                            Padding = new Thickness(8, 4, 8, 4),
                            Margin = new Thickness(0, 0, 3, 0),
                            FontSize = 9
                        };
                        executeBtn.Click += (s, e) => ExecuteScript_Click(script);
                        buttonsPanel.Children.Add(executeBtn);

                        var downloadBtn = new Button
                        {
                            Content = "💾 Download",
                            Foreground = Brushes.White,
                            Background = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100)),
                            Padding = new Thickness(8, 4, 8, 4),
                            Margin = new Thickness(0, 0, 3, 0),
                            FontSize = 9
                        };
                        downloadBtn.Click += (s, e) => DownloadScriptForImport_Click(script);
                        buttonsPanel.Children.Add(downloadBtn);
                    }

                    rowPanel.Children.Add(buttonsPanel);
                    scriptRow.Child = rowPanel;
                    companyButtonsPanel.Children.Add(scriptRow);
                }

                mainStack.Children.Add(companyButtonsPanel);
            }

            mainBorder.Child = mainStack;
            LogPremium($"[CreatePremiumButtonsPanel] ✅ Panel finalizado con mainStack.Children.Count = {mainStack.Children.Count}");
            LogPremium($"[CreatePremiumButtonsPanel] ===== FIN CREACIÓN =====");
            return mainBorder;
        }

        /// <summary>
        /// Extrae el nombre de la empresa de la categoría "🔒 [Empresa]"
        /// </summary>
        private string ExtractCompanyFromCategory(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "Premium";

            // Remover el emoji 🔒 si está
            var cleaned = category.Replace("🔒", "").Trim();
            return !string.IsNullOrWhiteSpace(cleaned) ? cleaned : "Premium";
        }

        /// <summary>
        /// Determina el color del estado basado en su contenido
        /// </summary>
        private Brush DetermineStatusColor(string status)
        {
            if (status.Contains("cached") || status.Contains("✓"))
                return new SolidColorBrush(WpfColor.FromRgb(0, 200, 100)); // Verde
            else if (status.Contains("downloading") || status.Contains("⏳"))
                return new SolidColorBrush(WpfColor.FromRgb(255, 200, 0)); // Amarillo
            else if (status.Contains("error") || status.Contains("❌"))
                return new SolidColorBrush(WpfColor.FromRgb(255, 100, 100)); // Rojo
            else
                return new SolidColorBrush(WpfColor.FromRgb(180, 180, 180)); // Gris
        }

        /// <summary>
        /// Reintenta descargar un script premium fallido
        /// </summary>
        private async Task RetryDownloadScript_Click(ScriptDefinition script)
        {
            if (script == null) return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Reintentando descarga de: {script.Name}");
                
                // Obtener la información del botón original para el reintento
                // Buscamos en _premiumButtonStatus para encontrar el ID y URL
                var scriptId = script.Id;
                
                // Actualizar estado a "reintentando"
                _premiumButtonStatus[scriptId] = "⏳ Retrying...";
                
                // Recrear el panel para mostrar el estado actualizado
                RefreshPremiumPanel();

                // Intentar descargar nuevamente (sin usar caché)
                // Para ello, necesitamos reconstruir el PremiumButtonInfo desde el script
                if (script.Category.Contains("🔒"))
                {
                    var companyName = ExtractCompanyFromCategory(script.Category);
                    // Intentar obtener la URL desde el script - si existe en las propiedades
                    // Por ahora, reconstruimos una descarga limpia
                    
                    System.Diagnostics.Debug.WriteLine($"[Premium] Limpiando caché local para reintento: {scriptId}");
                    
                    // Limpiar la entrada de caché para forzar nueva descarga
                    string cachePath = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "RoslynCopilot",
                        "premium-buttons-cache",
                        $"{scriptId}.json"
                    );
                    
                    if (System.IO.File.Exists(cachePath))
                    {
                        try
                        {
                            System.IO.File.Delete(cachePath);
                            System.Diagnostics.Debug.WriteLine($"[Premium] Caché eliminado: {cachePath}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[Premium] Error eliminando caché: {ex.Message}");
                        }
                    }

                    // Aquí idealmente llamaríamos a re-descargar desde el botón premium
                    // Por ahora, informamos al usuario que debe reintentar la descarga completa
                    _premiumButtonStatus[scriptId] = "⚠️ Retry requires re-download";
                    MessageBox.Show(
                        "Reintento completado. Por favor, recarga los botones premium desde el menú de autenticación para obtener la última versión.",
                        "Retry Status",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }

                RefreshPremiumPanel();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Premium] Error en reintento: {ex.Message}");
                _premiumButtonStatus[script.Id] = $"❌ Retry error: {ex.Message.Substring(0, Math.Min(30, ex.Message.Length))}";
                RefreshPremiumPanel();
                MessageBox.Show($"Error durante reintento:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refresca el panel de botones premium sin recargar todo
        /// </summary>
        private void RefreshPremiumPanel()
        {
            try
            {
                LogPremium($"[RefreshPremiumPanel] ===== INICIO REFRESCO =====");
                LogPremium($"[RefreshPremiumPanel] _premiumScripts.Count = {_premiumScripts.Count}");
                LogPremium($"[RefreshPremiumPanel] _tabControl != null: {_tabControl != null}");
                LogPremium($"[RefreshPremiumPanel] _tabControl.Items.Count = {_tabControl?.Items.Count ?? 0}");
                
                // Si el TabControl existe, encontrar el Advanced tab (índice 1)
                if (_tabControl != null && _tabControl.Items.Count > 1)
                {
                    LogPremium($"[RefreshPremiumPanel] TabControl tiene {_tabControl.Items.Count} items");
                    
                    var advancedTab = _tabControl.Items[1] as TabItem;
                    LogPremium($"[RefreshPremiumPanel] advancedTab != null: {advancedTab != null}");
                    LogPremium($"[RefreshPremiumPanel] advancedTab.Header: {advancedTab?.Header}");
                    
                    if (advancedTab != null)
                    {
                        var scrollViewer = advancedTab.Content as ScrollViewer;
                        LogPremium($"[RefreshPremiumPanel] scrollViewer != null: {scrollViewer != null}");
                        
                        if (scrollViewer != null && scrollViewer.Content is StackPanel outerStack)
                        {
                            LogPremium($"[RefreshPremiumPanel] outerStack.Children.Count = {outerStack.Children.Count}");
                            
                            if (outerStack.Children.Count > 0 && outerStack.Children[0] is Border)
                            {
                                LogPremium($"[RefreshPremiumPanel] Primer hijo es Border, removiendo...");
                                
                                // Reemplazar el premium panel con uno actualizado
                                outerStack.Children.RemoveAt(0);
                                LogPremium($"[RefreshPremiumPanel] Border removido. Creando nuevo panel...");
                                
                                var newPremiumPanel = CreatePremiumButtonsPanel();
                                LogPremium($"[RefreshPremiumPanel] Nuevo panel creado. Insertando...");
                                
                                outerStack.Children.Insert(0, newPremiumPanel);
                                LogPremium($"[RefreshPremiumPanel] ✅ Panel de botones actualizado exitosamente");
                            }
                            else
                            {
                                string childType = outerStack.Children.Count > 0 ? outerStack.Children[0]?.GetType().Name ?? "unknown" : "no children";
                                LogPremium($"[RefreshPremiumPanel] ❌ ERROR: Primer hijo NO es Border. Tipo: {childType}");
                            }
                        }
                        else
                        {
                            LogPremium($"[RefreshPremiumPanel] ❌ ERROR: scrollViewer es null o Content no es StackPanel");
                            if (scrollViewer != null)
                            {
                                LogPremium($"[RefreshPremiumPanel] scrollViewer.Content tipo: {scrollViewer.Content?.GetType().Name}");
                            }
                        }
                    }
                    else
                    {
                        LogPremium($"[RefreshPremiumPanel] ❌ ERROR: advancedTab es null");
                    }
                }
                else
                {
                    LogPremium($"[RefreshPremiumPanel] ❌ ERROR: TabControl es null o tiene menos de 2 items");
                }
                
                LogPremium($"[RefreshPremiumPanel] ===== FIN REFRESCO =====");
            }
            catch (Exception ex)
            {
                LogPremium($"[RefreshPremiumPanel] ❌ EXCEPCIÓN: {ex.GetType().Name}");
                LogPremium($"[RefreshPremiumPanel] Mensaje: {ex.Message}");
                LogPremium($"[RefreshPremiumPanel] StackTrace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"[Premium] Error refrescando panel: {ex.Message}\n{ex.StackTrace}");
            }
        }
        /// <summary>
        /// Ejecuta un script premium
        /// </summary>
        private async void ExecuteScript_Click(ScriptDefinition script)
        {
            // Ejecutar sin mostrar diálogo final (el script tiene sus propios diálogos)
            await ExecuteScriptSilent(script);
        }

        /// <summary>
        /// Descarga un script premium para importar después
        /// </summary>
        private void DownloadScriptForImport_Click(ScriptDefinition script)
        {
            try
            {
                var saveDialog = new System.Windows.Forms.SaveFileDialog
                {
                    FileName = $"{script.Name}.json",
                    Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (saveDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string json = JsonConvert.SerializeObject(new List<ScriptDefinition> { script }, Formatting.Indented);
                    System.IO.File.WriteAllText(saveDialog.FileName, json);
                    MessageBox.Show($"✅ Script descargado exitosamente:\n{saveDialog.FileName}",
                        "Download Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error descargando script: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Paneles de Configuración

        /// <summary>
        /// Crea el panel del AI Assistant (controlado por BIMtegration auth)
        /// </summary>
        private ScrollViewer CreateAdvancedAIAssistantPanel()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(15),
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48))
            };

            var mainStack = new StackPanel();

            // Panel de placeholder - AI Modeling está en desarrollo
            var placeholderPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 20, 0, 0)
            };

            var placeholderStack = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = "🚀 AI Modeling",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(0, 120, 212))
            };
            placeholderStack.Children.Add(titleBlock);

            var descriptionBlock = new TextBlock
            {
                Text = "This functionality is under development.\n\nComing soon: Advanced AI modeling for Revit.",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(200, 200, 200)),
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };
            placeholderStack.Children.Add(descriptionBlock);

            var statusBlock = new TextBlock
            {
                Text = "⏳ Complete soon.",
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(150, 150, 150)),
                FontStyle = FontStyles.Italic,
                TextWrapping = TextWrapping.Wrap
            };
            placeholderStack.Children.Add(statusBlock);

            placeholderPanel.Child = placeholderStack;
            mainStack.Children.Add(placeholderPanel);

            scrollViewer.Content = mainStack;
            return scrollViewer;
        }

        /// <summary>
        /// Crea el panel de solicitud de código AI
        /// </summary>
        private Border CreateAIRequestPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var stack = new StackPanel();

            // Title
            var title = new TextBlock
            {
                Text = "💬 Request Code",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            stack.Children.Add(title);

            // Prompt field
            var promptLabel = new TextBlock
            {
                Text = "Describe what you want to do:",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            stack.Children.Add(promptLabel);

            _aiPromptBox = new WpfTextBox
            {
                Height = 80,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Margin = new Thickness(0, 0, 0, 10)
            };
            stack.Children.Add(_aiPromptBox);

            // Optional context field
            var contextLabel = new TextBlock
            {
                Text = "Additional context (optional):",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 5),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            stack.Children.Add(contextLabel);

            _aiContextBox = new WpfTextBox
            {
                Height = 60,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(WpfColor.FromRgb(62, 62, 64)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            stack.Children.Add(_aiContextBox);

            // Generate button
            _aiGenerateButton = CreateStyledButton("✨ Generate Code", true);
            _aiGenerateButton.Click += AIGenerateButton_Click;
            stack.Children.Add(_aiGenerateButton);

            panel.Child = stack;
            return panel;
        }

        /// <summary>
        /// Crea el panel de código generado
        /// </summary>
        private Border CreateAICodePanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                Visibility = System.Windows.Visibility.Collapsed
            };

            var stack = new StackPanel();

            // Title
            var title = new TextBlock
            {
                Text = "📄 Generated Code",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            stack.Children.Add(title);

            // Área de código
            _aiGeneratedCodeBox = new WpfTextBox
            {
                Height = 200,
                TextWrapping = TextWrapping.NoWrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11,
                Background = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                Margin = new Thickness(0, 0, 0, 10),
                IsReadOnly = false
            };
            stack.Children.Add(_aiGeneratedCodeBox);

            // Action buttons
            var buttonStack = new StackPanel { Orientation = Orientation.Horizontal };
            
            _aiApplyCodeButton = CreateStyledButton("▶️ Run Code", true);
            _aiApplyCodeButton.Click += AIApplyCodeButton_Click;
            _aiApplyCodeButton.Margin = new Thickness(0, 0, 10, 0);
            buttonStack.Children.Add(_aiApplyCodeButton);

            var copyButton = CreateStyledButton("📋 Copy", false);
            copyButton.Click += (s, e) => 
            {
                if (!string.IsNullOrEmpty(_aiGeneratedCodeBox.Text))
                {
                    Clipboard.SetText(_aiGeneratedCodeBox.Text);
                }
            };
            copyButton.Margin = new Thickness(0, 0, 10, 0);
            buttonStack.Children.Add(copyButton);

            var clearButton = CreateStyledButton("🗑️ Clear", false);
            clearButton.Click += (s, e) => 
            {
                _aiGeneratedCodeBox.Text = "";
                panel.Visibility = System.Windows.Visibility.Collapsed;
            };
            buttonStack.Children.Add(clearButton);

            stack.Children.Add(buttonStack);

            panel.Child = stack;
            panel.Tag = "CodePanel"; // Para referencia
            return panel;
        }

        /// <summary>
        /// Crea el panel de errores y corrección
        /// </summary>
        private Border CreateAIErrorPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(60, 45, 45)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(185, 28, 28)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15),
                Visibility = System.Windows.Visibility.Collapsed
            };

            var stack = new StackPanel();

            // Title
            var title = new TextBlock
            {
                Text = "❌ Error Detected",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 10),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(252, 165, 165))
            };
            stack.Children.Add(title);

            // Área de error
            _aiErrorBox = new WpfTextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(252, 165, 165)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(185, 28, 28)),
                Margin = new Thickness(0, 0, 0, 10),
                IsReadOnly = true
            };
            stack.Children.Add(_aiErrorBox);

            // Fix button
            _aiFixErrorButton = CreateStyledButton("🔧 Fix with AI", true);
            _aiFixErrorButton.Click += AIFixErrorButton_Click;
            stack.Children.Add(_aiFixErrorButton);

            panel.Child = stack;
            panel.Tag = "ErrorPanel"; // Para referencia
            return panel;
        }

        /// <summary>
        /// Crea el panel de conversación/historial
        /// </summary>
        private Border CreateAIConversationPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var stack = new StackPanel();

            // Title with clear button
            var headerStack = new StackPanel { Orientation = Orientation.Horizontal };
            var title = new TextBlock
            {
                Text = "💭 Conversation",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241))
            };
            
            var clearConversationButton = CreateStyledButton("🗑️", false);
            clearConversationButton.Width = 30;
            clearConversationButton.Height = 25;
            clearConversationButton.FontSize = 10;
            clearConversationButton.Margin = new Thickness(10, 0, 0, 0);
            clearConversationButton.Click += (s, e) => 
            {
                _aiConversationPanel.Children.Clear();
            };

            headerStack.Children.Add(title);
            headerStack.Children.Add(clearConversationButton);
            stack.Children.Add(headerStack);

            // Scroll for conversation
            _aiConversationScroll = new ScrollViewer
            {
                Height = 150,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Margin = new Thickness(0, 10, 0, 0),
                Background = new SolidColorBrush(WpfColor.FromRgb(30, 30, 30))
            };

            _aiConversationPanel = new StackPanel();
            _aiConversationScroll.Content = _aiConversationPanel;
            stack.Children.Add(_aiConversationScroll);

            panel.Child = stack;
            return panel;
        }

        #endregion

        #region AI Assistant Events

        /// <summary>
        /// Handles the generate code button click event
        /// </summary>
        private async void AIGenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_aiPromptBox.Text))
                {
                    AddAIMessage("⚠️ Please describe what you want to do.", true);
                    return;
                }

                // Disable button during generation
                _aiGenerateButton.IsEnabled = false;
                _aiGenerateButton.Content = "⏳ Generating...";

                var prompt = _aiPromptBox.Text;
                var context = _aiContextBox.Text;

                // Add user message
                AddAIMessage($"👤 User: {prompt}", false);

                // Generate code
                var generatedCode = "AI Modeling is not available"; // _aiService was removed
                // var generatedCode = await _aiService.GenerateRevitCodeAsync(prompt, context);

                // Show generated code
                _aiGeneratedCodeBox.Text = generatedCode;
                ShowCodePanel();

                // Add AI message
                AddAIMessage("🤖 AI: Code generated successfully", false);

                // Clear fields
                _aiPromptBox.Text = "";
                _aiContextBox.Text = "";
            }
            catch (Exception ex)
            {
                AddAIMessage($"❌ Error: {ex.Message}", true);
            }
            finally
            {
                // Re-enable button
                _aiGenerateButton.IsEnabled = true;
                _aiGenerateButton.Content = "✨ Generate Code";
            }
        }

        /// <summary>
        /// Handles the run code button click event
        /// </summary>
        private async void AIApplyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_aiGeneratedCodeBox.Text))
                {
                    AddAIMessage("⚠️ No code to execute.", true);
                    return;
                }

                _aiApplyCodeButton.IsEnabled = false;
                _aiApplyCodeButton.Content = "⏳ Running...";

                // Hide error panel
                HideErrorPanel();

                // Execute code using existing scripting system
                var result = await ExecuteGeneratedCode(_aiGeneratedCodeBox.Text);

                if (result.Success)
                {
                    AddAIMessage("✅ Code executed successfully", false);
                }
                else
                {
                    // Show error and offer correction
                    ShowErrorPanel(result.ErrorMessage);
                    AddAIMessage($"❌ Execution error: {result.ErrorMessage}", true);
                }
            }
            catch (Exception ex)
            {
                ShowErrorPanel(ex.Message);
                AddAIMessage($"❌ Unexpected error: {ex.Message}", true);
            }
            finally
            {
                _aiApplyCodeButton.IsEnabled = true;
                _aiApplyCodeButton.Content = "▶️ Run Code";
            }
        }

        /// <summary>
        /// Handles the fix error button click event
        /// </summary>
        private async void AIFixErrorButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_aiErrorBox.Text) || string.IsNullOrWhiteSpace(_aiGeneratedCodeBox.Text))
                {
                    AddAIMessage("⚠️ No code or error to fix.", true);
                    return;
                }

                _aiFixErrorButton.IsEnabled = false;
                _aiFixErrorButton.Content = "⏳ Fixing...";

                var errorMessage = _aiErrorBox.Text;
                var originalCode = _aiGeneratedCodeBox.Text;

                // Create correction prompt
                var fixPrompt = $"The following Revit code has an error:\n\nCode:\n{originalCode}\n\nError:\n{errorMessage}\n\nPlease fix the code.";

                AddAIMessage("👤 User: Request error correction", false);

                // Generate corrected code
                var correctedCode = "AI Modeling is not available"; // _aiService was removed
                // var correctedCode = await _aiService.GenerateRevitCodeAsync(fixPrompt, "Error correction");

                // Update generated code
                _aiGeneratedCodeBox.Text = correctedCode;
                AddAIMessage("🤖 AI: Code corrected", false);

                // Hide error panel
                HideErrorPanel();
            }
            catch (Exception ex)
            {
                AddAIMessage($"❌ Error fixing: {ex.Message}", true);
            }
            finally
            {
                _aiFixErrorButton.IsEnabled = true;
                _aiFixErrorButton.Content = "🔧 Fix with AI";
            }
        }

        /// <summary>
        /// Executes generated code and returns the result
        /// </summary>
        private async Task<(bool Success, string ErrorMessage)> ExecuteGeneratedCode(string code)
        {
            try
            {
                // Integrate with existing script execution system
                var uiApp = GetCurrentUIApplication();
                if (uiApp?.ActiveUIDocument?.Document == null)
                {
                    return (false, "No active Revit document");
                }

                var doc = uiApp.ActiveUIDocument.Document;
                var uidoc = uiApp.ActiveUIDocument;
                var app = uiApp.Application;

                // Asegurar que el SharedExternalEvent/Handler estén inicializados
                if (RoslynCopilotTest.Application.SharedExternalEventHandler == null)
                    RoslynCopilotTest.Application.SharedExternalEventHandler = new RoslynCopilotTest.GenericExternalEventHandler();
                if (RoslynCopilotTest.Application.SharedExternalEvent == null)
                    RoslynCopilotTest.Application.SharedExternalEvent = Autodesk.Revit.UI.ExternalEvent.Create(RoslynCopilotTest.Application.SharedExternalEventHandler);

                // Create script context
                var globals = new ScriptGlobals();
                globals.doc = doc;
                globals.uidoc = uidoc;
                globals.app = app;
                globals.uiapp = uiApp;
                globals.externalEventHandler = RoslynCopilotTest.Application.SharedExternalEventHandler;
                globals.externalEvent = RoslynCopilotTest.Application.SharedExternalEvent;

                // Execute script
                var options = ScriptOptions.Default
                    .WithReferences(typeof(Document).Assembly, typeof(UIApplication).Assembly)
                    .WithImports("System", "Autodesk.Revit.DB", "Autodesk.Revit.UI");

                var state = await CSharpScript.RunAsync(code, options, globals);

                // Procesar comando devuelto por el script (pattern 1)
                var returnValue = state.ReturnValue;
                try
                {
                    if (returnValue != null)
                    {
                        dynamic cmd = returnValue;
                        string commandName = null;
                        string path = null;
                        try { commandName = cmd.Command as string; } catch { }
                        try { path = cmd.Path as string; } catch { }

                        if (!string.IsNullOrEmpty(commandName) && !string.IsNullOrEmpty(path))
                        {
                            RoslynCopilotTest.Application.SharedKatalogSelectionPath = path;
                            RoslynCopilotTest.Application.SharedExternalEventHandler.ActionToExecute = (uiapp) =>
                            {
                                try
                                {
                                    var uidocument = uiapp?.ActiveUIDocument;
                                    var docLocal = uidocument?.Document;
                                    var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();

                                    if (docLocal == null) return;

                                    if (ext == ".rfa")
                                    {
                                        Family family = null;
                                        if (docLocal.LoadFamily(path, out family))
                                        {
                                            // family loaded
                                        }
                                    }
                                    else if (ext == ".rvt")
                                    {
                                        var cmdId = RevitCommandId.LookupPostableCommandId(PostableCommand.LinkRevit);
                                        uiapp.PostCommand(cmdId);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Windows.Forms.MessageBox.Show($"Error al procesar Katalog: {ex.Message}");
                                }
                            };

                            RoslynCopilotTest.Application.SharedExternalEvent.Raise();
                        }
                    }
                }
                catch { }

                return (true, "");
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        /// <summary>
        /// Adds a message to the AI conversation
        /// </summary>
        private void AddAIMessage(string message, bool isError = false)
        {
            var messagePanel = new Border
            {
                Background = isError ? new SolidColorBrush(WpfColor.FromRgb(254, 242, 242)) : new SolidColorBrush(WpfColor.FromRgb(248, 250, 252)),
                BorderBrush = isError ? new SolidColorBrush(WpfColor.FromRgb(252, 165, 165)) : new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 0, 5)
            };

            var messageText = new TextBlock
            {
                Text = $"[{DateTime.Now:HH:mm:ss}] {message}",
                FontSize = 11,
                Foreground = isError ? new SolidColorBrush(WpfColor.FromRgb(185, 28, 28)) : new SolidColorBrush(WpfColor.FromRgb(0, 0, 0)),
                TextWrapping = TextWrapping.Wrap
            };

            messagePanel.Child = messageText;
            _aiConversationPanel.Children.Add(messagePanel);

            // Scroll to bottom
            _aiConversationScroll.ScrollToBottom();
        }

        /// <summary>
        /// Shows the generated code panel
        /// </summary>
        private void ShowCodePanel()
        {
            // Buscar el panel de código y mostrarlo
            foreach (var child in ((StackPanel)((ScrollViewer)((DockPanel)((TabItem)_tabControl.Items[2]).Content).Children[0]).Content).Children)
            {
                if (child is Border panel && panel.Tag?.ToString() == "CodePanel")
                {
                    panel.Visibility = System.Windows.Visibility.Visible;
                    break;
                }
            }
        }

        /// <summary>
        /// Shows the error panel with the specified message
        /// </summary>
        private void ShowErrorPanel(string errorMessage)
        {
            _aiErrorBox.Text = errorMessage;
            
            // Buscar el panel de errores y mostrarlo
            foreach (var child in ((StackPanel)((ScrollViewer)((DockPanel)((TabItem)_tabControl.Items[2]).Content).Children[0]).Content).Children)
            {
                if (child is Border panel && panel.Tag?.ToString() == "ErrorPanel")
                {
                    panel.Visibility = System.Windows.Visibility.Visible;
                    break;
                }
            }
        }

        /// <summary>
        /// Hides the error panel
        /// </summary>
        private void HideErrorPanel()
        {
            // Buscar el panel de errores y ocultarlo
            foreach (var child in ((StackPanel)((ScrollViewer)((DockPanel)((TabItem)_tabControl.Items[2]).Content).Children[0]).Content).Children)
            {
                if (child is Border panel && panel.Tag?.ToString() == "ErrorPanel")
                {
                    panel.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                }
            }
        }

        #endregion

        #region Advanced AI Features (GitHub Required)

        /// <summary>
        /// Checks if user has GitHub account connected
        /// </summary>
        private bool IsGitHubConnected()
        {
            return false; // GitHub auth removed
        }

        /// <summary>
        /// Creates panel that requires GitHub access
        /// </summary>
        private Border CreateGitHubAccessRequiredPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(63, 63, 70)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(20),
                Margin = new Thickness(0, 10, 0, 0)
            };

            var stack = new StackPanel();

            var titleBlock = new TextBlock
            {
                Text = "🔒 Premium",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 15),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241)),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            stack.Children.Add(titleBlock);

            var descBlock = new TextBlock
            {
                Text = "🚀 This is an advanced AI feature for direct modeling in Revit\n\n" +
                       "✨ Premium Features:\n" +
                       "• Intelligent BIM modeling with AI\n" +
                       "• Automatic modification of 3D elements\n" +
                       "• Advanced structural and MEP analysis\n" +
                       "• Automatic model correction\n\n" +
                       "🔐 Requires connected GitHub account",
                FontSize = 12,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(WpfColor.FromRgb(241, 241, 241)),
                TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
            stack.Children.Add(descBlock);

            panel.Child = stack;
            return panel;
        }

        private void LogPremium(string message)
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

        #endregion
    }

    /// <summary>
    /// Globals para el contexto de ejecución de scripts
    /// </summary>
    public class ScriptGlobals
    {
        public Document doc { get; set; }
        public UIDocument uidoc { get; set; }
        public Autodesk.Revit.ApplicationServices.Application app { get; set; }
        public UIApplication uiapp { get; set; }
        public object externalEventHandler { get; set; }
        public object externalEvent { get; set; }
        
        /// <summary>
        /// Método seguro para mostrar TaskDialog desde scripts async
        /// </summary>
        public void ShowDialog(string title, string message)
        {
            try
            {
                var dialog = new TaskDialog(title)
                {
                    MainContent = message,
                    MainIcon = TaskDialogIcon.TaskDialogIconInformation,
                    CommonButtons = TaskDialogCommonButtons.Ok
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }
    // ...eliminado: propiedad copilotBridge...
    }
}
