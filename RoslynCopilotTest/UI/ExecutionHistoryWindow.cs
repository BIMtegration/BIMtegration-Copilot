using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;
using WpfColor = System.Windows.Media.Color;

namespace RoslynCopilotTest.UI
{
    /// <summary>
    /// Ventana para mostrar el historial de ejecuciones de scripts
    /// </summary>
    public class ExecutionHistoryWindow : Window
    {
        private ListBox _historyListBox;
        private ComboBox _filterComboBox;
        private DatePicker _fromDatePicker;
        private DatePicker _toDatePicker;
        private TextBlock _statisticsText;
        private Button _refreshButton;
        private Button _clearButton;
        private Button _reExecuteButton;
        private List<ExecutionHistoryEntry> _allHistory;
        private List<ExecutionHistoryEntry> _filteredHistory;

        public ExecutionHistoryWindow()
        {
            InitializeWindow();
            LoadHistory();
        }

        /// <summary>
        /// Inicializa la ventana y sus controles
        /// </summary>
        private void InitializeWindow()
        {
            Title = "Historial de Ejecuciones";
            Width = 800;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new SolidColorBrush(WpfColor.FromRgb(250, 250, 250));

            var mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Filtros
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Lista
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Estadísticas
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Botones

            // Header
            var headerPanel = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(45, 45, 48)),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var headerText = new TextBlock
            {
                Text = "📊 Historial de Ejecuciones",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White
            };

            headerPanel.Child = headerText;
            Grid.SetRow(headerPanel, 0);
            mainGrid.Children.Add(headerPanel);

            // Panel de filtros
            var filtersPanel = CreateFiltersPanel();
            Grid.SetRow(filtersPanel, 1);
            mainGrid.Children.Add(filtersPanel);

            // Lista de historial
            var historyPanel = CreateHistoryPanel();
            Grid.SetRow(historyPanel, 2);
            mainGrid.Children.Add(historyPanel);

            // Panel de estadísticas
            var statsPanel = CreateStatisticsPanel();
            Grid.SetRow(statsPanel, 3);
            mainGrid.Children.Add(statsPanel);

            // Panel de botones
            var buttonPanel = CreateButtonPanel();
            Grid.SetRow(buttonPanel, 4);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        /// <summary>
        /// Crea el panel de filtros
        /// </summary>
        private Border CreateFiltersPanel()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(20, 15, 20, 15)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Filtro por estado
            var filterLabel = new TextBlock
            {
                Text = "Filtrar:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            Grid.SetColumn(filterLabel, 0);
            grid.Children.Add(filterLabel);

            _filterComboBox = new ComboBox
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0)
            };
            _filterComboBox.Items.Add("Todas las ejecuciones");
            _filterComboBox.Items.Add("Solo exitosas");
            _filterComboBox.Items.Add("Solo fallidas");
            _filterComboBox.Items.Add("Últimas 24 horas");
            _filterComboBox.Items.Add("Última semana");
            _filterComboBox.SelectedIndex = 0;
            _filterComboBox.SelectionChanged += FilterComboBox_SelectionChanged;
            Grid.SetColumn(_filterComboBox, 1);
            grid.Children.Add(_filterComboBox);

            // Filtro desde
            var fromLabel = new TextBlock
            {
                Text = "Desde:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(fromLabel, 2);
            grid.Children.Add(fromLabel);

            _fromDatePicker = new DatePicker
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 20, 0)
            };
            _fromDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            Grid.SetColumn(_fromDatePicker, 3);
            grid.Children.Add(_fromDatePicker);

            // Filtro hasta
            var toLabel = new TextBlock
            {
                Text = "Hasta:",
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            Grid.SetColumn(toLabel, 4);
            grid.Children.Add(toLabel);

            _toDatePicker = new DatePicker
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            _toDatePicker.SelectedDateChanged += DatePicker_SelectedDateChanged;
            Grid.SetColumn(_toDatePicker, 5);
            grid.Children.Add(_toDatePicker);

            border.Child = grid;
            return border;
        }

        /// <summary>
        /// Crea el panel del historial
        /// </summary>
        private Border CreateHistoryPanel()
        {
            var border = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Margin = new Thickness(20, 10, 20, 10)
            };

            _historyListBox = new ListBox
            {
                Background = Brushes.White,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5)
            };

            _historyListBox.SelectionChanged += HistoryListBox_SelectionChanged;

            border.Child = _historyListBox;
            return border;
        }

        /// <summary>
        /// Crea el panel de estadísticas
        /// </summary>
        private Border CreateStatisticsPanel()
        {
            var border = new Border
            {
                Background = new SolidColorBrush(WpfColor.FromRgb(245, 245, 245)),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(220, 220, 220)),
                BorderThickness = new Thickness(0, 1, 0, 1),
                Padding = new Thickness(20, 10, 20, 10)
            };

            _statisticsText = new TextBlock
            {
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(80, 80, 80))
            };

            border.Child = _statisticsText;
            return border;
        }

        /// <summary>
        /// Crea el panel de botones
        /// </summary>
        private Border CreateButtonPanel()
        {
            var border = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(20, 15, 20, 15)
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            _reExecuteButton = new Button
            {
                Content = "🔄 Re-ejecutar Script",
                Width = 150,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(WpfColor.FromRgb(40, 167, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold,
                IsEnabled = false
            };
            _reExecuteButton.Click += ReExecuteButton_Click;
            panel.Children.Add(_reExecuteButton);

            _refreshButton = new Button
            {
                Content = "🔄 Actualizar",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(WpfColor.FromRgb(0, 123, 255)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            _refreshButton.Click += RefreshButton_Click;
            panel.Children.Add(_refreshButton);

            _clearButton = new Button
            {
                Content = "🗑️ Limpiar",
                Width = 100,
                Height = 35,
                Background = new SolidColorBrush(WpfColor.FromRgb(220, 53, 69)),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.SemiBold
            };
            _clearButton.Click += ClearButton_Click;
            panel.Children.Add(_clearButton);

            border.Child = panel;
            return border;
        }

        /// <summary>
        /// Carga el historial de ejecuciones
        /// </summary>
        private void LoadHistory()
        {
            _allHistory = ExecutionHistoryManager.LoadHistory();
            ApplyFilters();
            UpdateStatistics();
        }

        /// <summary>
        /// Aplica los filtros activos
        /// </summary>
        private void ApplyFilters()
        {
            _filteredHistory = _allHistory.ToList();

            // Filtrar por estado
            if (_filterComboBox.SelectedIndex == 1) // Solo exitosas
            {
                _filteredHistory = _filteredHistory.Where(h => h.Success).ToList();
            }
            else if (_filterComboBox.SelectedIndex == 2) // Solo fallidas
            {
                _filteredHistory = _filteredHistory.Where(h => !h.Success).ToList();
            }
            else if (_filterComboBox.SelectedIndex == 3) // Últimas 24 horas
            {
                var yesterday = DateTime.Now.AddDays(-1);
                _filteredHistory = _filteredHistory.Where(h => h.ExecutionTime >= yesterday).ToList();
            }
            else if (_filterComboBox.SelectedIndex == 4) // Última semana
            {
                var lastWeek = DateTime.Now.AddDays(-7);
                _filteredHistory = _filteredHistory.Where(h => h.ExecutionTime >= lastWeek).ToList();
            }

            // Filtrar por rango de fechas
            if (_fromDatePicker.SelectedDate.HasValue)
            {
                _filteredHistory = _filteredHistory.Where(h => h.ExecutionTime.Date >= _fromDatePicker.SelectedDate.Value.Date).ToList();
            }

            if (_toDatePicker.SelectedDate.HasValue)
            {
                _filteredHistory = _filteredHistory.Where(h => h.ExecutionTime.Date <= _toDatePicker.SelectedDate.Value.Date).ToList();
            }

            UpdateHistoryList();
        }

        /// <summary>
        /// Actualiza la lista del historial
        /// </summary>
        private void UpdateHistoryList()
        {
            _historyListBox.Items.Clear();

            foreach (var entry in _filteredHistory)
            {
                var item = CreateHistoryListItem(entry);
                _historyListBox.Items.Add(item);
            }
        }

        /// <summary>
        /// Crea un elemento de la lista del historial
        /// </summary>
        private ListBoxItem CreateHistoryListItem(ExecutionHistoryEntry entry)
        {
            var listItem = new ListBoxItem
            {
                Tag = entry,
                Padding = new Thickness(10, 8, 10, 8),
                BorderThickness = new Thickness(0, 0, 0, 1),
                BorderBrush = new SolidColorBrush(WpfColor.FromRgb(240, 240, 240))
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            // Estado y script
            var statusPanel = new StackPanel { Orientation = Orientation.Horizontal };
            
            var statusIcon = new TextBlock
            {
                Text = entry.StatusIcon,
                FontSize = 14,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            var scriptInfo = new StackPanel();
            
            var scriptName = new TextBlock
            {
                Text = entry.ScriptName,
                FontWeight = FontWeights.SemiBold,
                FontSize = 12
            };

            var categoryText = new TextBlock
            {
                Text = $"📂 {entry.ScriptCategory}",
                FontSize = 10,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100))
            };

            scriptInfo.Children.Add(scriptName);
            scriptInfo.Children.Add(categoryText);

            statusPanel.Children.Add(statusIcon);
            statusPanel.Children.Add(scriptInfo);

            Grid.SetColumn(statusPanel, 0);
            grid.Children.Add(statusPanel);

            // Resultado
            var resultText = new TextBlock
            {
                Text = entry.ResultDescription,
                FontSize = 11,
                Foreground = entry.Success ? 
                    new SolidColorBrush(WpfColor.FromRgb(40, 167, 69)) : 
                    new SolidColorBrush(WpfColor.FromRgb(220, 53, 69)),
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 200,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(10, 0, 10, 0)
            };

            Grid.SetColumn(resultText, 1);
            grid.Children.Add(resultText);

            // Duración
            var durationText = new TextBlock
            {
                Text = entry.FormattedDuration,
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100)),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };

            Grid.SetColumn(durationText, 2);
            grid.Children.Add(durationText);

            // Fecha/hora
            var timeText = new TextBlock
            {
                Text = entry.ExecutionTime.ToString("dd/MM/yyyy HH:mm:ss"),
                FontSize = 11,
                Foreground = new SolidColorBrush(WpfColor.FromRgb(100, 100, 100)),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(timeText, 3);
            grid.Children.Add(timeText);

            listItem.Content = grid;
            return listItem;
        }

        /// <summary>
        /// Actualiza las estadísticas
        /// </summary>
        private void UpdateStatistics()
        {
            var stats = ExecutionHistoryManager.GetStatistics();
            
            _statisticsText.Text = $"📊 Total: {stats.TotalExecutions} ejecuciones | " +
                                  $"✅ Exitosas: {stats.SuccessfulExecutions} ({stats.SuccessRate:F1}%) | " +
                                  $"❌ Fallidas: {stats.FailedExecutions} | " +
                                  $"⏱️ Tiempo promedio: {stats.FormattedAverageTime} | " +
                                  $"🔥 Más ejecutado: {stats.MostExecutedScript}";
        }

        // Event Handlers
        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void HistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _reExecuteButton.IsEnabled = _historyListBox.SelectedItem != null;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadHistory();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "¿Estás seguro de que quieres eliminar todo el historial de ejecuciones?\n\nEsta acción no se puede deshacer.",
                "Confirmar eliminación",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                ExecutionHistoryManager.ClearHistory();
                LoadHistory();
            }
        }

        private void ReExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_historyListBox.SelectedItem is ListBoxItem selectedItem && 
                selectedItem.Tag is ExecutionHistoryEntry entry)
            {
                // Buscar el script en el sistema
                var script = ScriptManager.FindScript(entry.ScriptId);
                if (script != null)
                {
                    // Cerrar esta ventana y notificar al panel principal para ejecutar el script
                    DialogResult = true;
                    Tag = script; // Pasar el script como resultado
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        $"El script '{entry.ScriptName}' ya no existe en el sistema.",
                        "Script no encontrado",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
            }
        }
    }
}