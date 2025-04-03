using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;
using Views;

using System.Net.NetworkInformation;
using System.IO;
using JR.Utils.GUI.Forms;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using ViewModels;
using ModulationComplexity.Models;
using System.Globalization;

using System.Windows.Media.Imaging;
using MAAS.Common.EulaVerification;

// TODO: Uncomment the following line if the script requires write access.
//15.x or later:
[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    public class Script
    {
        // Define the project information for EULA verification
        private const string PROJECT_NAME = "PlanComplexity";
        private const string PROJECT_VERSION = "1.0.0";
        private const string LICENSE_URL = "https://varian-medicalaffairsappliedsolutions.github.io/MAAS-PlanComplexity";
        private const string GITHUB_URL = "https://github.com/Varian-MedicalAffairsAppliedSolutions/MAAS-PlanComplexity";

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            try
            {
                // Check license acceptance first with version support
                var eulaVerifier = new EulaVerifier(PROJECT_NAME, PROJECT_VERSION, LICENSE_URL);

                // Get access to the EulaConfig
                var eulaConfig = EulaConfig.Load(PROJECT_NAME);
                if (eulaConfig.Settings == null)
                {
                    eulaConfig.Settings = new ApplicationSettings();
                }

                // If the JotForm license hasn't been accepted for this version, show the verification dialog
                if (!eulaVerifier.IsEulaAccepted())
                {
                    // Keep all your existing EULA verification dialog code here
                    MessageBox.Show(
                        $"This version of {PROJECT_NAME} (v{PROJECT_VERSION}) requires license acceptance before first use.\n\n" +
                        "You will be prompted to provide an access code. Please follow the instructions to obtain your code.",
                        "License Acceptance Required",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    // Load your pre-generated QR code image
                    BitmapImage qrCode = null;
                    try
                    {
                        string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
                        qrCode = new BitmapImage(new Uri($"pack://application:,,,/{assemblyName};component/Resources/qrcode.bmp"));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading QR code: {ex.Message}");
                    }

                    if (!eulaVerifier.ShowEulaDialog(qrCode))
                    {
                        MessageBox.Show(
                            "License acceptance is required to use this application.\n\n" +
                            "The application will now close.",
                            "License Not Accepted",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        return;
                    }
                }

                // Continue with the original program flow
                var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                var noexp_path = Path.Combine(path, "NOEXPIRE");
                bool foundNoExpire = File.Exists(noexp_path);

                // Use the settings from the XML config instead of looking for JSON
                var settings = eulaConfig.Settings;

                if (context.Patient == null || context.PlanSetup == null)
                {
                    MessageBox.Show("No active plan selected - exiting.");
                    return;
                }

                var asmCa = Assembly.GetExecutingAssembly().CustomAttributes.FirstOrDefault(ca => ca.AttributeType == typeof(AssemblyExpirationDate));
                DateTime exp;
                var provider = new CultureInfo("en-US");
                DateTime.TryParse(asmCa.ConstructorArguments.FirstOrDefault().Value as string, provider, DateTimeStyles.None, out exp);

                // Check exp date
                if (exp < DateTime.Now && !foundNoExpire)
                {
                    MessageBox.Show($"Application has expired. Newer builds with future expiration dates can be found here: {GITHUB_URL}");
                    return;
                }

                // Display opening msg
                string msg = $"The current ModulationComplexity application is provided AS IS as a non-clinical, research only tool in evaluation only. The current " +
                $"application will only be available until {exp.Date} after which the application will be unavailable. " +
                "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                $"Newer builds with future expiration dates can be found here: {GITHUB_URL}\n\n" +
                "See the FAQ for more information on how to remove this pop-up and expiration";

                string msg2 = $"Application will only be available until {exp.Date} after which the application will be unavailable. " +
                "By Clicking 'Yes' you agree that this application will be evaluated and not utilized in providing planning decision support\n\n" +
                $"Newer builds with future expiration dates can be found here: {GITHUB_URL} \n\n" +
                "See the FAQ for more information on how to remove this pop-up and expiration";

                if (!foundNoExpire)
                {
                    if (!settings.Validated)
                    {
                        // Show the first-time message
                        var res = MessageBox.Show(msg, "Agreement  ", MessageBoxButton.YesNo);
                        if (res == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                    else
                    {
                        // Show the returning user message
                        var res = MessageBox.Show(msg2, "Agreement  ", MessageBoxButton.YesNo);
                        if (res == MessageBoxResult.No)
                        {
                            return;
                        }
                    }
                }

                var mainWindow = new MainWindow(context, new MainViewModel(context, settings.Validated));
                mainWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}\n\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}