using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace MAAS.Common.EulaVerification
{
    [Serializable]
    public class EulaConfig
    {
        public EulaConfig()
        {
            EulaEntries = new List<EulaEntry>();
            Settings = new ApplicationSettings(); // Initialize settings
        }

        // Constructor with config path
        public EulaConfig(string configPath)
        {
            _configPath = configPath;
            EulaEntries = new List<EulaEntry>();
            Settings = new ApplicationSettings(); // Initialize settings
        }

        [XmlArray("AcceptedEulas")]
        [XmlArrayItem("EulaEntry")]
        public List<EulaEntry> EulaEntries { get; set; }

        // Add the ApplicationSettings property
        [XmlElement("ApplicationSettings")]
        public ApplicationSettings Settings { get; set; }

        [XmlIgnore]
        private Dictionary<string, string> _acceptedEulasDict;

        [XmlIgnore]
        private string _configPath;

        [XmlIgnore]
        public Dictionary<string, string> AcceptedEulas
        {
            get
            {
                if (_acceptedEulasDict == null)
                {
                    _acceptedEulasDict = new Dictionary<string, string>();

                    // Only populate if EulaEntries exists and has items
                    if (EulaEntries != null)
                    {
                        foreach (var entry in EulaEntries)
                        {
                            _acceptedEulasDict[entry.Key] = entry.Value;
                        }
                    }
                }
                return _acceptedEulasDict;
            }
            set
            {
                _acceptedEulasDict = value;

                // Make sure EulaEntries exists
                if (EulaEntries == null)
                {
                    EulaEntries = new List<EulaEntry>();
                }
                else
                {
                    EulaEntries.Clear();
                }

                // Only process if we have a dictionary with entries
                if (_acceptedEulasDict != null)
                {
                    foreach (var kvp in _acceptedEulasDict)
                    {
                        EulaEntries.Add(new EulaEntry { Key = kvp.Key, Value = kvp.Value });
                    }
                }
            }
        }

        public static EulaConfig Load(string projectFolder)
        {
            // Get config path in assembly directory
            string assemblyDir = GetExecutingDirectory();
            string configPath = Path.Combine(assemblyDir, $"{projectFolder}_EulaConfig.xml");

            EulaConfig config = new EulaConfig(configPath);

            if (File.Exists(configPath))
            {
                try
                {
                    XmlSerializer serializer = new XmlSerializer(typeof(EulaConfig));
                    using (FileStream fs = new FileStream(configPath, FileMode.Open))
                    {
                        var loadedConfig = (EulaConfig)serializer.Deserialize(fs);
                        if (loadedConfig != null)
                        {
                            // Transfer entries to our new config
                            config.EulaEntries = loadedConfig.EulaEntries ?? new List<EulaEntry>();

                            // Transfer settings if they exist
                            if (loadedConfig.Settings != null)
                            {
                                config.Settings = loadedConfig.Settings;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error loading config: {ex.Message}");
                }
            }

            return config;
        }

        private static string GetExecutingDirectory()
        {
            try
            {
                return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }
            catch
            {
                return Directory.GetCurrentDirectory();
            }
        }

        public bool Save()
        {
            try
            {
                // Modified to save even if there are no EULA entries but we have settings
                if ((EulaEntries == null || EulaEntries.Count == 0) &&
                    Settings == null)
                {
                    System.Diagnostics.Debug.WriteLine("No data to save!");
                    return false;
                }

                // Create directory if needed
                string directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Simple approach to save the XML
                XmlSerializer serializer = new XmlSerializer(typeof(EulaConfig));
                using (FileStream fs = new FileStream(_configPath, FileMode.Create))
                {
                    serializer.Serialize(fs, this);
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving config: {ex.Message}");
                return false;
            }
        }
    }

    [Serializable]
    public class EulaEntry
    {
        [XmlAttribute("ProjectVersionKey")]
        public string Key { get; set; }

        [XmlAttribute("AccessCode")]
        public string Value { get; set; }
    }

    // Add this new class for application settings
    [Serializable]
    public class ApplicationSettings
    {
        public ApplicationSettings()
        {
            // Initialize defaults
            Validated = false;
            EULAAgreed = false;
        }

        [XmlAttribute("Validated")]
        public bool Validated { get; set; }

        [XmlAttribute("EULAAgreed")]
        public bool EULAAgreed { get; set; }
    }
}