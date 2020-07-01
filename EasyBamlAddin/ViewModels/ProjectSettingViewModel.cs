using System;
using EasyBamlAddin.Domain;

namespace EasyBamlAddin.ViewModels
{
    public class ProjectSettingViewModel : BaseViewModel
    {
        private string uniqueName;
        private string projectName;
        private bool isWpfProject;
        private bool localizable;

        public string UniqueName
        {
            get { return uniqueName; }
            set
            {
                if (uniqueName != value)
                {
                    uniqueName = value;
                    OnPropertyChanged("UniqueName");
                }
            }
        }

        public string ProjectName
        {
            get { return projectName; }
            set
            {
                if (projectName != value)
                {
                    projectName = value;
                    OnPropertyChanged("ProjectName");
                }
            }
        }

        public bool IsWpfProject
        {
            get { return isWpfProject; }
            set
            {
                if (isWpfProject != value)
                {
                    isWpfProject = value;
                    OnPropertyChanged("IsWpfProject");
                }
            }
        }

        public bool Localizable
        {
            get { return localizable; }
            set
            {
                if (localizable != value)
                {
                    localizable = value;
                    OnPropertyChanged("Localizable");
                }
            }
        }
    }
}
