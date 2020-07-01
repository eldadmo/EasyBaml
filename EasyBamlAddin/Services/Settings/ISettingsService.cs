using System.Collections.Generic;
using EasyBamlAddin.Domain;

namespace EasyBamlAddin.Services.Settings
{
    public interface ISettingsService
    {
        bool IsSolutionSettingsExist { get; }

        SolutionSettings GetSolutionSettings();

        bool SaveSolutionSettings(SolutionSettings solutionSettings);

        XamlLocalizabilitySettings GetGlobalLocalizabilitySettings();

        bool IsProjectHandled(ProjectDescription projectDescription);
    }

    public class ProjectSettings
    {
        public string ProjectUniqueName { get; set; }
        
        public bool Localizable { get; set; }        
    }

    public class SolutionSettings
    {
        public UidGenerationMode UidGenerationMode { get; set; }

        public List<ProjectSettings> ProjectsSettings { get; set; }

        public string DevelepmentCulture { get; set; }

        public List<string> LocalizationCultures { get; set; }
    }
}
