using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RoslynCopilotTest.Services;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using MediaColor = System.Windows.Media.Color;
using System.IO;
using System.Diagnostics;

namespace RoslynCopilotTest.UI
{
    /// <summary>
    /// Ventana de login para BIMtegration
    /// </summary>
    public class BIMLoginWindow : Window
    {
        private WpfTextBox _usuarioTextBox;
        private PasswordBox _clavePasswordBox;
        private WpfButton _loginButton;
        private WpfButton _cancelButton;
        private TextBlock _statusTextBlock;
        private ProgressBar _progressBar;
        private readonly BIMAuthService _authService;

        public bool LoginSuccessful { get; private set; }
        public UserInfo AuthenticatedUser { get; private set; }
        public List<PremiumButtonInfo> PremiumButtons { get; private set; } = new List<PremiumButtonInfo>();

        public BIMLoginWindow(BIMAuthService authService)
        {
            _authService = authService;
            InitializeWindow();
        }

        private void InitializeWindow()
        {
            // Configuración de la ventana
            Title = "BIMtegration - Login";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;
            Background = new SolidColorBrush(MediaColor.FromRgb(45, 45, 48));

            // Layout principal
            var mainGrid = new Grid { Margin = new Thickness(20) };
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Título
            var titleLabel = new Label
            {
                Content = "🔐 BIMtegration Login",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(titleLabel, 0);
            mainGrid.Children.Add(titleLabel);

            // Panel de formulario
            var formPanel = CreateFormPanel();
            Grid.SetRow(formPanel, 1);
            mainGrid.Children.Add(formPanel);

            // Barra de progreso
            _progressBar = new ProgressBar
            {
                Height = 4,
                IsIndeterminate = true,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Grid.SetRow(_progressBar, 2);
            mainGrid.Children.Add(_progressBar);

            // Panel de botones
            var buttonPanel = CreateButtonPanel();
            Grid.SetRow(buttonPanel, 3);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;

            // Focus inicial
            Loaded += (s, e) => _usuarioTextBox.Focus();
        }

        private StackPanel CreateFormPanel()
        {
            var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 10) };

            // Username
            var usuarioLabel = new Label
            {
                Content = "Username:",
                Foreground = Brushes.White,
                FontSize = 12
            };
            panel.Children.Add(usuarioLabel);

            _usuarioTextBox = new WpfTextBox
            {
                Height = 30,
                FontSize = 14,
                Padding = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(62, 62, 64)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            _usuarioTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    _clavePasswordBox.Focus();
            };
            panel.Children.Add(_usuarioTextBox);

            // Password
            var claveLabel = new Label
            {
                Content = "Password:",
                Foreground = Brushes.White,
                FontSize = 12
            };
            panel.Children.Add(claveLabel);

            _clavePasswordBox = new PasswordBox
            {
                Height = 30,
                FontSize = 14,
                Padding = new Thickness(5),
                Background = new SolidColorBrush(MediaColor.FromRgb(62, 62, 64)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                Margin = new Thickness(0, 0, 0, 15)
            };
            _clavePasswordBox.KeyDown += async (s, e) =>
            {
                if (e.Key == System.Windows.Input.Key.Enter)
                    await PerformLoginAsync();
            };
            panel.Children.Add(_clavePasswordBox);

            // Status text
            _statusTextBlock = new TextBlock
            {
                FontSize = 12,
                Foreground = Brushes.Yellow,
                TextWrapping = TextWrapping.Wrap,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 0, 0, 10)
            };
            panel.Children.Add(_statusTextBlock);

            return panel;
        }

        private StackPanel CreateButtonPanel()
        {
            var panel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            // Botón Cancelar
            _cancelButton = new WpfButton
            {
                Content = "Cancel",
                Width = 100,
                Height = 35,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(MediaColor.FromRgb(62, 62, 64)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(5)
            };
            _cancelButton.Click += (s, e) =>
            {
                LoginSuccessful = false;
                Close();
            };
            panel.Children.Add(_cancelButton);

            // Botón Login
            _loginButton = new WpfButton
            {
                Content = "🔓 Login",
                Width = 150,
                Height = 35,
                Background = new SolidColorBrush(MediaColor.FromRgb(0, 120, 212)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(MediaColor.FromRgb(0, 100, 180)),
                BorderThickness = new Thickness(1),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(5)
            };
            _loginButton.Click += async (s, e) => await PerformLoginAsync();
            panel.Children.Add(_loginButton);

            return panel;
        }

        private async System.Threading.Tasks.Task PerformLoginAsync()
        {
            var usuario = _usuarioTextBox.Text.Trim();
            var clave = _clavePasswordBox.Password;

            // Validar inputs
            if (string.IsNullOrEmpty(usuario))
            {
                ShowStatus("⚠️ Please enter your username", Brushes.Yellow);
                _usuarioTextBox.Focus();
                return;
            }

            if (string.IsNullOrEmpty(clave))
            {
                ShowStatus("⚠️ Please enter your password", Brushes.Yellow);
                _clavePasswordBox.Focus();
                return;
            }

            // Deshabilitar controles
            SetControlsEnabled(false);
            _progressBar.Visibility = Visibility.Visible;
            ShowStatus("🔄 Authenticating...", Brushes.Cyan);

            try
            {
                var result = await _authService.LoginAsync(usuario, clave);

                if (result.Success)
                {
                    ShowStatus("✅ Login successful!", Brushes.LightGreen);
                    await System.Threading.Tasks.Task.Delay(500);
                    
                    LoginSuccessful = true;
                    AuthenticatedUser = result.User;
                    PremiumButtons = result.Buttons ?? new List<PremiumButtonInfo>();
                    
                    // Logging a archivo
                    LogToFile($"[BIMLoginWindow] ✅ Login exitoso - Usuario: {result.User?.Usuario ?? "unknown"}");
                    LogToFile($"[BIMLoginWindow] Plan: {result.User?.Plan ?? "unknown"}");
                    LogToFile($"[BIMLoginWindow] Botones premium recibidos: {PremiumButtons?.Count ?? 0}");
                    if (PremiumButtons?.Count > 0)
                    {
                        foreach (var btn in PremiumButtons)
                        {
                            LogToFile($"  - {btn.name} (Empresa: {btn.company})");
                        }
                    }
                    
                    Close();
                }
                else
                {
                    ShowStatus($"❌ {result.Message}", Brushes.OrangeRed);
                    LogToFile($"[BIMLoginWindow] ❌ Login falló: {result.Message}");
                    SetControlsEnabled(true);
                    _progressBar.Visibility = Visibility.Collapsed;
                    _clavePasswordBox.Clear();
                    _clavePasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Error: {ex.Message}", Brushes.Red);
                SetControlsEnabled(true);
                _progressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void ShowStatus(string message, Brush color)
        {
            _statusTextBlock.Text = message;
            _statusTextBlock.Foreground = color;
            _statusTextBlock.Visibility = Visibility.Visible;
        }

        private void SetControlsEnabled(bool enabled)
        {
            _usuarioTextBox.IsEnabled = enabled;
            _clavePasswordBox.IsEnabled = enabled;
            _loginButton.IsEnabled = enabled;
            _cancelButton.IsEnabled = enabled;
        }

        private void LogToFile(string message)
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
    }
}
