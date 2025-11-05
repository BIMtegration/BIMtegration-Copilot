using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

// Alias para evitar conflictos
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using MediaColor = System.Windows.Media.Color;

namespace RoslynCopilotTest.UI
{
    public class AIAssistDialog : Window
    {
        private WpfTextBox _promptTextBox;
        private WpfTextBox _contextTextBox;
        private WpfButton _generateButton;
        private WpfButton _cancelButton;

        public string UserPrompt { get; private set; }
        public string AdditionalContext { get; private set; }

        public AIAssistDialog()
        {
            InitializeWindow();
            CreateControls();
            LayoutControls();
        }

        private void InitializeWindow()
        {
            Title = "🤖 GitHub AI Assistant - Revit Script Generator";
            Width = 600;
            Height = 500;
            MinWidth = 500;
            MinHeight = 400;
            
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 48));
            Foreground = Brushes.White;
        }

        private void CreateControls()
        {
            // Prompt field
            _promptTextBox = new WpfTextBox
            {
                Height = 80,
                FontSize = 12,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255)), // Fondo blanco
                Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 0, 0)), // Texto negro
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(122, 122, 122)), // Borde gris
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Additional context field
            _contextTextBox = new WpfTextBox
            {
                Height = 60,
                FontSize = 11,
                Margin = new Thickness(10),
                Background = new SolidColorBrush(MediaColor.FromRgb(255, 255, 255)), // Fondo blanco
                Foreground = new SolidColorBrush(MediaColor.FromRgb(0, 0, 0)), // Texto negro
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(122, 122, 122)), // Borde gris
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            // Removed templates ComboBox to simplify the UI

            // Buttons
            _generateButton = new WpfButton
            {
                Content = "🚀 Generate Code",
                Height = 40,
                Width = 150,
                Margin = new Thickness(10),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.MediumPurple),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            _cancelButton = new WpfButton
            {
                Content = "❌ Cancel",
                Height = 40,
                Width = 100,
                Margin = new Thickness(10),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.Gray),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            // Eventos
            _generateButton.Click += GenerateButton_Click;
            _cancelButton.Click += (s, e) => { DialogResult = false; Close(); };
        }



        private void LayoutControls()
        {
            var mainGrid = new WpfGrid();
            
            // Define rows (without templates ComboBox)
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Título
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Label prompt
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Prompt
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Label contexto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(0.7, GridUnitType.Star) }); // Contexto
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            // Title
            var titleLabel = new Label
            {
                Content = "🤖 Describe what you want your Revit script to do:",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.MediumPurple),
                Margin = new Thickness(10, 10, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WpfGrid.SetRow(titleLabel, 0);
            mainGrid.Children.Add(titleLabel);

            // Prompt label
            var promptLabel = new Label
            {
                Content = "💭 Your request (be specific):",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 5, 10, 0)
            };
            WpfGrid.SetRow(promptLabel, 1);
            mainGrid.Children.Add(promptLabel);

            // TextBox prompt
            WpfGrid.SetRow(_promptTextBox, 2);
            mainGrid.Children.Add(_promptTextBox);

            // Context label
            var contextLabel = new Label
            {
                Content = "📝 Additional context (optional):",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 5, 10, 0)
            };
            WpfGrid.SetRow(contextLabel, 3);
            mainGrid.Children.Add(contextLabel);

            // TextBox contexto
            WpfGrid.SetRow(_contextTextBox, 4);
            mainGrid.Children.Add(_contextTextBox);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            buttonPanel.Children.Add(_generateButton);
            buttonPanel.Children.Add(_cancelButton);

            WpfGrid.SetRow(buttonPanel, 5);
            mainGrid.Children.Add(buttonPanel);

            // Extra info
            var infoLabel = new Label
            {
                Content = "💡 Tip: Be specific about elements, parameters, or actions you need",
                FontSize = 10,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(10, 80, 10, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WpfGrid.SetRow(infoLabel, 5);
            mainGrid.Children.Add(infoLabel);

            Content = mainGrid;
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_promptTextBox.Text))
            {
                MessageBox.Show("Please describe what you want the script to do.", 
                              "Prompt required", MessageBoxButton.OK, MessageBoxImage.Information);
                _promptTextBox.Focus();
                return;
            }

            UserPrompt = _promptTextBox.Text.Trim();
            AdditionalContext = _contextTextBox.Text.Trim();
            
            DialogResult = true;
            Close();
        }
    }
}