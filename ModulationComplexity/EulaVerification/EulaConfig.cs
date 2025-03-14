using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace MAAS.Common.EulaVerification
{
    /// <summary>
    /// Configuration class for storing EULA acceptance information
    /// </summary>
    public class EulaConfig
    {
        // Dictionary to store accepted EULAs
        public Dictionary<string, string> AcceptedEulas { get; set; } = new Dictionary<string, string>();

        // Path to the config file - stored in user's local app data
        // Changed from readonly to allow deserialization to modify it
        private string _configPath;

        public string ConfigPath
        {
            get { return _configPath; }
            set { _configPath = value; }
        }

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
                "MAAS", projectFolder, "EulaConfig.xml");

            EulaConfig config = null;

            try
            {
                if (File.Exists(configPath))
                {
                    var serializer = new XmlSerializer(typeof(EulaConfig));
                    using (var stream = File.OpenRead(configPath))
                    {
                        config = (EulaConfig)serializer.Deserialize(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading EULA config: {ex.Message}");
            }

            if (config == null)
            {
                config = new EulaConfig();
            }

            // Always set the config path
            config.ConfigPath = configPath;

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
                Directory.CreateDirectory(Path.GetDirectoryName(_configPath));

                var serializer = new XmlSerializer(typeof(EulaConfig));
                using (var stream = File.Create(_configPath))
                {
                    serializer.Serialize(stream, this);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving EULA config: {ex.Message}");
            }
        }
    }
}