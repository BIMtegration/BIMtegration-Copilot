using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;

namespace RoslynCopilotTest.UI
{
    /// <summary>
    /// Ventana para gestionar botones: agregar, remover, exportar e importar scripts
    /// </summary>
    public class ButtonManagerWindow : Window
    {
        private List<ScriptDefinition> _allScripts;
        private List<ScriptDefinition> _currentButtonScripts;
        private ListBox _availableScriptsListBox;
    private ListBox _currentButtonsListBox;
    private TextBox _currentButtonsSearchBox;
    private string _currentButtonsSearch = string.Empty;
        private Button _addToButtonsButton;
        private Button _removeFromButtonsButton;
        private Button _exportButton;
        private Button _importButton;
        private Button _clearAllButtonsButton;
        private TextBox _searchBox;
        private ComboBox _categoryFilterBox;

        public event EventHandler ButtonsUpdated;        public ButtonManagerWindow()
        {
            InitializeWindow();
            LoadData();
        }

        private void InitializeWindow()
        {
            Title = "Button Manager";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.CanResize;

            var mainGrid = new Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(50) });

            // Header panel with title
            var headerPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                Margin = new Thickness(10),
                VerticalAlignment = VerticalAlignment.Center
            };

            var titleBlock = new TextBlock
            {
                Text = "🎯 Manage Quick Access Buttons",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0)
            };

            headerPanel.Children.Add(titleBlock);
            Grid.SetColumn(headerPanel, 0);
            Grid.SetColumnSpan(headerPanel, 3);
            Grid.SetRow(headerPanel, 0);

            // Panel izquierdo - Scripts disponibles
            var leftPanel = CreateAvailableScriptsPanel();
            Grid.SetColumn(leftPanel, 0);
            Grid.SetRow(leftPanel, 1);

            // Panel central - Botones de acción
            var centerPanel = CreateCenterPanel();
            Grid.SetColumn(centerPanel, 1);
            Grid.SetRow(centerPanel, 1);

            // Panel derecho - Botones actuales
            var rightPanel = CreateCurrentButtonsPanel();
            Grid.SetColumn(rightPanel, 2);
            Grid.SetRow(rightPanel, 1);

            // Panel inferior - Botones de gestión
            var bottomPanel = CreateBottomPanel();
            Grid.SetColumn(bottomPanel, 0);
            Grid.SetColumnSpan(bottomPanel, 3);
            Grid.SetRow(bottomPanel, 2);

            mainGrid.Children.Add(headerPanel);
            mainGrid.Children.Add(leftPanel);
            mainGrid.Children.Add(centerPanel);
            mainGrid.Children.Add(rightPanel);
            mainGrid.Children.Add(bottomPanel);

            Content = mainGrid;
        }

        private StackPanel CreateAvailableScriptsPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(10, 0, 5, 0) };

            var titleBlock = new TextBlock
            {
                Text = "📋 Available Scripts",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(titleBlock);

            // Filters
            var filtersPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            // Search
            var searchLabel = new TextBlock { Text = "🔍 Search:", Margin = new Thickness(0, 0, 0, 2) };
            _searchBox = new TextBox { Margin = new Thickness(0, 0, 0, 5) };
            _searchBox.TextChanged += SearchBox_TextChanged;
            filtersPanel.Children.Add(searchLabel);
            filtersPanel.Children.Add(_searchBox);

            // Category filter
            var categoryLabel = new TextBlock { Text = "📂 Category:", Margin = new Thickness(0, 0, 0, 2) };
            _categoryFilterBox = new ComboBox { Margin = new Thickness(0, 0, 0, 5) };
            _categoryFilterBox.SelectionChanged += CategoryFilter_SelectionChanged;
            filtersPanel.Children.Add(categoryLabel);
            filtersPanel.Children.Add(_categoryFilterBox);

            panel.Children.Add(filtersPanel);

            // Lista de scripts disponibles
            // Available scripts list
            _availableScriptsListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                Height = 350
            };
            _availableScriptsListBox.SelectionChanged += AvailableScripts_SelectionChanged;

            var scrollViewer = new ScrollViewer
            {
                Content = _availableScriptsListBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            panel.Children.Add(scrollViewer);

            return panel;
        }

        private StackPanel CreateCenterPanel()
        {
            var panel = new StackPanel 
            { 
                Margin = new Thickness(5),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            _addToButtonsButton = new Button
            {
                Content = "➡️\nAdd",
                Width = 80,
                Height = 60,
                Margin = new Thickness(0, 10, 0, 10),
                IsEnabled = false
            };
            _addToButtonsButton.Click += AddToButtons_Click;

            _removeFromButtonsButton = new Button
            {
                Content = "⬅️\nRemove",
                Width = 80,
                Height = 60,
                Margin = new Thickness(0, 10, 0, 10),
                IsEnabled = false
            };
            _removeFromButtonsButton.Click += RemoveFromButtons_Click;

            panel.Children.Add(_addToButtonsButton);
            panel.Children.Add(_removeFromButtonsButton);

            return panel;
        }

        private StackPanel CreateCurrentButtonsPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(5, 0, 10, 0) };

            var titleBlock = new TextBlock
            {
                Text = "🎯 Current Buttons",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(titleBlock);

            // Search for current buttons list
            var searchLabel = new TextBlock { Text = "🔍 Search in buttons:", Margin = new Thickness(0, 0, 0, 2) };
            _currentButtonsSearchBox = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
            _currentButtonsSearchBox.TextChanged += (s, e) => {
                _currentButtonsSearch = _currentButtonsSearchBox.Text ?? string.Empty;
                RefreshLists();
            };
            panel.Children.Add(searchLabel);
            panel.Children.Add(_currentButtonsSearchBox);

            var infoBlock = new TextBlock
            {
                Text = "These scripts will appear as buttons in the 'Basic' tab",
                FontSize = 11,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(infoBlock);

            _currentButtonsListBox = new ListBox
            {
                SelectionMode = SelectionMode.Multiple,
                Height = 350
            };
            _currentButtonsListBox.SelectionChanged += CurrentButtons_SelectionChanged;

            var scrollViewer = new ScrollViewer
            {
                Content = _currentButtonsListBox,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };

            panel.Children.Add(scrollViewer);

            return panel;
        }

        private StackPanel CreateBottomPanel()
        {
            var panel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10)
            };

            _exportButton = new Button
            {
                Content = "📤 Export Scripts",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0)
            };
            _exportButton.Click += Export_Click;

            _importButton = new Button
            {
                Content = "📥 Import Scripts",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0)
            };
            _importButton.Click += Import_Click;

            _clearAllButtonsButton = new Button
            {
                Content = "🧹 Remove all buttons",
                Width = 180,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(232, 17, 35)),
                Foreground = Brushes.White
            };
            _clearAllButtonsButton.Click += ClearAllButtons_Click;

            var saveButton = new Button
            {
                Content = "💾 Save Changes",
                Width = 120,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)),
                Foreground = Brushes.White
            };
            saveButton.Click += Save_Click;

            var cancelButton = new Button
            {
                Content = "❌ Cancel",
                Width = 100,
                Height = 30,
                Margin = new Thickness(5, 0, 5, 0)
            };
            cancelButton.Click += Cancel_Click;

            panel.Children.Add(_exportButton);
            panel.Children.Add(_importButton);
            panel.Children.Add(_clearAllButtonsButton);
            panel.Children.Add(saveButton);
            panel.Children.Add(cancelButton);

            return panel;
        }

        private void LoadData()
        {
            // Load all scripts
            _allScripts = ScriptManager.LoadAllScripts();
            
            // Load scripts currently marked as buttons
            _currentButtonScripts = _allScripts.Where(s => s.ShowAsButton).ToList();

            // Load categories in filter
            var categories = _allScripts.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
            _categoryFilterBox.Items.Clear();
            _categoryFilterBox.Items.Add("🔄 All categories");
            _categoryFilterBox.Items.Add("🔄 All categories");
            foreach (var category in categories)
            {
                _categoryFilterBox.Items.Add(category);
            }
            _categoryFilterBox.SelectedIndex = 0;

            RefreshLists();
        }

        private void RefreshLists()
        {
            // Filtrar scripts disponibles (excluir los que ya son botones)
            // Filter available scripts (exclude already-buttons)
            var availableScripts = _allScripts.Where(s => !_currentButtonScripts.Any(b => b.Id == s.Id)).ToList();

            // Aplicar filtros
            // Apply filters
            var filteredScripts = ApplyFilters(availableScripts);

            // Actualizar lista de disponibles
            // Update available list
            _availableScriptsListBox.Items.Clear();
            foreach (var script in filteredScripts)
            {
                var item = new ListBoxItem
                {
                    Content = $"📄 {script.Name} ({script.Category})",
                    Tag = script
                };
                _availableScriptsListBox.Items.Add(item);
            }

            // Actualizar lista de botones actuales
            _currentButtonsListBox.Items.Clear();
            // Update current buttons list
            // Filter current buttons list by search box
            IEnumerable<ScriptDefinition> currentFiltered = _currentButtonScripts;
            if (!string.IsNullOrWhiteSpace(_currentButtonsSearch))
            {
                var term = _currentButtonsSearch.ToLowerInvariant();
                currentFiltered = currentFiltered.Where(s =>
                    (s.Name ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (s.Description ?? string.Empty).ToLowerInvariant().Contains(term) ||
                    (s.Category ?? string.Empty).ToLowerInvariant().Contains(term)
                );
            }

            foreach (var script in currentFiltered)
            {
                var item = new ListBoxItem
                {
                    Content = $"🎯 {script.Name} ({script.Category})",
                    Tag = script
                };
                _currentButtonsListBox.Items.Add(item);
            }
        }

        private List<ScriptDefinition> ApplyFilters(List<ScriptDefinition> scripts)
        {
            var filtered = scripts.AsEnumerable();

            // Filtro de búsqueda
            // Search filter
            // Category filter
            if (!string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                var searchTerm = _searchBox.Text.ToLower();
                filtered = filtered.Where(s => 
                    s.Name.ToLower().Contains(searchTerm) ||
                    s.Description.ToLower().Contains(searchTerm) ||
                    s.Category.ToLower().Contains(searchTerm));
            }

            // Filtro de categoría
            if (_categoryFilterBox.SelectedIndex > 0)
            {
                var selectedCategory = _categoryFilterBox.SelectedItem.ToString();
                filtered = filtered.Where(s => s.Category == selectedCategory);
            }

            return filtered.ToList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshLists();
        }

        private void CategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshLists();
        }

        private void AvailableScripts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _addToButtonsButton.IsEnabled = _availableScriptsListBox.SelectedItems.Count > 0;
        }

        private void CurrentButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _removeFromButtonsButton.IsEnabled = _currentButtonsListBox.SelectedItems.Count > 0;
        }

        private void AddToButtons_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _availableScriptsListBox.SelectedItems.Cast<ListBoxItem>().ToList();
            foreach (var item in selectedItems)
            {
                var script = (ScriptDefinition)item.Tag;
                _currentButtonScripts.Add(script);
            }
            RefreshLists();
        }

        private void RemoveFromButtons_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = _currentButtonsListBox.SelectedItems.Cast<ListBoxItem>().ToList();
            if (selectedItems.Count == 0)
            {
                return;
            }

            // Quitar de la lista visual de botones actuales
            // Remove from the visual list of current buttons
            foreach (var item in selectedItems)
            {
                var script = (ScriptDefinition)item.Tag;
                _currentButtonScripts.RemoveAll(s => s.Id == script.Id);
                // También actualizar el modelo principal para persistir
                var original = _allScripts.FirstOrDefault(s => s.Id == script.Id);
                if (original != null)
                {
                    original.ShowAsButton = false;
                }
            }

            // Guardar cambios inmediatamente en el JSON de AppData
            // Persist changes immediately to AppData JSON
            ScriptManager.SaveAllScripts(_allScripts);

            // Refrescar UI
            // Refresh UI
            RefreshLists();

            // Notificar a quien escuche que los botones fueron actualizados
            // Notify listeners that buttons were updated
            ButtonsUpdated?.Invoke(this, EventArgs.Empty);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exportWindow = new ExportSelectionWindow();
                exportWindow.ShowDialog();

                if (!exportWindow.DialogResult)
                {
                    return;
                }

                var selectedScripts = exportWindow.SelectedScripts;
                if (selectedScripts == null || selectedScripts.Count == 0)
                {
                    MessageBox.Show("No scripts selected to export.",
                                   "No Selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

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

                    MessageBox.Show($"Scripts exported successfully to:\n{Path.GetFileName(saveFileDialog.FileName)}",
                                   "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting scripts: {ex.Message}", "Export Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Title = "Import Scripts",
                    Filter = "JSON Files (*.json)|*.json|All files (*.*)|*.*",
                    DefaultExt = "json"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    var jsonContent = File.ReadAllText(openFileDialog.FileName, Encoding.UTF8);
                    
                    List<ScriptDefinition> scriptsToImport;
                    try
                    {
                        // Primero intentar deserializar como formato de exportación completo
                        var exportData = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonContent);
                        if (exportData != null && exportData["Scripts"] != null)
                        {
                            scriptsToImport = exportData["Scripts"].ToObject<List<ScriptDefinition>>();
                        }
                        else
                        {
                            // Si no tiene el formato de exportación, intentar como lista directa
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                        }
                    }
                    catch (Exception)
                    {
                        // Si falla, intentar como lista directa
                        try
                        {
                            scriptsToImport = JsonConvert.DeserializeObject<List<ScriptDefinition>>(jsonContent);
                        }
                        catch (Exception finalEx)
                        {
                            MessageBox.Show($"Error reading JSON file:\n{finalEx.Message}", 
                                           "Format Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (scriptsToImport == null || scriptsToImport.Count == 0)
                    {
                        MessageBox.Show("No valid scripts found in the selected file.",
                                       "Empty File", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    var selectionWindow = new ImportSelectionWindow(scriptsToImport, Path.GetFileName(openFileDialog.FileName));
                    var result = selectionWindow.ShowDialog();

                    if (result == true && selectionWindow.SelectedScripts.Count > 0)
                    {
                        var currentScripts = ScriptManager.LoadAllScripts();
                        int newScripts = 0;
                        int updatedScripts = 0;

                        foreach (var script in selectionWindow.SelectedScripts)
                        {
                            if (string.IsNullOrWhiteSpace(script.Name) || string.IsNullOrWhiteSpace(script.Code))
                            {
                                continue;
                            }

                            // Asignar nuevo ID
                            script.Id = Guid.NewGuid().ToString();
                            
                            // Asegurar que los scripts importados no aparezcan como botones por defecto
                            script.ShowAsButton = false;

                            var existingScript = currentScripts.FirstOrDefault(s => 
                                s.Name.Equals(script.Name, StringComparison.OrdinalIgnoreCase) && 
                                s.Category.Equals(script.Category, StringComparison.OrdinalIgnoreCase));

                            if (existingScript != null)
                            {
                                existingScript.Code = script.Code;
                                existingScript.Description = script.Description;
                                existingScript.Category = script.Category;
                                // Mantener la configuración ShowAsButton existente
                                updatedScripts++;
                            }
                            else
                            {
                                currentScripts.Add(script);
                                newScripts++;
                            }
                        }

                        ScriptManager.SaveAllScripts(currentScripts);
                        
                        // Recargar datos completamente
                        _allScripts = ScriptManager.LoadAllScripts();
                        // Recargar también los botones actuales para reflejar cualquier cambio
                        _currentButtonScripts = _allScripts.Where(s => s.ShowAsButton).ToList();
                        
                        // Actualizar categorías en el filtro
                        var categories = _allScripts.Select(s => s.Category).Distinct().OrderBy(c => c).ToList();
                        _categoryFilterBox.Items.Clear();
                        _categoryFilterBox.Items.Add("🔄 All categories");
                        foreach (var category in categories)
                        {
                            _categoryFilterBox.Items.Add(category);
                        }
                        _categoryFilterBox.SelectedIndex = 0;
                        
                        // Forzar actualización inmediata de las listas
                        Dispatcher.Invoke(() =>
                        {
                            RefreshLists();
                            _availableScriptsListBox.UpdateLayout();
                            _currentButtonsListBox.UpdateLayout();
                        });

                        MessageBox.Show($"Import completed successfully.\n\n" +
                                       $"➕ New scripts: {newScripts}\n" +
                                       $"🔄 Updated scripts: {updatedScripts}\n\n" +
                                       $"💡 Imported scripts are available in the left list\n" +
                                       $"to add them as buttons.",
                                       "Import Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing scripts: {ex.Message}", "Import Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Primero, marcar todos los scripts como no-botones
                foreach (var script in _allScripts)
                {
                    script.ShowAsButton = false;
                }
                
                // Luego, marcar solo los scripts seleccionados como botones
                foreach (var buttonScript in _currentButtonScripts)
                {
                    var originalScript = _allScripts.FirstOrDefault(s => s.Id == buttonScript.Id);
                    if (originalScript != null)
                    {
                        originalScript.ShowAsButton = true;
                    }
                }
                
                // Guardar todos los scripts con las nuevas configuraciones
                ScriptManager.SaveAllScripts(_allScripts);
                
                // Notificar que los botones fueron actualizados
                ButtonsUpdated?.Invoke(this, EventArgs.Empty);
                
                MessageBox.Show("Button configuration saved successfully.", 
                               "Changes Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al guardar cambios: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAllButtons_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Do you want to remove all scripts from quick access buttons?\n\nThis action doesn't delete the scripts, it only removes them from the buttons tab.",
                    "Confirm cleanup", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // Persistir cambio: desmarcar todos como botones
                ScriptManager.ClearAllButtonScripts();

                // Recargar datos en memoria y UI
                _allScripts = ScriptManager.LoadAllScripts();
                _currentButtonScripts = _allScripts.Where(s => s.ShowAsButton).ToList(); // debería quedar vacío
                RefreshLists();

                // Notificar a quien suscriba para regenerar los botones en el panel principal
                ButtonsUpdated?.Invoke(this, EventArgs.Empty);

                MessageBox.Show("All buttons were removed successfully.",
                                "Operation successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error removing all buttons: {ex.Message}",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}