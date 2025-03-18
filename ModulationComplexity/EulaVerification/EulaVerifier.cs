using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

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
        private readonly string _licenseUrl;
        private readonly string _secretKey;

        /// <summary>
        /// Create a new EULA verifier with mandatory version support
        /// </summary>
        /// <param name="projectName">Project name</param>
        /// <param name="version">Project version (required)</param>
        /// <param name="licenseUrl">URL to the GitHub Page site with the JotForm</param>
        /// <param name="secretKey">Secret key used for code verification</param>
        public EulaVerifier(
            string projectName,
            string version,
            string licenseUrl,
            string secretKey = "VarianMAASSecretKey2025")
        {
            if (string.IsNullOrEmpty(version))
                throw new ArgumentException("Version parameter is required", nameof(version));

            _projectName = projectName;
            _version = version;
            _licenseUrl = licenseUrl;
            _secretKey = secretKey;

            try
            {
                _config = EulaConfig.Load(projectName);
                System.Diagnostics.Debug.WriteLine($"Successfully loaded EulaConfig for {projectName}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading EulaConfig: {ex.Message}");
                // Create an empty config as fallback
                _config = new EulaConfig();
            }
        }

        /// <summary>
        /// Check if the EULA has been accepted for this project and version
        /// </summary>
        public bool IsEulaAccepted()
        {
            string configKey = GetConfigKey();
            System.Diagnostics.Debug.WriteLine($"Checking EULA acceptance for: {configKey}");

            // Debug: List all entries in config
            foreach (var key in _config.AcceptedEulas.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"Found entry: {key} = {_config.AcceptedEulas[key]}");
            }

            if (_config.AcceptedEulas.TryGetValue(configKey, out string storedCode))
            {
                // Verify the stored code is valid
                bool isValid = VerifyEulaCode(storedCode);
                System.Diagnostics.Debug.WriteLine($"Stored code found. Valid: {isValid}");
                return isValid;
            }

            // Check if any previous version has been accepted
            // This allows for backward compatibility with previous acceptances if desired
            var majorMinorVersion = GetMajorMinorVersion(_version);
            if (!string.IsNullOrEmpty(majorMinorVersion))
            {
                string exactVersionPattern = $"{_projectName}-{majorMinorVersion}";
                foreach (var key in _config.AcceptedEulas.Keys)
                {
                    // Only accept keys with exact major.minor version match
                    if (key.StartsWith(exactVersionPattern))
                    {
                        string oldCode = _config.AcceptedEulas[key];
                        System.Diagnostics.Debug.WriteLine($"Found exact major.minor version acceptance: {key}");
                        return true;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("No valid EULA acceptance found");
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
        /// Extract the major.minor version string
        /// </summary>
        private string GetMajorMinorVersion(string version)
        {
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            // Extract the major.minor version (e.g., "16.1.0" -> "16.1")
            var parts = version.Split('.');
            if (parts.Length >= 2)
                return $"{parts[0]}.{parts[1]}";
            else if (parts.Length == 1)
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
                Title = $"License Acceptance - {_projectName} v({_version})",
                Width = 600,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White
            };

            // Create layout
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
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
            grid.Children.Add(introMessage);

            // Instructions
            TextBlock instructions = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 5, 20, 10),
                VerticalAlignment = VerticalAlignment.Center
            };

            // Create the hyperlink inline
            Hyperlink link = new Hyperlink();
            link.Inlines.Add(_licenseUrl);
            link.NavigateUri = new Uri(_licenseUrl);
            link.RequestNavigate += (sender, e) =>
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            };

            // Add text with embedded hyperlink
            instructions.Inlines.Add("Please visit ");
            instructions.Inlines.Add(link);
            instructions.Inlines.Add(" or use the QR code below to accept the license and receive your access code.");

            Grid.SetRow(instructions, 1);
            grid.Children.Add(instructions);

            // QR Code Image
            if (qrCodeImage != null)
            {
                // Create a NEW image control each time
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
                Background = Brushes.White,
                Foreground = Brushes.Black
            };

            codePanel.Children.Add(codeLabel);
            codePanel.Children.Add(codeTextBox);
            Grid.SetRow(codePanel, 3);
            grid.Children.Add(codePanel);

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
                Margin = new Thickness(0, 0, 10, 0),
                Background = Brushes.LightGray
            };

            Button cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 25,
                Background = Brushes.LightGray
            };

            buttonPanel.Children.Add(verifyButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 4);
            grid.Children.Add(buttonPanel);

            // Important: set the content only once at the end
            eulaWindow.Content = grid;

            // Set up event handlers
            bool result = false;

            verifyButton.Click += (sender, e) =>
            {
                string code = codeTextBox.Text.Trim();

                if (VerifyEulaCode(code))
                {
                    string configKey = GetConfigKey();

                    // Directly modify the EulaEntries list 
                    if (_config.EulaEntries == null)
                    {
                        _config.EulaEntries = new List<EulaEntry>();
                    }

                    // Check if entry already exists
                    bool found = false;
                    foreach (var entry in _config.EulaEntries)
                    {
                        if (entry.Key == configKey)
                        {
                            entry.Value = code; // Update existing entry
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        // Add new entry
                        _config.EulaEntries.Add(new EulaEntry { Key = configKey, Value = code });
                    }

                    // Also update the dictionary for in-memory use
                    _config.AcceptedEulas[configKey] = code;

                    // Set EULAAgreed to true since they've entered a valid code
                    if (_config.Settings != null)
                    {
                        _config.Settings.EULAAgreed = true;
                    }

                    // Try to save
                    bool saveSuccess = _config.Save();

                    if (!saveSuccess)
                    {
                        // Fallback: Create simple text file as backup
                        try
                        {
                            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                            string backupPath = Path.Combine(dir, $"{_projectName}_accepted.txt");
                            File.WriteAllText(backupPath, $"{configKey}={code}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Backup save failed: {ex.Message}");
                        }
                    }

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

        /// <summary>
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
                    string expectedShortCode = expectedHash.Substring(0, 8);

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
                return expectedHash.Substring(0, 8);
            }
        }
    }
}