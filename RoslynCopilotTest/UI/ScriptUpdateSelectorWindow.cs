using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using RoslynCopilotTest.Models;
using RoslynCopilotTest.Services;

namespace RoslynCopilotTest.UI
{
    public class ScriptUpdateSelectorWindow : Window
    {
    private ListBox _scriptsListBox;
    private TextBox _searchBox;
    private Button _activateButton;
    private Button _deactivateButton;
    private Button _editButton;
    private List<ScriptDefinition> _scripts;

        public ScriptUpdateSelectorWindow()
        {
            Title = "Actualizar Script";
            Width = 500;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _scripts = ScriptManager.LoadAllScripts();

            var mainStack = new StackPanel { Margin = new Thickness(10) };

            _searchBox = new TextBox { Margin = new Thickness(0, 0, 0, 8) };
            _searchBox.TextChanged += SearchBox_TextChanged;
            mainStack.Children.Add(_searchBox);

            _scriptsListBox = new ListBox { Height = 250 };
            _scriptsListBox.SelectionChanged += ScriptsListBox_SelectionChanged;
            mainStack.Children.Add(_scriptsListBox);

            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 0) };
            _activateButton = new Button { Content = "Activar como botón", IsEnabled = false, Margin = new Thickness(0, 0, 8, 0) };
            _activateButton.Click += ActivateButton_Click;
            buttonPanel.Children.Add(_activateButton);

            _deactivateButton = new Button { Content = "Desactivar botón", IsEnabled = false, Margin = new Thickness(0, 0, 8, 0) };
            _deactivateButton.Click += DeactivateButton_Click;
            buttonPanel.Children.Add(_deactivateButton);

            _editButton = new Button { Content = "Editar código", IsEnabled = false };
            _editButton.Click += EditButton_Click;
            buttonPanel.Children.Add(_editButton);

            mainStack.Children.Add(buttonPanel);

            Content = mainStack;
            RefreshScriptList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshScriptList();
        }

        private void RefreshScriptList()
        {
            var query = _searchBox.Text?.ToLower() ?? "";
            var filtered = string.IsNullOrWhiteSpace(query)
                ? _scripts
                : _scripts.Where(s => s.Name.ToLower().Contains(query) || s.Id.ToLower().Contains(query)).ToList();
            _scriptsListBox.ItemsSource = filtered;
            _scriptsListBox.DisplayMemberPath = "Name";
        }

        private void ScriptsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = _scriptsListBox.SelectedItem as ScriptDefinition;
            _activateButton.IsEnabled = selected != null && !selected.ShowAsButton;
            _deactivateButton.IsEnabled = selected != null && selected.ShowAsButton;
            _editButton.IsEnabled = selected != null;
        }

        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _scriptsListBox.SelectedItem as ScriptDefinition;
            if (selected != null)
            {
                selected.ShowAsButton = true;
                ScriptManager.SaveScript(selected);
                RefreshScriptList();
            }
        }

        private void DeactivateButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _scriptsListBox.SelectedItem as ScriptDefinition;
            if (selected != null)
            {
                selected.ShowAsButton = false;
                ScriptManager.SaveScript(selected);
                RefreshScriptList();
            }
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = _scriptsListBox.SelectedItem as ScriptDefinition;
            if (selected != null)
            {
                var editor = new ScriptEditorWindow(null, selected);
                editor.ShowDialog();
                // Recargar scripts por si hubo cambios
                _scripts = ScriptManager.LoadAllScripts();
                RefreshScriptList();
            }
        }
    }
}
