using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml.Serialization;

namespace EasyBamlAddin.Services.Settings
{
    public class SettingsService : ISettingsService
    {
        public const string SettingsFolder = "BamlLocalization";
        public const string SettingsFile = "EasyBaml.settings";
        public const string LocalizabilitySettingsFile = "XamlLocalizabilitySettings.xml";

        private readonly IVisualStudioAdapter visualStudioAdapter;

        private bool globalSettingsLoaded;
        private bool settingsExistenceChecked;
        private SolutionSettings solutionSettings;
        private XamlLocalizabilitySettings globalLocalizabilitySettings;

        public SettingsService(IVisualStudioAdapter visualStudioAdapter)
        {
            this.visualStudioAdapter = visualStudioAdapter;
        }

        public bool IsSolutionSettingsExist
        {
            get { return CheckSettingsExists(); }
        }

        public SolutionSettings GetSolutionSettings()
        {
            CheckSettingsExists();
            return solutionSettings;
        }

        public bool SaveSolutionSettings(SolutionSettings settings)
        {
            string solutionFolder = visualStudioAdapter.GetSolutionFolder();
            if (String.IsNullOrEmpty(solutionFolder))
            {
                throw new InvalidOperationException("Solution folder in not specified");
            }

            string settingsFolder = Path.Combine(solutionFolder, SettingsFolder);
            if (!Directory.Exists(settingsFolder))
            {
                Directory.CreateDirectory(settingsFolder);
            }

            string settingsFile = Path.Combine(settingsFolder, SettingsFile);

            var res = SaveSettings(settings, settingsFile);
            if (res)
            {
                solutionSettings = settings;
                settingsExistenceChecked = true;
            }
            return res;
        }

        private bool CheckSettingsExists()
        {
            if (!settingsExistenceChecked)
            {
                string solutionFolder = visualStudioAdapter.GetSolutionFolder();
                if (String.IsNullOrEmpty(solutionFolder))
                {
                    throw new InvalidOperationException("Solution folder in not specified");
                }

                settingsExistenceChecked = true;

                string settingsFolder = Path.Combine(solutionFolder, SettingsFolder);
                if (!Directory.Exists(settingsFolder)) return false;

                string settingsFile = Path.Combine(settingsFolder, SettingsFile);
                if (!File.Exists(settingsFile)) return false;

                solutionSettings = TryLoadSettings(settingsFile);
            }
            return (solutionSettings != null);
        }

        private static SolutionSettings TryLoadSettings(string settingsFile)
        {
            try
            {
                using (var strm = File.OpenRead(settingsFile))
                {
                    var ser = new XmlSerializer(typeof (SolutionSettings));
                    var settings = (SolutionSettings) ser.Deserialize(strm);
                    return settings;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error loading settings file.\n" + e.Message, "Easy BAML");
                return null;
            }
        }

        private static bool SaveSettings(SolutionSettings settings, string settingsFile)
        {
            try
            {
                using (var strm = File.Create(settingsFile))
                {
                    var ser = new XmlSerializer(typeof (SolutionSettings));
                    ser.Serialize(strm, settings);
                    return true;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error saving settings file.\n" + e.Message, "Easy BAML");
                return false;
            }
        }

        public XamlLocalizabilitySettings GetGlobalLocalizabilitySettings()
        {
            if (globalLocalizabilitySettings == null && !globalSettingsLoaded)
            {
                globalLocalizabilitySettings = TryLoadGlobalLocalizabilitySettings();
                globalSettingsLoaded = true;
            }
            return globalLocalizabilitySettings;
        }

        private static XamlLocalizabilitySettings TryLoadGlobalLocalizabilitySettings()
        {
            try
            {
                string homeDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
                string settingsFile = Path.Combine(homeDir, LocalizabilitySettingsFile);

                using (var strm = File.OpenRead(settingsFile))
                {
                    var ser = new XmlSerializer(typeof(XamlLocalizabilitySettings));
                    var settings = (XamlLocalizabilitySettings)ser.Deserialize(strm);
                    return settings;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error loading global localizability settings file.\n" + e.Message, "Easy BAML");
                return null;
            }
        }

        public bool IsProjectHandled(ProjectDescription projectDescription)
        {
            GetSolutionSettings();
            if (solutionSettings == null || solutionSettings.ProjectsSettings == null) return false;
            
            foreach (var projectSetting in solutionSettings.ProjectsSettings)
            {
                if (projectSetting.ProjectUniqueName == projectDescription.UniqueName)
                {
                    return projectSetting.Localizable;
                }
            }
            return false;
        }

    }
}
