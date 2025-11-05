using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoslynCopilotTest.Models;

namespace RoslynCopilotTest.UI
{
    public class ImportSelectionWindow : Window
    {
        private List<ScriptDefinition> _allScripts;
        private Dictionary<ScriptDefinition, CheckBox> _checkBoxes;
        private CheckBox _selectAllCheckBox;
        
    public List<ScriptDefinition> SelectedScripts { get; private set; }

        public ImportSelectionWindow(List<ScriptDefinition> scripts, string fileName)
        {
            _allScripts = scripts;
            _checkBoxes = new Dictionary<ScriptDefinition, CheckBox>();
            SelectedScripts = new List<ScriptDefinition>();
            
            InitializeComponent(fileName);
        }

        private void InitializeComponent(string fileName)
        {
            Title = "Select Scripts to Import";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;
            
            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header
            var headerPanel = new StackPanel 
            { 
                Margin = new Thickness(10),
                Background = new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };
            
            var titleText = new TextBlock
            {
                Text = $"📁 File: {fileName}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 10, 5)
            };
            
            var countText = new TextBlock
            {
                Text = $"📊 {_allScripts.Count} scripts found",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.DimGray),
                Margin = new Thickness(10, 0, 10, 10)
            };
            
            headerPanel.Children.Add(titleText);
            headerPanel.Children.Add(countText);
            
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Select All checkbox
            _selectAllCheckBox = new CheckBox
            {
                Content = "Select all scripts",
                IsChecked = true,
                Margin = new Thickness(10, 5, 10, 5),
                FontWeight = FontWeights.Bold
            };
            _selectAllCheckBox.Checked += SelectAllCheckBox_Checked;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Unchecked;
            
            Grid.SetRow(_selectAllCheckBox, 1);

            // Scripts list
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10, 0, 10, 10)
            };
            
            var scriptsPanel = new StackPanel();
            
            foreach (var script in _allScripts)
            {
                var checkBox = new CheckBox
                {
                    IsChecked = true,
                    Margin = new Thickness(5, 5, 5, 5)
                };
                
                var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                
                var nameText = new TextBlock
                {
                    Text = $"📝 {script.Name}",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 2)
                };
                
                var detailsText = new TextBlock
                {
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Colors.DimGray),
                    TextWrapping = TextWrapping.Wrap
                };
                
                var details = new List<string>();
                if (!string.IsNullOrEmpty(script.Category))
                    details.Add($"🏷️ {script.Category}");
                if (!string.IsNullOrEmpty(script.Description))
                    details.Add($"📄 {script.Description}");
                else
                    details.Add("📄 No description");
                
                detailsText.Text = string.Join(" • ", details);
                
                stackPanel.Children.Add(nameText);
                stackPanel.Children.Add(detailsText);
                
                checkBox.Content = stackPanel;
                checkBox.Checked += ScriptCheckBox_Changed;
                checkBox.Unchecked += ScriptCheckBox_Changed;
                
                _checkBoxes[script] = checkBox;
                scriptsPanel.Children.Add(checkBox);
                
                // Separator
                if (script != _allScripts.Last())
                {
                    var separator = new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                        Margin = new Thickness(0, 5, 0, 5)
                    };
                    scriptsPanel.Children.Add(separator);
                }
            }
            
            scrollViewer.Content = scriptsPanel;
            Grid.SetRow(scrollViewer, 1);
            mainGrid.Children.Add(scrollViewer);

            // Selection info
            var infoText = new TextBlock
            {
                Name = "InfoText",
                Text = $"✅ {_allScripts.Count} scripts selected",
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.Green),
                Margin = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            Grid.SetRow(infoText, 2);
            mainGrid.Children.Add(infoText);

            // Buttons
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };
            
            var importButton = new Button
            {
                Content = "📥 Import Selected",
                Width = 150,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.Bold
            };
            importButton.Click += ImportButton_Click;
            
            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0)
            };
            cancelButton.Click += CancelButton_Click;
            
            buttonPanel.Children.Add(importButton);
            buttonPanel.Children.Add(cancelButton);
            
            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
            
            // Update selection count
            UpdateSelectionInfo();
        }

        private void SelectAllCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _checkBoxes.Values)
            {
                checkBox.Checked -= ScriptCheckBox_Changed;
                checkBox.IsChecked = true;
                checkBox.Checked += ScriptCheckBox_Changed;
            }
            UpdateSelectionInfo();
        }

        private void SelectAllCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var checkBox in _checkBoxes.Values)
            {
                checkBox.Unchecked -= ScriptCheckBox_Changed;
                checkBox.IsChecked = false;
                checkBox.Unchecked += ScriptCheckBox_Changed;
            }
            UpdateSelectionInfo();
        }

        private void ScriptCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSelectAllState();
            UpdateSelectionInfo();
        }

        private void UpdateSelectAllState()
        {
            var checkedCount = _checkBoxes.Values.Count(cb => cb.IsChecked == true);
            var totalCount = _checkBoxes.Count;

            _selectAllCheckBox.Checked -= SelectAllCheckBox_Checked;
            _selectAllCheckBox.Unchecked -= SelectAllCheckBox_Unchecked;

            if (checkedCount == 0)
                _selectAllCheckBox.IsChecked = false;
            else if (checkedCount == totalCount)
                _selectAllCheckBox.IsChecked = true;
            else
                _selectAllCheckBox.IsChecked = null; // Indeterminate state

            _selectAllCheckBox.Checked += SelectAllCheckBox_Checked;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Unchecked;
        }

        private void UpdateSelectionInfo()
        {
            var selectedCount = _checkBoxes.Values.Count(cb => cb.IsChecked == true);
            var infoText = FindName("InfoText") as TextBlock;
            
            if (infoText != null)
            {
                if (selectedCount == 0)
                {
                    infoText.Text = "⚠️ No script selected";
                    infoText.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    infoText.Text = $"✅ {selectedCount} script(s) selected";
                    infoText.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedScripts = _checkBoxes
                .Where(kvp => kvp.Value.IsChecked == true)
                .Select(kvp => kvp.Key)
                .ToList();

            if (SelectedScripts.Count == 0)
            {
                MessageBox.Show("You must select at least one script to import.",
                               "Selection Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // No debug popups

            // Establecer el resultado del diálogo en la propiedad de Window para que ShowDialog() devuelva true
            base.DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            // Asegurar que ShowDialog() devuelva false
            base.DialogResult = false;
            Close();
        }
    }
}