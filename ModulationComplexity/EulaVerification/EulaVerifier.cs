using System;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MAAS.Common.EulaVerification
{
    /// <summary>
    /// EULA verification utility class for ESAPI scripts
    /// </summary>
    public class EulaVerifier
    {
        private readonly EulaConfig _config;
        private readonly string _projectName;
        private readonly string _githubPagesUrl;
        private readonly string _secretKey;

        /// <summary>
        /// Create a new EULA verifier
        /// </summary>
        /// <param name="projectName">Project name </param>
        /// <param name="githubPagesUrl">URL to the GitHub Page site with the JotForm</param>
        /// <param name="secretKey">Secret key used for code verification </param>
        public EulaVerifier(
            string projectName,
            string githubPagesUrl,
            string secretKey = "DefaultMAASSecretKey2025!")
        {
            _projectName = projectName;
            _githubPagesUrl = githubPagesUrl;
            _secretKey = secretKey;
            _config = EulaConfig.Load(projectName);
        }

        /// <summary>
        /// Check if the EULA has been accepted for this project
        /// </summary>
        public bool IsEulaAccepted()
        {
            string configKey = _projectName;

            if (_config.AcceptedEulas.TryGetValue(configKey, out string storedCode))
            {
                // Verify the stored code is valid
                return VerifyEulaCode(storedCode);
            }

            return false;
        }

        /// <summary>
        /// Show the EULA acceptance dialog
        /// </summary>
        public bool ShowEulaDialog(BitmapImage qrCodeImage = null)
        {
            // Create WPF window for EULA acceptance
            Window eulaWindow = new Window
            {
                Title = $"EULA Acceptance - {_projectName}",
                Width = 500,
                Height = 350,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize
            };

            // Create layout
            Grid grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(60) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

            // Instructions
            TextBlock instructions = new TextBlock
            {
                Text = $"Please visit {_githubPagesUrl} to accept the EULA and receive your access code.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 20, 20, 10),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(instructions, 0);

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
                Grid.SetRow(qrImage, 1);
                grid.Children.Add(qrImage);
            }
            else
            {
                // QR Code placeholder
                Border qrBorder = new Border
                {
                    Width = 120,
                    Height = 120,
                    BorderBrush = System.Windows.Media.Brushes.Black,
                    BorderThickness = new Thickness(1),
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
                Grid.SetRow(qrBorder, 1);
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
                Margin = new Thickness(0, 0, 10, 0)
            };

            TextBox codeTextBox = new TextBox
            {
                Width = 250,
                Height = 25,
                VerticalAlignment = VerticalAlignment.Center
            };

            codePanel.Children.Add(codeLabel);
            codePanel.Children.Add(codeTextBox);
            Grid.SetRow(codePanel, 2);

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
            Grid.SetRow(buttonPanel, 3);

            // Add all controls to grid
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
                    // Store the accepted EULA
                    _config.AcceptedEulas[_projectName] = code;
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

        /// <summary>
        /// Verify an EULA acceptance code
        /// </summary>
        private bool VerifyEulaCode(string inputCode)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                // Create a deterministic "expected" code for this project
                string expectedCode = $"{_projectName}-{_secretKey}";

                // Hash the expected code
                byte[] expectedHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(expectedCode));
                string expectedHash = BitConverter.ToString(expectedHashBytes).Replace("-", "").ToLowerInvariant();

                // Hash the input code
                byte[] inputHashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(inputCode));
                string inputHash = BitConverter.ToString(inputHashBytes).Replace("-", "").ToLowerInvariant();

                return string.Equals(inputHash, expectedHash, StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}