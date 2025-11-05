using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfCheckBox = System.Windows.Controls.CheckBox;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfColor = System.Windows.Media.Color;

namespace RoslynCopilotTest.UI
{
    /// <summary>
    /// Ventana para seleccionar scripts específicos para exportar
    /// </summary>
    public class ExportSelectionWindow : Window
    {
        private List<ScriptDefinition> _allScripts;
        private List<ScriptItemViewModel> _scriptItems;
        private StackPanel _scriptsContainer;
        private WpfComboBox _categoryFilter;
        private WpfTextBox _searchBox;
        private WpfCheckBox _selectAllCheckBox;
        private Button _exportButton;
        private Button _cancelButton;

        public List<ScriptDefinition> SelectedScripts { get; private set; }
        public bool DialogResult { get; private set; }

        public ExportSelectionWindow()
        {
            InitializeWindow();
            LoadScripts();
        }

        /// <summary>
        /// Configura la ventana principal
        /// </summary>
        private void InitializeWindow()
        {
            Title = "Select Scripts to Export";
            Width = 600;
            Height = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;
            Background = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Filters
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Lista
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Header
            var headerPanel = CreateHeader();
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Panel de filtros
            var filtersPanel = CreateFiltersPanel();
            Grid.SetRow(filtersPanel, 1);
            mainGrid.Children.Add(filtersPanel);

            // Lista de scripts con scroll
            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = Brushes.White,
                Margin = new Thickness(10, 5, 10, 5)
            };

            _scriptsContainer = new StackPanel();
            scrollViewer.Content = _scriptsContainer;
            Grid.SetRow(scrollViewer, 2);
            mainGrid.Children.Add(scrollViewer);

            // Panel de botones
            var buttonsPanel = CreateButtonsPanel();
            Grid.SetRow(buttonsPanel, 3);
            mainGrid.Children.Add(buttonsPanel);

            Content = mainGrid;
        }

        /// <summary>
        /// Crea el panel de header
        /// </summary>
        private Border CreateHeader()
        {
            var header = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                Padding = new Thickness(15, 12, 15, 12)
            };

            var headerStack = new StackPanel();

            var titleText = new TextBlock
                {
                    Text = "📤 Export Scripts",
                Foreground = Brushes.White,
                FontSize = 16,
                FontWeight = FontWeights.Bold
            };

            var subtitleText = new TextBlock
                {
                    Text = "Select the scripts you want to export to a JSON file",
                Foreground = new SolidColorBrush(WpfColor.FromRgb(204, 204, 204)),
                FontSize = 11,
                Margin = new Thickness(0, 2, 0, 0)
            };

            headerStack.Children.Add(titleText);
            headerStack.Children.Add(subtitleText);
            header.Child = headerStack;

            return header;
        }

        /// <summary>
        /// Crea el panel de filtros
        /// </summary>
        private Border CreateFiltersPanel()
        {
            var filtersPanel = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Filtro por categoría
            var categoryLabel = new TextBlock
                {
                    Text = "📂 Category:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _categoryFilter = new WpfComboBox
            {
                Margin = new Thickness(0, 0, 10, 0)
            };
            _categoryFilter.SelectionChanged += CategoryFilter_SelectionChanged;

            var categoryStack = new StackPanel();
            categoryStack.Children.Add(categoryLabel);
            categoryStack.Children.Add(_categoryFilter);
            Grid.SetColumn(categoryStack, 0);
            grid.Children.Add(categoryStack);

            // Búsqueda por texto
            var searchLabel = new TextBlock
                {
                    Text = "🔍 Search:",
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 0, 5)
            };

            _searchBox = new WpfTextBox
            {
                Margin = new Thickness(0, 0, 10, 0)
            };
            _searchBox.TextChanged += SearchBox_TextChanged;

            var searchStack = new StackPanel();
            searchStack.Children.Add(searchLabel);
            searchStack.Children.Add(_searchBox);
            Grid.SetColumn(searchStack, 1);
            grid.Children.Add(searchStack);

            // Seleccionar todos
            _selectAllCheckBox = new WpfCheckBox
            {
                    Content = "✅ Select all",
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(10, 0, 0, 0)
            };
            _selectAllCheckBox.Checked += SelectAllCheckBox_Changed;
            _selectAllCheckBox.Unchecked += SelectAllCheckBox_Changed;
            Grid.SetColumn(_selectAllCheckBox, 2);
            grid.Children.Add(_selectAllCheckBox);

            filtersPanel.Child = grid;
            return filtersPanel;
        }

        /// <summary>
        /// Crea el panel de botones
        /// </summary>
        private Border CreateButtonsPanel()
        {
            var buttonsPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(248, 248, 248)),
                BorderThickness = new Thickness(0, 1, 0, 0),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                Padding = new Thickness(15, 10, 15, 10)
            };

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            _cancelButton = new Button
                {
                    Content = "❌ Cancel",
                Width = 120,
                Height = 32,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(169, 169, 169))
            };
            _cancelButton.Click += CancelButton_Click;

            _exportButton = new Button
            {
                    Content = "📤 Export Selected",
                Width = 180,
                Height = 32,
                Background = new SolidColorBrush(WpfColor.FromRgb(0, 123, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(0, 86, 179)),
                IsEnabled = false
            };
            _exportButton.Click += ExportButton_Click;

            buttonStack.Children.Add(_cancelButton);
            buttonStack.Children.Add(_exportButton);
            buttonsPanel.Child = buttonStack;

            return buttonsPanel;
        }

        /// <summary>
        /// Carga todos los scripts disponibles
        /// </summary>
        private void LoadScripts()
        {
            try
            {
                _allScripts = ScriptManager.LoadAllScripts();
                _scriptItems = _allScripts.Select(script => new ScriptItemViewModel(script)).ToList();

                // Poblar filtro de categorías
                PopulateCategoryFilter();

                // Mostrar todos los scripts inicialmente
                DisplayScripts(_scriptItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar scripts: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Puebla el filtro de categorías
        /// </summary>
        private void PopulateCategoryFilter()
        {
            var categories = new List<string> { "📋 All categories" };
            categories.AddRange(_allScripts.Select(s => s.Category).Distinct().OrderBy(c => c));

            _categoryFilter.ItemsSource = categories;
            _categoryFilter.SelectedIndex = 0;
        }

        /// <summary>
        /// Muestra los scripts filtrados
        /// </summary>
        private void DisplayScripts(List<ScriptItemViewModel> scripts)
        {
            _scriptsContainer.Children.Clear();

            if (!scripts.Any())
            {
                var noResultsText = new TextBlock
                {
                    Text = "📭 No scripts found with the applied filters",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(20),
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(WpfColor.FromRgb(128, 128, 128))
                };
                _scriptsContainer.Children.Add(noResultsText);
                return;
            }

            foreach (var scriptItem in scripts)
            {
                var scriptPanel = CreateScriptPanel(scriptItem);
                _scriptsContainer.Children.Add(scriptPanel);
            }

            UpdateExportButton();
        }

        /// <summary>
        /// Crea el panel para un script individual
        /// </summary>
        private Border CreateScriptPanel(ScriptItemViewModel scriptItem)
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(224, 224, 224)),
                Margin = new Thickness(0, 2, 0, 2),
                Padding = new Thickness(12, 8, 12, 8)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Checkbox
            scriptItem.CheckBox = new WpfCheckBox
            {
                IsChecked = false,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, 2, 10, 0)
            };
            scriptItem.CheckBox.Checked += ScriptCheckBox_Changed;
            scriptItem.CheckBox.Unchecked += ScriptCheckBox_Changed;
            Grid.SetColumn(scriptItem.CheckBox, 0);
            grid.Children.Add(scriptItem.CheckBox);

            // Información del script
            var infoStack = new StackPanel();

            var nameText = new TextBlock
            {
                Text = $"📜 {scriptItem.Script.Name}",
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 2)
            };

            var categoryText = new TextBlock
            {
                Text = $"📂 {scriptItem.Script.Category}",
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(108, 117, 125)),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var descriptionText = new TextBlock
            {
                Text = scriptItem.Script.Description,
                FontSize = 12,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(73, 80, 87)),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 450
            };

            infoStack.Children.Add(nameText);
            infoStack.Children.Add(categoryText);
            infoStack.Children.Add(descriptionText);

            Grid.SetColumn(infoStack, 1);
            grid.Children.Add(infoStack);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// Actualiza el estado del botón de exportar
        /// </summary>
        private void UpdateExportButton()
        {
            var selectedCount = _scriptItems.Count(si => si.CheckBox?.IsChecked == true);
            _exportButton.IsEnabled = selectedCount > 0;
            _exportButton.Content = selectedCount > 0 
                ? $"📤 Export {selectedCount} Script{(selectedCount != 1 ? "s" : "" )}"
                : "📤 Export Selected";
        }

        /// <summary>
        /// Filtra los scripts según los criterios seleccionados
        /// </summary>
        private void FilterScripts()
        {
            var filteredScripts = _scriptItems.AsEnumerable();

            // Filtrar por categoría
            if (_categoryFilter.SelectedIndex > 0)
            {
                var selectedCategory = _categoryFilter.SelectedItem.ToString().Substring(2); // Remover emoji
                filteredScripts = filteredScripts.Where(si => si.Script.Category == selectedCategory);
            }

            // Filtrar por texto de búsqueda
            if (!string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                var searchText = _searchBox.Text.ToLower();
                filteredScripts = filteredScripts.Where(si =>
                    si.Script.Name.ToLower().Contains(searchText) ||
                    si.Script.Description.ToLower().Contains(searchText) ||
                    si.Script.Category.ToLower().Contains(searchText));
            }

            DisplayScripts(filteredScripts.ToList());
        }

        #region Event Handlers

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilterScripts();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterScripts();
        }

        private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var isChecked = _selectAllCheckBox.IsChecked == true;
            
            foreach (var child in _scriptsContainer.Children)
            {
                if (child is Border border && border.Child is Grid grid)
                {
                    var checkBox = grid.Children.OfType<WpfCheckBox>().FirstOrDefault();
                    if (checkBox != null)
                    {
                        checkBox.IsChecked = isChecked;
                    }
                }
            }
        }

        private void ScriptCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            UpdateExportButton();
            
            // Actualizar estado del checkbox "Seleccionar todos"
            var visibleCheckBoxes = _scriptsContainer.Children
                .OfType<Border>()
                .Select(b => b.Child as Grid)
                .Where(g => g != null)
                .SelectMany(g => g.Children.OfType<WpfCheckBox>())
                .ToList();

            var checkedCount = visibleCheckBoxes.Count(cb => cb.IsChecked == true);
            
            if (checkedCount == 0)
            {
                _selectAllCheckBox.IsChecked = false;
            }
            else if (checkedCount == visibleCheckBoxes.Count)
            {
                _selectAllCheckBox.IsChecked = true;
            }
            else
            {
                _selectAllCheckBox.IsChecked = null; // Estado indeterminado
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedScripts = _scriptItems
                .Where(si => si.CheckBox?.IsChecked == true)
                .Select(si => si.Script)
                .ToList();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        /// <summary>
        /// ViewModel para representar un script con su checkbox
        /// </summary>
        private class ScriptItemViewModel
        {
            public ScriptDefinition Script { get; set; }
            public WpfCheckBox CheckBox { get; set; }

            public ScriptItemViewModel(ScriptDefinition script)
            {
                Script = script;
            }
        }
    }
}