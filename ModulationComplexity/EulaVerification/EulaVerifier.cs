using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MAAS.Common.EulaVerification
{
    /// <summary>
    /// EULA verification utility class for ESAPI scripts with version support
    /// </summary>
    public class EulaVerifier
    {
        private readonly EulaConfig _config;
        private readonly string _projectName;
        private readonly string _version;
        private readonly string _githubPagesUrl;
        private readonly string _secretKey;

        /// <summary>
        /// Create a new EULA verifier with mandatory version support
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="version">Project version (required)</param>
        /// <param name="githubPagesUrl">URL to the GitHub Page site with the JotForm</param>
        /// <param name="secretKey">Secret key used for code verification</param>
        public EulaVerifier(
            string projectName,
            string version,
            string githubPagesUrl,
            string secretKey = "VarianMAASSecretKey2025")
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version parameter is required", nameof(version));

            _projectName = projectName;
            _version = version;
            _githubPagesUrl = githubPagesUrl;
            _secretKey = secretKey;
            _config = EulaConfig.Load(projectName);
        }

        /// <summary>
        /// Check if the EULA has been accepted for this project and version
        /// </summary>
        public bool IsEulaAccepted()
        {
            string configKey = GetConfigKey();
            System.Diagnostics.Debug.WriteLine($"Checking EULA acceptance for: {configKey}");

            if (_config.AcceptedEulas.TryGetValue(configKey, out string storedCode))
            {
                // Verify the stored code is valid
                return VerifyEulaCode(storedCode);
            }

            // Check if any previous version has been accepted
            // This allows for backward compatibility with previous acceptances if desired
            var majorVersion = GetMajorVersion(_version);
            if (!string.IsNullOrEmpty(majorVersion))
            {
                foreach (var key in _config.AcceptedEulas.Keys)
                {
                    // If the key starts with the project name and has the same major version
                    if (key.StartsWith($"{_projectName}-{majorVersion}"))
                    {
                        string oldCode = _config.AcceptedEulas[key];
                        // Optionally validate with older version's code
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Get the config key based on project name and version
        /// </summary>
        private string GetConfigKey()
        {
            return $"{_projectName}-{_version}";
        }

        /// <summary>
        /// Extract the major version number
        /// </summary>
        private string GetMajorVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            // Extract the major version (e.g., "16.1.0" -> "16")
            var parts = version.Split('.');
            if (parts.Length > 0)
                return parts[0];

            return version;
        }


        /// <summary>
        /// Show the EULA acceptance dialog
        /// </summary>
        public bool ShowEulaDialog(BitmapImage qrCodeImage = null)
        {
            // Create WPF window for EULA acceptance
            Window eulaWindow = new Window
            {
                Title = $"EULA Acceptance - {_projectName} v({_version})",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Create layout
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });


            // Introductory message
            TextBlock introMessage = new TextBlock
            {
                Text = $"First-time use of {_projectName} requires license acceptance.",
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 10, 20, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(introMessage, 0);
            

            // Instructions
            TextBox instructions = new TextBox
            {
                Text = $"Please visit {_githubPagesUrl} or use the QR code below to accept the EULA and receive your access code.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 5, 20, 10),
                VerticalAlignment = VerticalAlignment.Center,
                IsReadOnly = true,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent,
                IsTabStop = false,
                AcceptsReturn = false,
                AcceptsTab = false,
                IsReadOnlyCaretVisible = false
            };
            Grid.SetRow(instructions, 1);
          
            

            // QR Code Image
            if (qrCodeImage != null)
            {
                Image qrImage = new Image
                {
                    Source = qrCodeImage,
                    Width = 120,
                    Height = 120,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(qrImage, 2);
                grid.Children.Add(qrImage);
            }
            else
            {
                // QR Code placeholder
                Border qrBorder = new Border
                {
                    Width = 120,
                    Height = 120,
                    BorderBrush = System.Windows.Media.Brushes.LightBlue,
                    BorderThickness = new Thickness(3),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock qrPlaceholder = new TextBlock
                {
                    Text = "QR Code\nPlaceholder",
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                qrBorder.Child = qrPlaceholder;
                Grid.SetRow(qrBorder, 2);
                grid.Children.Add(qrBorder);
            }

            // Code input
            StackPanel codePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 10, 20, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            TextBlock codeLabel = new TextBlock
            {
                Text = "Enter your access code:",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            };

            TextBox codeTextBox = new TextBox
            {
                Width = 250,
                Height = 25,
                VerticalAlignment = VerticalAlignment.Center,
                BorderBrush = new SolidColorBrush(Colors.Gray),
                BorderThickness = new Thickness(2),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(4),
                Background = Brushes.White
            };

            codePanel.Children.Add(codeLabel);
            codePanel.Children.Add(codeTextBox);
            Grid.SetRow(codePanel, 3);

            // Buttons
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 10, 20, 10)
            };

            Button verifyButton = new Button
            {
                Content = "Verify",
                Width = 80,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 25
            };

            buttonPanel.Children.Add(verifyButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 4);

            // Add all controls to grid
            grid.Children.Add(introMessage);
            grid.Children.Add(instructions);
            grid.Children.Add(codePanel);
            grid.Children.Add(buttonPanel);

            eulaWindow.Content = grid;

            // Set up event handlers
            bool result = false;

            verifyButton.Click += (sender, e) =>
            {
                string code = codeTextBox.Text.Trim();

                if (VerifyEulaCode(code))
                {
                    // Store with the proper key that includes version
                    _config.AcceptedEulas[GetConfigKey()] = code;
                    _config.Save();
                    result = true;
                    eulaWindow.Close();
                }
                else
                {
                    MessageBox.Show("Invalid access code. Please try again.", "Error",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            cancelButton.Click += (sender, e) =>
            {
                result = false;
                eulaWindow.Close();
            };

            eulaWindow.ShowDialog();
            return result;
        }

        // <summary>
        /// Verify an EULA acceptance code using SHA256
        /// </summary>
        private bool VerifyEulaCode(string inputCode)
        {
            // Make sure we have a code to check
            if (string.IsNullOrWhiteSpace(inputCode))
                return false;

            using (SHA256 sha256 = SHA256.Create())
            {
                try
                {
                    // Always include version in the seed
                    string expectedSeed = $"{_projectName}-{_version}-{_secretKey}";

                    // Hash the expected code
                    byte[] expectedHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(expectedSeed));
                    string expectedHash = BitConverter.ToString(expectedHashBytes).Replace("-", "").ToLowerInvariant();

                    // Format the code exactly as it appears in your JotForm email
                    string expectedShortCode = $"MAAS-{_projectName.Substring(0, Math.Min(4, _projectName.Length))}-{expectedHash.Substring(0, 8)}";

                    // Compare with input - case insensitive
                    return string.Equals(inputCode, expectedShortCode, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in VerifyEulaCode: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Generate an access code for a specific project and version (for JotForm email)
        /// </summary>
        public static string GenerateAccessCode(string projectName, string version, string secretKey = "VarianMAASSecretKey2025")
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Create a deterministic seed for this project and version
                string expectedSeed = $"{projectName}-{version}-{secretKey}";

                // Hash the expected code
                byte[] expectedHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(expectedSeed));
                string expectedHash = BitConverter.ToString(expectedHashBytes).Replace("-", "").ToLowerInvariant();

                // Take first 8 characters to create a more user-friendly code
                return $"MAAS-{projectName.Substring(0, Math.Min(4, projectName.Length))}-{expectedHash.Substring(0, 8)}";
            }
        }
    }
}