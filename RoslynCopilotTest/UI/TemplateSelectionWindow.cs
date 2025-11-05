using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoslynCopilotTest.Models;

// Alias para evitar conflictos de namespace
using WpfListBox = System.Windows.Controls.ListBox;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBlock = System.Windows.Controls.TextBlock;
using WpfGrid = System.Windows.Controls.Grid;
using MediaColor = System.Windows.Media.Color;

namespace RoslynCopilotTest.UI
{
    public class TemplateSelectionWindow : Window
    {
        private WpfListBox _templatesListBox;
        private WpfButton _selectButton;
        private WpfButton _cancelButton;
        private WpfTextBlock _descriptionTextBlock;

        public event Action<ScriptTemplate> TemplateSelected;

        public TemplateSelectionWindow()
        {
            InitializeWindow();
            CreateControls();
            LayoutControls();
            LoadTemplates();
        }

        private void InitializeWindow()
        {
            Title = "📋 Select Template - Roslyn Copilot";
            Width = 700;
            Height = 500;
            MinWidth = 600;
            MinHeight = 400;
            
            // Centrar en pantalla
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            // Estilo
            Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 48));
            Foreground = Brushes.White;
        }

        private void CreateControls()
        {
            // Lista de templates
            _templatesListBox = new WpfListBox
            {
                Margin = new Thickness(10),
                Background = new SolidColorBrush(MediaColor.FromRgb(60, 60, 60)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                FontSize = 14
            };

            _templatesListBox.SelectionChanged += OnTemplateSelectionChanged;

            // Selected template description
            _descriptionTextBlock = new WpfTextBlock
            {
                Margin = new Thickness(10),
                Background = new SolidColorBrush(MediaColor.FromRgb(30, 30, 30)),
                Foreground = new SolidColorBrush(MediaColor.FromRgb(200, 200, 200)),
                Padding = new Thickness(10),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 12,
                Text = "💡 Select a template to see its description and available variables."
            };

            // Buttons
            _selectButton = new WpfButton
            {
                Content = "✅ Use Template",
                Height = 35,
                Width = 150,
                Margin = new Thickness(5),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.Green),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsEnabled = false
            };

            _cancelButton = new WpfButton
            {
                Content = "❌ Cancel",
                Height = 35,
                Width = 100,
                Margin = new Thickness(5),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Background = new SolidColorBrush(Colors.Gray),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            _selectButton.Click += OnSelectButtonClick;
            _cancelButton.Click += OnCancelButtonClick;
        }

        private void LayoutControls()
        {
            var mainGrid = new WpfGrid();
            
            // Definir filas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(2, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Title
            var titleLabel = new Label
            {
                Content = "📋 Choose a Template for your Script",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            WpfGrid.SetRow(titleLabel, 0);
            mainGrid.Children.Add(titleLabel);

            // Template list
            var templatesLabel = new Label
            {
                Content = "🗂️ Available Templates:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 5, 10, 0)
            };
            
            var templatesPanel = new StackPanel();
            templatesPanel.Children.Add(templatesLabel);
            templatesPanel.Children.Add(_templatesListBox);
            
            WpfGrid.SetRow(templatesPanel, 1);
            mainGrid.Children.Add(templatesPanel);

            // Description
            var descriptionLabel = new Label
            {
                Content = "📄 Description and Variables:",
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(10, 5, 10, 0)
            };
            
            var descriptionPanel = new StackPanel();
            descriptionPanel.Children.Add(descriptionLabel);
            descriptionPanel.Children.Add(_descriptionTextBlock);
            
            WpfGrid.SetRow(descriptionPanel, 2);
            mainGrid.Children.Add(descriptionPanel);

            // Botones
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            buttonPanel.Children.Add(_selectButton);
            buttonPanel.Children.Add(_cancelButton);

            WpfGrid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            this.Content = mainGrid;
        }

        private void LoadTemplates()
        {
            var templates = ScriptTemplates.GetAvailableTemplates();
            
            // Agrupar por categoría
            var groupedTemplates = templates.GroupBy(t => t.Category).OrderBy(g => g.Key);
            
            foreach (var group in groupedTemplates)
            {
                // Agregar separador de categoría
                var categoryItem = new ListBoxItem
                {
                    Content = $"📂 {group.Key}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    Background = new SolidColorBrush(MediaColor.FromRgb(80, 80, 80)),
                    Foreground = new SolidColorBrush(MediaColor.FromRgb(255, 215, 0)),
                    IsEnabled = false,
                    Margin = new Thickness(0, 5, 0, 2)
                };
                _templatesListBox.Items.Add(categoryItem);

                // Agregar templates de la categoría
                foreach (var template in group.OrderBy(t => t.Name))
                {
                    var templateItem = new ListBoxItem
                    {
                        Content = $"  📋 {template.Name}",
                        Tag = template,
                        FontSize = 12,
                        Padding = new Thickness(10, 5, 5, 5),
                        Margin = new Thickness(0, 1, 0, 1)
                    };
                    
                    _templatesListBox.Items.Add(templateItem);
                }
            }
        }

        private void OnTemplateSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = _templatesListBox.SelectedItem as ListBoxItem;
            if (selectedItem?.Tag is ScriptTemplate template)
            {
                _selectButton.IsEnabled = true;
                
                // Mostrar descripción y variables
                string description = $"📋 {template.Name}\n\n";
                description += $"📄 {template.Description}\n\n";
                
                if (template.Variables.Any())
                {
                    description += "🔧 Customizable variables:\n";
                    foreach (var variable in template.Variables)
                    {
                        description += $"  • {variable}\n";
                    }
                    description += "\n💡 You can modify these variables in the code after loading the template.";
                }
                else
                {
                    description += "✨ This template is ready to use without modifications.";
                }

                _descriptionTextBlock.Text = description;
            }
            else
            {
                _selectButton.IsEnabled = false;
                _descriptionTextBlock.Text = "💡 Select a template to see its description and available variables.";
            }
        }

        private void OnSelectButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedItem = _templatesListBox.SelectedItem as ListBoxItem;
            if (selectedItem?.Tag is ScriptTemplate template)
            {
                TemplateSelected?.Invoke(template);
                this.Close();
            }
        }

        private void OnCancelButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}