using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace MAAS.Common.EulaVerification
{
    /// <summary>
    /// Configuration class for storing EULA acceptance information with version support
    /// </summary>
    public class EulaConfig
    {
        // Dictionary to store accepted EULAs with versions
        // Key: ProjectName-Version, Value: Access code
        public Dictionary<string, string> AcceptedEulas { get; set; } = new Dictionary<string, string>();

        // Path to the config file - not serialized
        [JsonIgnore]
        private string _configPath;

        // Default constructor for serialization
        public EulaConfig()
        {
        }

        // Constructor with config path
        public EulaConfig(string configPath)
        {
            _configPath = configPath;
        }

        /// <summary>
        /// Load the configuration from disk
        /// </summary>
        public static EulaConfig Load(string projectFolder)
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MAAS", projectFolder, "EulaConfig.json");

            EulaConfig config = new EulaConfig(configPath);

            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var loadedConfig = JsonConvert.DeserializeObject<EulaConfig>(json);

                    if (loadedConfig != null)
                    {
                        config.AcceptedEulas = loadedConfig.AcceptedEulas;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading EULA config: {ex.Message}");
            }

            return config;
        }

        /// <summary>
        /// Save the configuration to disk
        /// </summary>
        public void Save()
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(_configPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Serialize to JSON
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);

                // Write to file
                File.WriteAllText(_configPath, json, Encoding.UTF8);

                // Verify the file was created
                if (File.Exists(_configPath))
                {
                    System.Diagnostics.Debug.WriteLine($"Successfully saved EULA config to: {_configPath}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to save EULA config to: {_configPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving EULA config: {ex.Message}");
                // Add more detailed error information
                System.Diagnostics.Debug.WriteLine($"Path: {_configPath}");
                System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");
            }
        }
    }
}