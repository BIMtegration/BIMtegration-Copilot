using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

// Alias para evitar conflictos de namespace
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfButton = System.Windows.Controls.Button;
using WpfControl = System.Windows.Controls.Control;
using WpfGrid = System.Windows.Controls.Grid;
using MediaColor = System.Windows.Media.Color;

namespace RoslynCopilotTest.UI
{
    public class ScriptEditorWindow : Window
    {
        private WpfTextBox _nameTextBox;
        private WpfTextBox _descriptionTextBox;
        private WpfComboBox _categoryComboBox;
        private WpfTextBox _codeTextBox;
        private WpfTextBox _resultTextBox;
        private WpfButton _executeButton;
        private WpfButton _saveButton;
        private WpfButton _clearButton;
        private WpfButton _templatesButton;
        private WpfButton _aiAssistButton;
        private WpfButton _cancelButton;

        // Controles para previsualización de respuesta AI
        private Border _aiPreviewPanel;
        private WpfTextBox _aiResponseTextBox;
        private WpfButton _aiAcceptButton;
        private WpfButton _aiRejectButton;
        private string _pendingAICode;

        // Controles para chat con IA
        private WpfTextBox _chatHistoryTextBox;
        private WpfTextBox _chatInputTextBox;
        private WpfButton _sendButton;

        private readonly Action _onScriptSaved;
        private ScriptDefinition _editingScript; // Script que se está editando (null si es nuevo)
        private GitHubAIService _aiService;

        public ScriptEditorWindow(Action onScriptSaved = null)
        {
            _onScriptSaved = onScriptSaved;
            _editingScript = null; // Modo nuevo script
            _aiService = new GitHubAIService();
            
            InitializeWindow();
            CreateControls();
            LayoutControls();
            InitializeAIFeatures();
        }

        /// <summary>
        /// Constructor para editar un script existente
        /// </summary>
        public ScriptEditorWindow(Action onScriptSaved, ScriptDefinition scriptToEdit) : this(onScriptSaved)
        {
            _editingScript = scriptToEdit;
            PreloadScriptData();
        }

        private void InitializeWindow()
        {
            // Título dinámico según si es edición o creación
            Title = _editingScript != null 
                ? $"✏️ Editing: {_editingScript.Name} - Roslyn Copilot"
                : "📝 Script Editor - Roslyn Copilot";
                
            Width = 800;
            Height = 700;
            MinWidth = 600;
            MinHeight = 500;
            
            // Centrar en pantalla
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Estilo
            Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 48));
            Foreground = Brushes.White;
        }

        /// <summary>
        /// Precarga los datos del script que se está editando
        /// </summary>
        private void PreloadScriptData()
        {
            if (_editingScript == null) return;

            // Esta función se ejecutará después de que se creen los controles
            Loaded += (sender, e) =>
            {
                try
                {
                    _nameTextBox.Text = _editingScript.Name ?? "";
                    _categoryComboBox.Text = _editingScript.Category ?? "";
                    _descriptionTextBox.Text = _editingScript.Description ?? "";
                    _codeTextBox.Text = _editingScript.Code ?? "";
                    
                    // Change save button text when editing
                    _saveButton.Content = "✏️ Update";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading script data: {ex.Message}", "Error", 
                                   MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void CreateControls()
        {
            // Campos de información del script
            _nameTextBox = new WpfTextBox
            {
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100))
            };

            _descriptionTextBox = new WpfTextBox
            {
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100))
            };

            _categoryComboBox = new WpfComboBox
            {
                Height = 30,
                FontSize = 12,
                Margin = new Thickness(5),
                Background = Brushes.White,
                Foreground = Brushes.Black,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100))
            };

            // Cargar categorías existentes
            LoadCategories();

            // Editor de código
            _codeTextBox = new WpfTextBox
            {
                FontFamily = new FontFamily("Consolas, Monaco, monospace"),
                FontSize = 14,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(220, 220, 220)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = "// 🚀 Write your C# code here\n// Available variables:\n// - doc (active Document)\n// - uidoc (UIDocument)\n// - app (Application)\n// - uiapp (UIApplication)\n\n// Example: Count elements\nint wallCount = new FilteredElementCollector(doc)\n    .OfCategory(BuiltInCategory.OST_Walls)\n    .WhereElementIsNotElementType()\n    .GetElementCount();\n\nreturn $\"Total walls: {wallCount}\";"
            };

            // Área de resultados
            _resultTextBox = new WpfTextBox
            {
                Height = 120,
                FontFamily = new FontFamily("Consolas, Monaco, monospace"),
                FontSize = 12,
                Margin = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(25, 25, 25)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(180, 180, 180)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                IsReadOnly = true,
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Text = "💡 Execution results will appear here..."
            };

            // Botones
            _executeButton = CreateButton("▶️ Run", Colors.Green, ExecuteScript);
            _saveButton = CreateButton("💾 Save", Colors.Blue, SaveScript);
            _clearButton = CreateButton("🗑️ Clear", Colors.Orange, ClearEditor);
            _templatesButton = CreateButton("📋 Templates", Colors.Purple, () => { });
            _aiAssistButton = CreateButton("🤖 AI Assist", Colors.MediumPurple, ShowAIAssistDialog);
            _cancelButton = CreateButton("❌ Cancel", Colors.Gray, () => this.Close());
        }

        private WpfButton CreateButton(string text, MediaColor color, Action action)
        {
            return new WpfButton
            {
                Content = text,
                Height = 35,
                Width = 100,
                Margin = new Thickness(5),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(color),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
        }

        private void LoadCategories()
        {
            var scripts = ScriptManager.LoadAllScriptsFlat();
            var categories = scripts.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
            
            // Agregar categorías predeterminadas si no existen
            var defaultCategories = new[] { "Análisis", "Información", "Selección", "Creación", "Modificación", "Utilidades" };
            foreach (var category in defaultCategories)
            {
                if (!categories.Contains(category))
                    categories.Add(category);
            }

            _categoryComboBox.ItemsSource = categories.OrderBy(c => c);
            _categoryComboBox.SelectedIndex = 0;
        }

        private void LayoutControls()
        {
            var mainGrid = new WpfGrid();
            
            // Definir columnas para layout de dos paneles
            // NOTA: Panel derecho (AI Assistant) DESHABILITADO temporalmente para pruebas con clientes
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Editor expandido a todo el ancho
            // mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5) }); // Separador DESHABILITADO
            // mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // AI Panel DESHABILITADO
            
            // Definir filas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Título en toda la parte superior
            var titleLabel = new Label
            {
                Content = _editingScript != null ? $"✏️ Editando: {_editingScript.Name}" : "📝 Crear Nuevo Script",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WpfGrid.SetRow(titleLabel, 0);
            WpfGrid.SetColumnSpan(titleLabel, 1); // Cambio de 3 a 1 columna
            mainGrid.Children.Add(titleLabel);

            // Panel izquierdo - Editor tradicional (ahora ocupa todo el ancho)
            var leftPanel = CreateLeftEditorPanel();
            WpfGrid.SetRow(leftPanel, 1);
            WpfGrid.SetColumn(leftPanel, 0);
            mainGrid.Children.Add(leftPanel);

            // SEPARADOR Y PANEL DERECHO DESHABILITADOS TEMPORALMENTE
            /*
            // Separador vertical
            var separator = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                Width = 1
            };
            WpfGrid.SetRow(separator, 1);
            WpfGrid.SetColumn(separator, 1);
            mainGrid.Children.Add(separator);

            // Panel derecho - AI Assistant integrado
            var rightPanel = CreateIntegratedAIPanel();
            WpfGrid.SetRow(rightPanel, 1);
            WpfGrid.SetColumn(rightPanel, 2);
            mainGrid.Children.Add(rightPanel);
            */

            // Botones en la parte inferior
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5)
            };

            _executeButton.Click += (s, e) => ExecuteScript();
            _saveButton.Click += (s, e) => SaveScript();
            _clearButton.Click += (s, e) => ClearEditor();
            _templatesButton.Click += (s, e) => ShowTemplates();
            _aiAssistButton.Click += (s, e) => ShowAIAssistDialog();
            _cancelButton.Click += (s, e) => this.Close();

            buttonPanel.Children.Add(_executeButton);
            buttonPanel.Children.Add(_saveButton);
            buttonPanel.Children.Add(_clearButton);
            buttonPanel.Children.Add(_templatesButton);
            buttonPanel.Children.Add(_cancelButton);

            WpfGrid.SetRow(buttonPanel, 2);
            WpfGrid.SetColumnSpan(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            this.Content = mainGrid;
        }

        private ScrollViewer CreateLeftEditorPanel()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(5)
            };

            var panel = new StackPanel();

            // Campos de información del script
            var namePanel = CreateLabeledInput("📛 Nombre del Script:", _nameTextBox);
            panel.Children.Add(namePanel);

            var descPanel = CreateLabeledInput("📄 Descripción:", _descriptionTextBox);
            panel.Children.Add(descPanel);

            var catPanel = CreateLabeledInput("📂 Category:", _categoryComboBox);
            panel.Children.Add(catPanel);

            // Editor de código
            var codeLabel = new Label
            {
                Content = "💻 Código C#:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 5, 5, 0)
            };
            panel.Children.Add(codeLabel);

            _codeTextBox.Height = 300;
            _codeTextBox.Margin = new Thickness(5, 0, 5, 5);
            panel.Children.Add(_codeTextBox);

            // Área de resultados
            var resultLabel = new Label
            {
                Content = "📋 Resultados:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 5, 5, 0)
            };
            panel.Children.Add(resultLabel);

            _resultTextBox.Margin = new Thickness(5, 0, 5, 5);
            panel.Children.Add(_resultTextBox);

            scrollViewer.Content = panel;
            return scrollViewer;
        }

        private ScrollViewer CreateIntegratedAIPanel()
        {
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Padding = new Thickness(5)
            };

            var panel = new StackPanel();

            // Título del AI Assistant
            var aiTitle = new Label
            {
                Content = "🤖 AI Assistant",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(5, 0, 5, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            panel.Children.Add(aiTitle);

            // Área de conversación/chat con IA (más grande)
            var chatArea = CreateAIChatArea();
            panel.Children.Add(chatArea);

            // Input de chat con botón enviar
            var chatInputPanel = CreateChatInputPanel();
            panel.Children.Add(chatInputPanel);

            // Panel de previsualización de respuesta AI (inicialmente oculto)
            _aiPreviewPanel = CreateAIPreviewPanel();
            panel.Children.Add(_aiPreviewPanel);

            scrollViewer.Content = panel;
            return scrollViewer;
        }

        private Border CreateAIPreviewPanel()
        {
            var panel = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(40, 40, 50)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 150)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5, 10, 5, 5),
                Padding = new Thickness(10),
                Visibility = System.Windows.Visibility.Collapsed  // Inicialmente oculto
            };

            var stackPanel = new StackPanel();

            // Título del panel
            var titleLabel = new Label
            {
                Content = "🤖 Respuesta de la IA",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.LightBlue),
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 14
            };
            stackPanel.Children.Add(titleLabel);

            // Área para mostrar la respuesta de la IA
            _aiResponseTextBox = new WpfTextBox
            {
                Height = 200,
                FontFamily = new FontFamily("Consolas, Monaco, monospace"),
                FontSize = 11,
                Background = new SolidColorBrush(MediaColor.FromRgb(25, 25, 35)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(220, 220, 220)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(80, 80, 80)),
                BorderThickness = new Thickness(1),
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.NoWrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Margin = new Thickness(0, 0, 0, 10)
            };
            stackPanel.Children.Add(_aiResponseTextBox);

            // Botones de acción
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 5, 0, 0)
            };

            _aiAcceptButton = new WpfButton
            {
                Content = "✅ Aplicar Código",
                Height = 35,
                Width = 120,
                Margin = new Thickness(5, 0, 5, 0),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.Green),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _aiAcceptButton.Click += AIAcceptButton_Click;

            _aiRejectButton = new WpfButton
            {
                Content = "❌ Descartar",
                Height = 35,
                Width = 100,
                Margin = new Thickness(5, 0, 5, 0),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.OrangeRed),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _aiRejectButton.Click += AIRejectButton_Click;

            var copyButton = new WpfButton
            {
                Content = "📋 Copiar",
                Height = 35,
                Width = 80,
                Margin = new Thickness(5, 0, 5, 0),
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.SteelBlue),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            copyButton.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_pendingAICode))
                {
                    Clipboard.SetText(_pendingAICode);
                    _resultTextBox.Text = "📋 Código copiado al portapapeles";
                }
            };

            buttonPanel.Children.Add(_aiAcceptButton);
            buttonPanel.Children.Add(_aiRejectButton);
            buttonPanel.Children.Add(copyButton);
            stackPanel.Children.Add(buttonPanel);

            panel.Child = stackPanel;
            return panel;
        }

        private Border CreateAIChatArea()
        {
            var chatPanel = new Border
            {
                Background = new SolidColorBrush(MediaColor.FromRgb(35, 35, 45)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(70, 70, 80)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(5),
                Margin = new Thickness(5),
                Padding = new Thickness(10),
                Height = 400  // Mucho más grande para que sea el foco principal
            };

            var chatContainer = new StackPanel();

            // Título del chat
            var chatTitle = new Label
            {
                Content = "💬 Conversación con IA",
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.LightBlue),
                Margin = new Thickness(0, 0, 0, 5),
                FontSize = 12
            };
            chatContainer.Children.Add(chatTitle);

            // Área de historial de chat (más grande)
            _chatHistoryTextBox = new WpfTextBox
            {
                Height = 350,  // Mucho más espacio para la conversación
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                Background = new SolidColorBrush(MediaColor.FromRgb(25, 25, 35)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(230, 230, 230)),
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(60, 60, 60)),
                BorderThickness = new Thickness(1),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsReadOnly = true,
                Text = "🤖 ¡Hola! Soy tu asistente de Revit. Puedes preguntarme cualquier cosa o pedirme que genere código.\n\nEjemplos:\n• \"Hola, ¿cómo estás?\"\n• \"Crear un muro de doble nivel\"\n• \"¿Qué día es hoy?\"\n• \"Seleccionar todas las puertas\""
            };
            chatContainer.Children.Add(_chatHistoryTextBox);

            chatPanel.Child = chatContainer;
            return chatPanel;
        }

        private StackPanel CreateChatInputPanel()
        {
            var inputPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(5, 0, 5, 5)
            };

            // Crear nueva área de input específica para el chat
            _chatInputTextBox = new WpfTextBox
            {
                Height = 35,
                FontSize = 12,
                Background = new SolidColorBrush(MediaColor.FromRgb(50, 50, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalContentAlignment = VerticalAlignment.Center,
                Text = "Escribe tu pregunta aquí..."
            };

            // Event para limpiar placeholder
            _chatInputTextBox.GotFocus += (s, e) => {
                if (_chatInputTextBox.Text == "Escribe tu pregunta aquí...")
                {
                    _chatInputTextBox.Text = "";
                    _chatInputTextBox.Foreground = Brushes.White;
                }
            };

            _chatInputTextBox.LostFocus += (s, e) => {
                if (string.IsNullOrWhiteSpace(_chatInputTextBox.Text))
                {
                    _chatInputTextBox.Text = "Escribe tu pregunta aquí...";
                    _chatInputTextBox.Foreground = new SolidColorBrush(MediaColor.FromRgb(150, 150, 150));
                }
            };

            // Event para enviar con Enter
            _chatInputTextBox.KeyDown += async (s, e) => {
                if (e.Key == Key.Enter)
                {
                    await SendChatMessage();
                }
            };

            // Botón enviar
            _sendButton = new WpfButton
            {
                Content = "📤 Enviar",
                Height = 35,
                Width = 80,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.DodgerBlue),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };
            _sendButton.Click += async (s, e) => await SendChatMessage();

            // Configurar el ancho del input para que ocupe el espacio restante
            _chatInputTextBox.Width = 300; // Ancho fijo por ahora

            inputPanel.Children.Add(_chatInputTextBox);
            inputPanel.Children.Add(_sendButton);

            return inputPanel;
        }

        private async Task SendChatMessage()
        {
            try
            {
                var message = _chatInputTextBox.Text?.Trim();
                if (string.IsNullOrEmpty(message) || message == "Escribe tu pregunta aquí...")
                {
                    return;
                }

                // Agregar mensaje del usuario al chat
                AddChatMessage($"👤 Tú: {message}", false);
                
                // Limpiar input
                _chatInputTextBox.Text = "";
                
                // Deshabilitar botón mientras procesa
                _sendButton.IsEnabled = false;
                _sendButton.Content = "⏳ Pensando...";

                // Generar respuesta con IA
                var response = await _aiService.GenerateRevitCodeAsync(message, GetCurrentContext());
                
                // Verificar si la respuesta es código o conversación
                if (IsCodeResponse(response))
                {
                    // Es código - mostrar en chat y en panel de código
                    AddChatMessage("🤖 IA: He generado el código para ti. Revísalo en el panel de abajo.", false);
                    ShowAIPreview(response);
                }
                else
                {
                    // Es conversación - mostrar solo en chat
                    AddChatMessage($"🤖 IA: {response}", false);
                    // Ocultar panel de código si estaba visible
                    _aiPreviewPanel.Visibility = System.Windows.Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                AddChatMessage($"❌ Error: {ex.Message}", true);
            }
            finally
            {
                // Rehabilitar botón
                _sendButton.IsEnabled = true;
                _sendButton.Content = "📤 Enviar";
            }
        }

        private bool IsCodeResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
                return false;
                
            // Detectar si contiene código C#
            var codeIndicators = new[]
            {
                "using ", "var ", "new ", "FilteredElementCollector", 
                "TaskDialog.Show", "Transaction", "try", "catch",
                "doc.", "uidoc.", "app.", "uiapp.",
                "{", "}", "//", "/*"
            };
            
            return codeIndicators.Any(indicator => response.Contains(indicator));
        }

        private void AddChatMessage(string message, bool isError)
        {
            var timestamp = DateTime.Now.ToString("HH:mm");
            var color = isError ? "❌" : "";
            
            _chatHistoryTextBox.Text += $"\n\n[{timestamp}] {message}";
            
            // Hacer scroll hacia abajo
            _chatHistoryTextBox.ScrollToEnd();
        }

        private StackPanel CreateLabeledInput(string labelText, WpfControl control)
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5)
            };

            var label = new Label
            {
                Content = labelText,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 2)
            };

            panel.Children.Add(label);
            panel.Children.Add(control);

            return panel;
        }

        private async void ExecuteScript()
        {
            try
            {
                _resultTextBox.Text = "⏳ Ejecutando script...";
                _executeButton.IsEnabled = false;

                var code = _codeTextBox.Text;
                if (string.IsNullOrWhiteSpace(code))
                {
                    _resultTextBox.Text = "❌ Error: El código está vacío";
                    return;
                }

                // Crear el script
                var options = ScriptOptions.Default
                    .WithReferences(typeof(Document).Assembly, typeof(UIApplication).Assembly)
                    .WithImports("Autodesk.Revit.DB", "Autodesk.Revit.UI", "System", "System.Linq", "System.Collections.Generic");

                var globals = new ScriptEditorGlobals
                {
                    doc = Application.CurrentUIApplication?.ActiveUIDocument?.Document,
                    uidoc = Application.CurrentUIApplication?.ActiveUIDocument,
                    app = Application.CurrentUIApplication?.Application,
                    uiapp = Application.CurrentUIApplication
                };

                if (globals.doc == null)
                {
                    _resultTextBox.Text = "⚠️ Warning: No active Revit document. The script will run without a document context.";
                }

                var result = await CSharpScript.EvaluateAsync(code, options, globals);
                _resultTextBox.Text = $"✅ Resultado:\n{result?.ToString() ?? "null"}";
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"❌ Error:\n{ex.Message}";
            }
            finally
            {
                _executeButton.IsEnabled = true;
            }
        }

        private void SaveScript()
        {
            try
            {
                var name = _nameTextBox.Text?.Trim();
                var description = _descriptionTextBox.Text?.Trim();
                var category = _categoryComboBox.SelectedItem?.ToString();
                var code = _codeTextBox.Text?.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("❌ Por favor ingresa un nombre para el script", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(code))
                {
                    MessageBox.Show("❌ Por favor ingresa el código del script", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrEmpty(category))
                {
                    MessageBox.Show("❌ Please select a category", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var scripts = ScriptManager.LoadAllScriptsFlat().ToList();

                if (_editingScript != null)
                {
                    // Modo edición: actualizar script existente
                    var existingScript = scripts.FirstOrDefault(s => s.Id == _editingScript.Id);
                    if (existingScript != null)
                    {
                        existingScript.Name = name;
                        existingScript.Description = description ?? "Script personalizado";
                        existingScript.Category = category;
                        existingScript.Code = code;
                        
                        MessageBox.Show($"✅ Script '{name}' updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("❌ Error: No se pudo encontrar el script original", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }
                else
                {
                    // Modo creación: agregar nuevo script
                    var newScript = new ScriptDefinition
                    {
                        Id = GenerateId(name),
                        Name = name,
                        Description = description ?? "Script personalizado",
                        Category = category,
                        Icon = "script.png",
                        Code = code,
                        ShowAsButton = true // Marcar como botón para que aparezca en la pestaña Básico
                    };

                    scripts.Add(newScript);
                    MessageBox.Show($"✅ Script '{name}' saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                ScriptManager.SaveAllScripts(scripts);

                // Notificar que se guardó un script para actualizar el panel
                _onScriptSaved?.Invoke();

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al guardar el script:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearEditor()
        {
            var result = MessageBox.Show("¿Estás seguro que quieres limpiar todo el editor?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _nameTextBox.Text = "";
                _descriptionTextBox.Text = "";
                _categoryComboBox.SelectedIndex = 0;
                _codeTextBox.Text = "// 🚀 Escribe tu código C# aquí\n";
                _resultTextBox.Text = "💡 Resultados de ejecución aparecerán aquí...";
            }
        }

        private void ShowTemplates()
        {
            try
            {
                var templatesWindow = new TemplateSelectionWindow();
                templatesWindow.Owner = this;
                templatesWindow.TemplateSelected += (template) =>
                {
                    // Llenar el editor con el template seleccionado
                    _nameTextBox.Text = template.Name;
                    _descriptionTextBox.Text = template.Description;
                    _categoryComboBox.Text = template.Category;
                    _codeTextBox.Text = template.Code;
                    
                    _resultTextBox.Text = "📋 Template cargado. Puedes modificar el código según tus necesidades.";
                };
                
                templatesWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al mostrar templates:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateId(string name)
        {
            return name.ToLower()
                      .Replace(" ", "-")
                      .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                      .Replace("ñ", "n")
                      + "-" + DateTime.Now.Ticks.ToString().Substring(8);
        }

        /// <summary>
        /// Inicializa las características de IA
        /// </summary>
        private async void InitializeAIFeatures()
        {
            try
            {
                var isAvailable = await _aiService.IsAvailableAsync();
                _aiAssistButton.IsEnabled = isAvailable;
                
                if (!isAvailable)
                {
                    _aiAssistButton.Content = "🤖 AI (No disponible)";
                    _aiAssistButton.ToolTip = "Conecta con GitHub primero para usar AI Assist";
                }
                else
                {
                    _aiAssistButton.ToolTip = "Genera código usando GitHub Copilot AI";
                }
            }
            catch
            {
                _aiAssistButton.IsEnabled = false;
                _aiAssistButton.Content = "🤖 AI (Error)";
            }
        }

        /// <summary>
        /// Muestra el diálogo de asistencia de IA
        /// </summary>
        private async void ShowAIAssistDialog()
        {
            try
            {
                var aiDialog = new AIAssistDialog();
                aiDialog.Owner = this;
                
                if (aiDialog.ShowDialog() == true)
                {
                    var prompt = aiDialog.UserPrompt;
                    var context = aiDialog.AdditionalContext;
                    
                    await GenerateCodeWithAI(prompt, context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error al abrir AI Assist:\n{ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Genera código usando IA y lo inserta en el editor
        /// </summary>
        private async Task GenerateCodeWithAI(string prompt, string context)
        {
            try
            {
                // Mostrar indicador de carga
                _aiAssistButton.Content = "🔄 Generando...";
                _aiAssistButton.IsEnabled = false;
                _resultTextBox.Text = "🤖 GitHub AI está generando tu código...";

                // Generar código con IA
                var generatedCode = await _aiService.GenerateRevitCodeAsync(prompt, context);
                
                // Insertar o reemplazar código
                if (string.IsNullOrWhiteSpace(_codeTextBox.Text) || 
                    MessageBox.Show("¿Reemplazar el código actual con el generado por IA?", 
                                   "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    _codeTextBox.Text = generatedCode;
                }
                else
                {
                    // Agregar al final del código existente
                    _codeTextBox.Text += Environment.NewLine + Environment.NewLine + 
                                        "// Código generado por AI:" + Environment.NewLine + 
                                        generatedCode;
                }

                _resultTextBox.Text = "✅ Code generated successfully by GitHub AI. Review before running!";
                
                // Auto-completar nombre si está vacío
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    _nameTextBox.Text = GenerateScriptNameFromPrompt(prompt);
                }
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"❌ Error al generar código con AI: {ex.Message}";
                MessageBox.Show($"Error con GitHub AI:\n{ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restaurar botón
                _aiAssistButton.Content = "🤖 AI Assist";
                _aiAssistButton.IsEnabled = true;
            }
        }

        /// <summary>
        /// Genera un nombre de script basado en el prompt del usuario
        /// </summary>
        private string GenerateScriptNameFromPrompt(string prompt)
        {
            var words = prompt.Split(' ');
            var relevantWords = words.Where(w => w.Length > 3 && 
                                                !new[] { "crear", "hacer", "generar", "script", "código" }.Contains(w.ToLower()))
                                    .Take(3);
            
            return string.Join(" ", relevantWords).Trim();
        }

        /// <summary>
        /// Genera código usando AI integrada directamente en el editor
        /// </summary>
        private async Task GenerateCodeWithIntegratedAI(string prompt)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(prompt))
                {
                    _resultTextBox.Text = "❌ Por favor describe lo que necesitas en el AI Assistant";
                    return;
                }

                _resultTextBox.Text = "🤖 Generando código con AI...";

                // Limpiar el texto de ejemplo si está presente
                if (prompt.Contains("Ejemplo:"))
                {
                    prompt = prompt.Split('\n')[0]; // Tomar solo la primera línea real
                }

                // Generar código con IA
                var generatedCode = await _aiService.GenerateRevitCodeAsync(prompt, GetCurrentContext());
                
                // Mostrar el código en el panel de previsualización
                ShowAIPreview(generatedCode);
                _resultTextBox.Text = "✅ Código generado. Revisa la respuesta y decide si aplicarla.";
                
                // Auto-completar nombre y descripción si están vacíos
                if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
                {
                    _nameTextBox.Text = GenerateScriptNameFromPrompt(prompt);
                }
                
                if (string.IsNullOrWhiteSpace(_descriptionTextBox.Text))
                {
                    _descriptionTextBox.Text = $"Script generado por AI: {prompt}";
                }
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"❌ Error al generar código con AI: {ex.Message}";
                MessageBox.Show($"Error con GitHub AI:\n{ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Mejora el código existente usando AI
        /// </summary>
        private async Task ImproveExistingCode(string improvementRequest)
        {
            try
            {
                var currentCode = _codeTextBox.Text?.Trim();
                if (string.IsNullOrWhiteSpace(currentCode) || currentCode.Contains("// 🚀 Escribe tu código C# aquí"))
                {
                    _resultTextBox.Text = "❌ Primero necesitas tener código para mejorar. Usa 'Generar Código' primero.";
                    return;
                }

                if (string.IsNullOrWhiteSpace(improvementRequest))
                {
                    improvementRequest = "Mejorar este código, optimizarlo y agregar comentarios explicativos";
                }

                _resultTextBox.Text = "🔧 Mejorando código con AI...";

                // Crear contexto específico para mejora
                var context = $"Código actual a mejorar:\n{currentCode}\n\nMejoras solicitadas: {improvementRequest}\n\n{GetCurrentContext()}";
                
                // Generar versión mejorada
                var improvedCode = await _aiService.GenerateRevitCodeAsync($"Mejorar y optimizar: {improvementRequest}", context);
                
                // Mostrar el código mejorado en el panel de previsualización
                ShowAIPreview(improvedCode);
                _resultTextBox.Text = "✅ Código mejorado. Revisa los cambios y decide si aplicarlos.";
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"❌ Error al mejorar código con AI: {ex.Message}";
                MessageBox.Show($"Error con GitHub AI:\n{ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AIAcceptButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_pendingAICode))
                {
                    // Preguntar si reemplazar o agregar al código existente
                    var currentCode = _codeTextBox.Text.Trim();
                    var isEmptyOrTemplate = string.IsNullOrWhiteSpace(currentCode) || 
                                           currentCode.Contains("// 🚀 Escribe tu código C# aquí") ||
                                           currentCode.Contains("// Ejemplo: Contar elementos");

                    if (isEmptyOrTemplate)
                    {
                        _codeTextBox.Text = _pendingAICode;
                    }
                    else
                    {
                        var result = MessageBox.Show("¿Cómo quieres aplicar el código?\n\nSí = Reemplazar código actual\nNo = Agregar al final", 
                                                   "Aplicar Código AI", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                        
                        if (result == MessageBoxResult.Yes)
                        {
                            _codeTextBox.Text = _pendingAICode;
                        }
                        else if (result == MessageBoxResult.No)
                        {
                            _codeTextBox.Text += "\n\n// Código generado por AI:\n" + _pendingAICode;
                        }
                        else
                        {
                            return; // Cancelado
                        }
                    }

                    _resultTextBox.Text = "✅ Código AI aplicado exitosamente";
                    HideAIPreview();
                }
            }
            catch (Exception ex)
            {
                _resultTextBox.Text = $"❌ Error al aplicar código: {ex.Message}";
            }
        }

        private void AIRejectButton_Click(object sender, RoutedEventArgs e)
        {
            _resultTextBox.Text = "🔄 Código AI descartado";
            HideAIPreview();
        }

        private void ShowAIPreview(string aiResponse)
        {
            _pendingAICode = aiResponse;
            _aiResponseTextBox.Text = aiResponse;
            _aiPreviewPanel.Visibility = System.Windows.Visibility.Visible;
        }

        private void HideAIPreview()
        {
            _pendingAICode = null;
            _aiResponseTextBox.Text = "";
            _aiPreviewPanel.Visibility = System.Windows.Visibility.Collapsed;
        }

        /// <summary>
        /// Obtiene el contexto actual para la AI
        /// </summary>
        private string GetCurrentContext()
        {
            var context = "Contexto del proyecto Revit:\n";
            context += $"- Nombre: {_nameTextBox.Text ?? "Nuevo script"}\n";
            context += $"- Category: {_categoryComboBox.Text ?? "General"}\n";
            context += $"- Descripción: {_descriptionTextBox.Text ?? "Sin descripción"}\n\n";
            
            context += "Variables disponibles en el script:\n";
            context += "- doc: Document (documento activo de Revit)\n";
            context += "- uidoc: UIDocument (documento UI de Revit)\n";
            context += "- app: Application (aplicación de Revit)\n";
            context += "- uiapp: UIApplication (aplicación UI de Revit)\n\n";
            
            context += "Usar estas variables para acceder al contexto de Revit. Ejemplos:\n";
            context += "- doc.ActiveView para obtener la vista activa\n";
            context += "- new FilteredElementCollector(doc) para coleccionar elementos\n";
            context += "- uidoc.Selection para acceder a la selección\n\n";
            
            if (!string.IsNullOrWhiteSpace(_codeTextBox.Text) && !_codeTextBox.Text.Contains("// 🚀 Escribe tu código C# aquí"))
            {
                context += $"Código existente:\n{_codeTextBox.Text}\n\n";
            }
            
            return context;
        }
    }

    /// <summary>
    /// Globals para el contexto de ejecución de scripts en ScriptEditor
    /// </summary>
    public class ScriptEditorGlobals
    {
        public Document doc { get; set; }
        public UIDocument uidoc { get; set; }
        public Autodesk.Revit.ApplicationServices.Application app { get; set; }
        public UIApplication uiapp { get; set; }
    }
}