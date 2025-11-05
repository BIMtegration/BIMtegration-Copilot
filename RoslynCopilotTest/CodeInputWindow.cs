using System;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace RoslynCopilotTest
{
    /// <summary>
    /// Ventana para ingresar y ejecutar código C# manualmente
    /// </summary>
    public partial class CodeInputWindow : Window
    {
        private ExternalCommandData _commandData;

        public CodeInputWindow(ExternalCommandData commandData)
        {
            _commandData = commandData;
            InitializeComponent();
            LoadSampleCode();
        }

        private void InitializeComponent()
        {
            // Configuración básica de la ventana
            Title = "Roslyn Code Executor - Prueba Manual";
            Width = 1000;  // Ventana más ancha
            Height = 700;  // Ventana más alta
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Layout principal con proporciones optimizadas
            var mainGrid = new System.Windows.Controls.Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) }); // Más espacio para código
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(3, GridUnitType.Star) }); // Más espacio para resultados

            // Área de código
            var codeLabel = new Label { Content = "Código C# a ejecutar:", FontWeight = FontWeights.Bold };
            System.Windows.Controls.Grid.SetRow(codeLabel, 0);

            CodeTextBox = new System.Windows.Controls.TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 12,
                Margin = new Thickness(5, 5, 5, 5)
            };
            System.Windows.Controls.Grid.SetRow(CodeTextBox, 0);

            // Botones
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(5, 5, 5, 5)
            };

            var executeButton = new Button 
            { 
                Content = "Ejecutar Código", 
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 5, 5, 5),
                IsDefault = true
            };
            executeButton.Click += ExecuteButton_Click;

            var clearButton = new Button 
            { 
                Content = "Limpiar", 
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 5, 5, 5)
            };
            clearButton.Click += (s, e) => { CodeTextBox.Text = ""; ResultTextBox.Text = ""; };

            var samplesButton = new Button 
            { 
                Content = "Código de Ejemplo", 
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(5, 5, 5, 5)
            };
            samplesButton.Click += (s, e) => LoadSampleCode();

            buttonPanel.Children.Add(executeButton);
            buttonPanel.Children.Add(clearButton);
            buttonPanel.Children.Add(samplesButton);
            System.Windows.Controls.Grid.SetRow(buttonPanel, 1);

            // Área de resultados con scroll mejorado
            var resultLabel = new Label { Content = "Resultado:", FontWeight = FontWeights.Bold };
            ResultTextBox = new System.Windows.Controls.TextBox
            {
                IsReadOnly = true,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,  // Desactivar scroll del TextBox
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,  // Desactivar scroll del TextBox
                FontFamily = new System.Windows.Media.FontFamily("Consolas"),
                FontSize = 11,
                Background = System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(0, 0, 0, 0),  // Sin margen para que el ScrollViewer controle
                TextWrapping = TextWrapping.Wrap,  // Envolver texto automáticamente
                BorderThickness = new Thickness(0)  // Sin borde para que se vea limpio
            };

            // Usar ScrollViewer para garantizar el scroll
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(5, 5, 5, 5),
                Content = ResultTextBox
            };

            var resultStack = new StackPanel();
            resultStack.Children.Add(resultLabel);
            resultStack.Children.Add(scrollViewer);
            System.Windows.Controls.Grid.SetRow(resultStack, 2);

            // Añadir todo al grid principal
            mainGrid.Children.Add(new StackPanel 
            { 
                Children = { codeLabel, CodeTextBox }
            });
            mainGrid.Children.Add(buttonPanel);
            mainGrid.Children.Add(resultStack);

            Content = mainGrid;
        }

        public System.Windows.Controls.TextBox CodeTextBox { get; private set; }
        public System.Windows.Controls.TextBox ResultTextBox { get; private set; }

        private void LoadSampleCode()
        {
            CodeTextBox.Text = @"// Ejemplo: Análisis avanzado de materiales y áreas
using System.Linq;

// Obtener todos los muros y calcular área total
var walls = new FilteredElementCollector(RevitDoc)
    .OfCategory(BuiltInCategory.OST_Walls)
    .WhereElementIsNotElementType()
    .Cast<Wall>();

double totalWallArea = 0;
string materials = """";

foreach (Wall wall in walls)
{
    Parameter areaParam = wall.get_Parameter(BuiltInParameter.HOST_AREA_COMPUTED);
    if (areaParam != null)
        totalWallArea += areaParam.AsDouble();
    
    // Obtener material principal
    ElementId materialId = wall.WallType.get_Parameter(BuiltInParameter.STRUCTURAL_MATERIAL_PARAM)?.AsElementId();
    if (materialId != null && materialId != ElementId.InvalidElementId)
    {
        Material mat = RevitDoc.GetElement(materialId) as Material;
        if (mat != null)
            materials += $""- {mat.Name}\n"";
    }
}

// Convertir de pies cuadrados a metros cuadrados
double areaInSquareMeters = totalWallArea * 0.092903;

return $""📊 ANÁLISIS DE CONSTRUCCIÓN:\n"" +
       $""==========================================\n"" +
       $""🏗️ Área total de muros: {areaInSquareMeters:F2} m²\n"" +
       $""� Área en pies²: {totalWallArea:F2} ft²\n"" +
       $""� Materiales encontrados:\n{materials}"" +
       $""� Total de muros: {walls.Count()}\n"" +
       $""⏰ Análisis completado: {DateTime.Now:HH:mm:ss}"";";
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ResultTextBox.Text = "⏳ Ejecutando código...\n";
                
                // Ejecutar el código de forma asíncrona
                var result = await ExecuteUserCode(CodeTextBox.Text);
                
                ResultTextBox.Text = $"✅ ÉXITO - Código ejecutado correctamente\n" +
                                   $"Hora: {DateTime.Now:HH:mm:ss}\n\n" +
                                   $"RESULTADO:\n{result}";
            }
            catch (Exception ex)
            {
                ResultTextBox.Text = $"❌ ERROR - No se pudo ejecutar el código\n" +
                                   $"Hora: {DateTime.Now:HH:mm:ss}\n\n" +
                                   $"DETALLE DEL ERROR:\n{ex.Message}\n\n" +
                                   $"TIPO DE ERROR: {ex.GetType().Name}";
            }
        }

        public class ScriptGlobals
        {
            public Document doc { get; set; }
            public UIDocument uidoc { get; set; }
            public UIApplication uiapp { get; set; }
            public Autodesk.Revit.ApplicationServices.Application app { get; set; }
            public object externalEventHandler { get; set; }
            public object externalEvent { get; set; }
            // ...eliminado: propiedad copilotBridge...
        }

        private async Task<object> ExecuteUserCode(string code)
        {
            // Configurar referencias
            var binPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Release", "net48");
            var webView2WinFormsPath = System.IO.Path.Combine(binPath, "Microsoft.Web.WebView2.WinForms.dll");
            var webView2CorePath = System.IO.Path.Combine(binPath, "Microsoft.Web.WebView2.Core.dll");
            var hostAssemblyPath = System.IO.Path.Combine(binPath, "CodeAssistantPro.dll");
            if (!System.IO.File.Exists(hostAssemblyPath))
            {
                MessageBox.Show($"No se encontró el assembly del host en: {hostAssemblyPath}", "Error referencia CopilotBridge");
            }
            else
            {
                MessageBox.Show($"Assembly del host referenciado: {hostAssemblyPath}", "Referencia CopilotBridge");
            }
            var scriptOptions = ScriptOptions.Default
                .AddReferences(
                    MetadataReference.CreateFromFile(typeof(Document).Assembly.Location),           // RevitAPI.dll
                    MetadataReference.CreateFromFile(typeof(UIDocument).Assembly.Location),         // RevitAPIUI.dll
                    MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),  // System.Core.dll
                    MetadataReference.CreateFromFile(typeof(System.Windows.Forms.Form).Assembly.Location), // WinForms
                    MetadataReference.CreateFromFile(webView2WinFormsPath), // WebView2 WinForms
                    MetadataReference.CreateFromFile(webView2CorePath), // WebView2 Core
                    MetadataReference.CreateFromFile(hostAssemblyPath) // Host add-in assembly (exponer tipos públicos a scripts)
                )
                .WithImports(
                    "System",
                    "System.Linq",
                    "Autodesk.Revit.DB",
                    "Autodesk.Revit.UI",
                    "System.Windows.Forms",
                    "Microsoft.Web.WebView2.WinForms",
                    "RoslynCopilotTest.Services"
                );

            // Validar e inicializar handler/evento si es necesario
            if (RoslynCopilotTest.Application.SharedExternalEventHandler == null)
                RoslynCopilotTest.Application.SharedExternalEventHandler = new GenericExternalEventHandler();
            if (RoslynCopilotTest.Application.SharedExternalEvent == null)
                RoslynCopilotTest.Application.SharedExternalEvent = ExternalEvent.Create(RoslynCopilotTest.Application.SharedExternalEventHandler);

            // Inyectar variables globales como diccionario para Roslyn
            var globals = new ScriptGlobals();
            globals.doc = _commandData?.Application?.ActiveUIDocument?.Document;
            globals.uidoc = _commandData?.Application?.ActiveUIDocument;
            globals.uiapp = _commandData?.Application;
            globals.app = _commandData?.Application?.Application;
            globals.externalEventHandler = RoslynCopilotTest.Application.SharedExternalEventHandler;
            globals.externalEvent = RoslynCopilotTest.Application.SharedExternalEvent;
            // ...eliminado: referencia a CopilotBridge...

            var state = await CSharpScript.RunAsync(code, scriptOptions, globals: globals);

            // Procesar comando devuelto por el script (pattern 1)
            var returnValue = state.ReturnValue;
            try
            {
                if (returnValue != null)
                {
                    // intentar leer propiedades dinámicamente
                    dynamic cmd = returnValue;
                    string commandName = null;
                    string path = null;

                    try { commandName = cmd.Command as string; } catch { }
                    try { path = cmd.Path as string; } catch { }

                    if (!string.IsNullOrEmpty(commandName) && !string.IsNullOrEmpty(path))
                    {
                        // Guardar selección y configurar acción segura
                        RoslynCopilotTest.Application.SharedKatalogSelectionPath = path;

                        RoslynCopilotTest.Application.SharedExternalEventHandler.ActionToExecute = (uiapp) =>
                        {
                            try
                            {
                                var uidocument = uiapp?.ActiveUIDocument;
                                var doc = uidocument?.Document;
                                var ext = System.IO.Path.GetExtension(path)?.ToLowerInvariant();

                                if (doc == null) return;

                                if (ext == ".rfa")
                                {
                                    // Intentar cargar la familia
                                    Family family = null;
                                    if (doc.LoadFamily(path, out family))
                                    {
                                        // No posicionamos automáticamente; se deja al usuario
                                    }
                                }
                                else if (ext == ".rvt")
                                {
                                    // Intentar lanzar el comando de vincular Revit
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
            catch { /* proteger host de excepciones lanzadas por scripts */ }

            return returnValue ?? "[null - el código no devolvió ningún valor]";
        }
    }
}