using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using EasyBamlAddin.Domain;
using EasyBamlAddin.Services;
using EasyBamlAddin.Services.Settings;
using EasyBamlAddin.Tools;
using EasyBamlAddin.Views;

namespace EasyBamlAddin.ViewModels
{
    public class SettingsViewModel : BaseWindowAwareViewModel
    {
        private const string DefaultDevLanguage = "en-US";

        private readonly IVisualStudioAdapter visualStudioAdapter;
        private readonly ISettingsService settingsService;

        private UidGenerationMode uidGenerationMode;
        private List<ProjectSettingViewModel> projectsSettings;
        private List<CultureItemViewModel> allLanguages;
        private CultureItemViewModel defaultLanguage;
        private ObservableCollection<CultureItemViewModel> availableLanguages;
        private ObservableCollection<CultureItemViewModel> selectedLanguages;
        private SolutionSettings settings;
        private OperationProgressViewModel progressMonitor;

        public SettingsViewModel(IVisualStudioAdapter visualStudioAdapter, ISettingsService settingsService)
        {
            this.visualStudioAdapter = visualStudioAdapter;
            this.settingsService = settingsService;

            AddLanguagesCommand = new DelegateCommand(AddLanguages);
            RemoveLanguagesCommand = new DelegateCommand(RemoveLanguages);
            CancelCommand = new DelegateCommand(_ => Close());
            SaveCommand = new DelegateCommand(SaveSettings);

            DefaultLanguage = new CultureItemViewModel {Name = DefaultDevLanguage};
            FillAvailableLanguages();

            FillProjectSettings();

            LoadSettings();
        }

        public UidGenerationMode UidGenerationMode
        {
            get { return uidGenerationMode; }
            set
            {
                if (uidGenerationMode != value)
                {
                    uidGenerationMode = value;
                    OnPropertyChanged("UidGenerationMode");
                }
            }
        }

        public List<ProjectSettingViewModel> ProjectsSettings
        {
            get { return projectsSettings; }
            set
            {
                if (projectsSettings != value)
                {
                    projectsSettings = value;
                    OnPropertyChanged("ProjectsSettings");
                }
            }
        }

        public DelegateCommand AddLanguagesCommand { get; private set;}

        public DelegateCommand RemoveLanguagesCommand { get; private set; }

        public DelegateCommand CancelCommand { get; private set; }

        public DelegateCommand SaveCommand { get; private set; }

        public CultureItemViewModel DefaultLanguage
        {
            get { return defaultLanguage; }
            set
            {
                if (defaultLanguage != value)
                {
                    defaultLanguage = value;
                    OnPropertyChanged("DefaultLanguage");
                }
            }
        }

        public List<CultureItemViewModel> AllLanguages
        {
            get { return allLanguages; }
            set
            {
                if (allLanguages != value)
                {
                    allLanguages = value;
                    OnPropertyChanged("AllLanguages");
                }
            }
        }

        public ObservableCollection<CultureItemViewModel> AvailableLanguages
        {
            get { return availableLanguages; }
            set
            {
                if (availableLanguages != value)
                {
                    availableLanguages = value;
                    OnPropertyChanged("AvailableLanguages");
                }
            }
        }

        public ObservableCollection<CultureItemViewModel> SelectedLanguages
        {
            get { return selectedLanguages; }
            set
            {
                if (selectedLanguages != value)
                {
                    selectedLanguages = value;
                    OnPropertyChanged("SelectedLanguages");
                }
            }
        }

        public IList SelectedAvailableLanguages { get; set; }

        public IList SelectedSelectedLanguages { get; set; }

        private void FillAvailableLanguages()
        {
            // get culture names
            var list = new List<CultureItemViewModel>();
            foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                if (!String.IsNullOrEmpty(ci.Name))
                {
                    list.Add(new CultureItemViewModel {Name = ci.Name});
                }
            }
            list.Sort();  // sort by name
            AllLanguages = list;

            AvailableLanguages = new ObservableCollection<CultureItemViewModel>(AllLanguages);

            SelectedLanguages = new ObservableCollection<CultureItemViewModel>();

            var selLangsView = (CollectionView) CollectionViewSource.GetDefaultView(SelectedLanguages);
            selLangsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
        }

        private void AddLanguages(object obj)
        {
            if (SelectedAvailableLanguages != null && SelectedAvailableLanguages.Count > 0)
            {
                var selection = new ArrayList(SelectedAvailableLanguages);
                foreach (CultureItemViewModel l in selection)
                {
                    if (!SelectedLanguages.Contains(l))
                    {
                        SelectedLanguages.Add(l);
                    }
                }
            }
        }

        private void RemoveLanguages(object obj)
        {
            if (SelectedSelectedLanguages != null && SelectedSelectedLanguages.Count > 0)
            {
                var selection = new ArrayList(SelectedSelectedLanguages);
                foreach (CultureItemViewModel l in selection)
                {
                    SelectedLanguages.Remove(l);
                }                
            }
        }

        private void FillProjectSettings()
        {
            projectsSettings = new List<ProjectSettingViewModel>();

            foreach (var projDescr in visualStudioAdapter.GetProjects())
            {
                var projSettings = new ProjectSettingViewModel();
                projSettings.UniqueName = projDescr.UniqueName;
                projSettings.ProjectName = projDescr.Name;
                projSettings.IsWpfProject = (projDescr.WpfProjectType == WpfProjectType.Wpf); 
                projSettings.Localizable = projSettings.IsWpfProject;
                projectsSettings.Add(projSettings);
            }
        }


        private void LoadSettings()
        {
            settings = settingsService.GetSolutionSettings();
            if (settings == null) return;

            UidGenerationMode = settings.UidGenerationMode;
            if (!String.IsNullOrEmpty(settings.DevelepmentCulture))
            {
                DefaultLanguage = new CultureItemViewModel(settings.DevelepmentCulture);
            }

            if (settings.LocalizationCultures != null)
            {
                foreach (var cult in settings.LocalizationCultures)
                {
                    SelectedLanguages.Add(new CultureItemViewModel(cult));
                }
            }

            foreach (var projSettingVM in projectsSettings)
            {
                if (settings.ProjectsSettings != null)
                {
                    var projSetting =
                        settings.ProjectsSettings.FirstOrDefault(
                            ps => ps.ProjectUniqueName == projSettingVM.UniqueName);
                    if (projSetting != null)
                    {
                        projSettingVM.Localizable = projSetting.Localizable;
                    }
                }
            }
        }

        private void SaveSettings(object obj)
        {
            if (MessageBox.Show(Window, "Easy BAML is about to make changes in selected projects: set default UI culture, add localization build steps and add translation files.\nOperation may take several minutes.\nAfter it complete, selected project(s) will be build with localization.", "Easy BAML", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
            {
                return;
            }

            if (settings == null)
            {
                settings = new SolutionSettings();
            }
            
            settings.UidGenerationMode = UidGenerationMode;
            settings.DevelepmentCulture = DefaultLanguage.Name;

            var selCults = SelectedLanguages.Select(lang => lang.Name).ToList();
            settings.LocalizationCultures = selCults;

            if (settings.ProjectsSettings == null) settings.ProjectsSettings = new List<ProjectSettings>();
            foreach (var projSettingVM in projectsSettings)
            {
                var projSetting = settings.ProjectsSettings.FirstOrDefault(
                         ps => ps.ProjectUniqueName == projSettingVM.UniqueName);
                if (projSetting == null)
                {
                    projSetting = new ProjectSettings {ProjectUniqueName = projSettingVM.UniqueName};
                    settings.ProjectsSettings.Add(projSetting);
                }
                projSetting.Localizable = projSettingVM.Localizable;                
            }

            if (settingsService.SaveSolutionSettings(settings))
            {
                if (ConfigureSolution())
                {
                    Close(true);
                }
            }
        }

        private bool ConfigureSolution()
        {
            try
            {
                progressMonitor = new OperationProgressViewModel();
                progressMonitor.WindowTitle = "Easy BAML - Configuring Solution";
                progressMonitor.Operation = DoConfigureSolution;
                progressMonitor.OperationDescription = "Configuring solution projects";
                ViewTools.ShowModalWindow<OperationProgressView>(progressMonitor, Window);

                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show("Error configuring solution: " + e.Message, "Easy BAML");
                return false;
            }
        }

        private void DoConfigureSolution()
        {
            visualStudioAdapter.ConfigureSolution();

            var projects = visualStudioAdapter.GetProjects();
            progressMonitor.TotalSteps = projects.Count * 2;

            foreach (var projDescr in projects)
            {
                var projSettings = settings.ProjectsSettings.FirstOrDefault(p => p.ProjectUniqueName == projDescr.UniqueName);
                bool localize = (projSettings != null && projSettings.Localizable);

                progressMonitor.StepDescription = "Configuring project " + projDescr.Name;
                visualStudioAdapter.ConfigureProject(projDescr, settings.DevelepmentCulture, localize);
                progressMonitor.CurrentStep++;
            }

            //Need to refresh projects list, because it is invalidadet after projects reload
            projects = visualStudioAdapter.GetProjects();
            foreach (var projDescr in projects)
            {
                var projSettings = settings.ProjectsSettings.FirstOrDefault(p => p.ProjectUniqueName == projDescr.UniqueName);
                bool localize = (projSettings != null && projSettings.Localizable);

                progressMonitor.StepDescription = "Adding translation files in project " + projDescr.Name;
                visualStudioAdapter.ConfigureTranslationFiles(projDescr, settings.DevelepmentCulture, settings.LocalizationCultures, localize);
                progressMonitor.CurrentStep++;
            }

            progressMonitor.CurrentStep = progressMonitor.TotalSteps;
            progressMonitor.OperationDescription = "Configuring solution projects complete";
            progressMonitor.StepDescription = "";
        }
    }
}
