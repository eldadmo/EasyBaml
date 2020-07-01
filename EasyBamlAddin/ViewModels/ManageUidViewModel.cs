using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using EasyBamlAddin.Services;
using EasyBamlAddin.Services.Settings;
using EasyBamlAddin.Tools;
using EasyBamlAddin.UidManagement;
using EasyBamlFormats.Resx;

namespace EasyBamlAddin.ViewModels
{
    public class ManageUidViewModel : OperationProgressViewModel
    {
        private readonly IVisualStudioAdapter visualStudioAdapter;
        private readonly ISettingsService settingsService;
        private readonly ILocalizabilityChecker localizabilityChecker;
        private ManageUidOperation manageUidOperation;
        private IUidUpdateHandleStrategy uidUpdateHandleStrategy;
        private BamlResourceCollector bamlResourceCollector;

        public ManageUidViewModel(IVisualStudioAdapter visualStudioAdapter, ISettingsService settingsService,
            ILocalizabilityChecker localizabilityChecker)
        {
            this.visualStudioAdapter = visualStudioAdapter;
            this.settingsService = settingsService;
            this.localizabilityChecker = localizabilityChecker;

            CheckUidsCommand = new DelegateCommand(CheckUidsHandler);
            UpdateUidsCommand = new DelegateCommand(UpdateUidsHandler);
            RemoveUidsCommand = new DelegateCommand(RemoveUidsHandler);
            UpdateTranslationFilesCommand = new DelegateCommand(UpdateTranslationFilesHandler);
            PrepareTranslationCommand = new DelegateCommand(PrepareTranslationHandler);

            WindowTitle = "Easy BAML - Manage Uid";
        }

        public ManageUidOperation ManageUidOperation
        {
            get { return manageUidOperation; }
            set
            {
                if (manageUidOperation != value)
                {
                    manageUidOperation = value;
                    OnPropertyChanged("ManageUidOperation");
                }
            }
        }

        private ObservableCollection<XamlFileViewModel> invalidFiles;

        public ObservableCollection<XamlFileViewModel> InvalidFiles
        {
            get { return invalidFiles; }
            set
            {
                if (invalidFiles != value)
                {
                    invalidFiles = value;
                    OnPropertyChanged("InvalidFiles");
                }
            }
        }


        public DelegateCommand CheckUidsCommand { get; private set; }
        public DelegateCommand UpdateUidsCommand { get; private set; }
        public DelegateCommand RemoveUidsCommand { get; private set; }
        public DelegateCommand UpdateTranslationFilesCommand { get; private set; }
        public DelegateCommand PrepareTranslationCommand { get; private set; }

        private void CheckUidsHandler(object obj)
        {
            if (IsRunning) return;
            ManageUidOperation = ManageUidOperation.CheckUid;
            AsyncStartOperation(ManageUid);
        }

        private void UpdateUidsHandler(object obj)
        {
            if (IsRunning) return;
            ManageUidOperation = ManageUidOperation.UpdateUid;
            AsyncStartOperation(ManageUid);
        }

        private void RemoveUidsHandler(object obj)
        {
            if (IsRunning) return;
            ManageUidOperation = ManageUidOperation.RemoveUid;
            AsyncStartOperation(ManageUid);
        }

        private void UpdateTranslationFilesHandler(object obj)
        {
            if (IsRunning) return;
            ManageUidOperation = ManageUidOperation.PrepareTranslation;
            AsyncStartOperation(DoUpdateTranslationFiles);
        }

        private void DoUpdateTranslationFiles()
        {
            bamlResourceCollector = new BamlResourceCollector();
            ManageUid();
            UpdateTranslationFiles();
        }

        private void UpdateTranslationFiles()
        {
            var updater = new TranslationFilesUpdater(visualStudioAdapter, settingsService, bamlResourceCollector, this);
            updater.UpdateTranslationFiles();
        }

        private void PrepareTranslationHandler(object obj)
        {
            if (IsRunning) return;
            AsyncStartOperation(DoPrepareTranslation);
        }

        public void DoPrepareTranslation()
        {
            ManageUidOperation = ManageUidOperation.UpdateUid;
            ManageUid();
            ManageUidOperation = ManageUidOperation.PrepareTranslation;
            DoUpdateTranslationFiles();
            OperationDescription = "Update UIDs and translation files complete";
        }

        private string solutionFolder;

        private void ManageUid()
        {
            OperationDescription = "Performing " + ManageUidOperation;
            StepDescription = String.Format("Preparing files list...");

            solutionFolder = visualStudioAdapter.GetSolutionFolder();
            //solutionSettings = settingsService.GetSolutionSettings();

            var xamlFiles = new List<XamlFileDescription>();
            foreach (var projDescr in visualStudioAdapter.GetProjects())
            {
                if (settingsService.IsProjectHandled(projDescr))
                {
                    xamlFiles.AddRange(visualStudioAdapter.GetXamlFiles(projDescr));
                }
            }

            if (xamlFiles.Count == 0)
            {
                MessageBox.Show("No .xaml files in selected projects", "Easy BAML");
                return;
            }

            uidUpdateHandleStrategy = new DefaultUidUpdateHandleStrategy(ManageUidOperation == ManageUidOperation.RemoveUid);

            InvalidFiles = new ObservableCollection<XamlFileViewModel>();
            TotalSteps = xamlFiles.Count;
            CurrentStep = 0;
            foreach (var xamlFileDescription in xamlFiles)
            {
                UpdateStepDescription(xamlFileDescription);
                ManageFileUids(xamlFileDescription);
                CurrentStep++;
            }
            CurrentStep = TotalSteps;
            OperationDescription = "Completed: " + ManageUidOperation;
            StepDescription = "";
        }

        private void UpdateStepDescription(XamlFileDescription xamlFileDescription)
        {
            var fileName = xamlFileDescription.Name;
            if (fileName.StartsWith(solutionFolder, StringComparison.OrdinalIgnoreCase))
            {
                fileName = fileName.Substring(solutionFolder.Length + 1);
            }
            StepDescription = String.Format("Processing file: {0}", fileName);
        }

        private void ManageFileUids(XamlFileDescription xamlFileDescription)
        {
            using (var textReader = visualStudioAdapter.GetXamlFileContent(xamlFileDescription))
            {
                var uidCollector = new UidManager(xamlFileDescription.Name, textReader, localizabilityChecker).ParseFileSmart();

                if (uidCollector.Count > 0)
                {
                    switch (ManageUidOperation)
                    {
                        case ManageUidOperation.CheckUid:
                            var fvm = new XamlFileViewModel(solutionFolder, xamlFileDescription, uidCollector);
                            ThreadTools.InvokeInUIThread(Window, () => InvalidFiles.Add(fvm));
                            break;
                        case ManageUidOperation.UpdateUid:
                            if (!uidCollector.AllAreValid())
                            {
                                //TODO: resolve duplicates

                                UpdateFileUids(xamlFileDescription, uidCollector);

                                var fvm1 = new XamlFileViewModel(solutionFolder, xamlFileDescription, null);
                                ThreadTools.InvokeInUIThread(Window, () => InvalidFiles.Add(fvm1));
                            }
                            break;
                        case ManageUidOperation.RemoveUid:
                            if (!uidCollector.AllAreAbsent())
                            {
                                UpdateFileUids(xamlFileDescription, uidCollector);
                                var fvm2 = new XamlFileViewModel(solutionFolder, xamlFileDescription, null);
                                ThreadTools.InvokeInUIThread(Window, () => InvalidFiles.Add(fvm2));
                            }
                            break;
                        case ManageUidOperation.PrepareTranslation:
                            bamlResourceCollector.Add(xamlFileDescription, uidCollector);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        private void UpdateFileUids(XamlFileDescription xamlFileDescription, UidCollector uidCollector)
        {
            CorrectUids(uidCollector);

            using (var textReader = visualStudioAdapter.GetXamlFileContent(xamlFileDescription))
            {
                var target = new StringWriter();
                var uidWriter = new UidWriter(uidCollector, textReader, target);
                if (uidWriter.UpdateUidWrite(uidUpdateHandleStrategy))
                {
                    visualStudioAdapter.SetXamlFileContent(xamlFileDescription, target.ToString());
                }
                else
                {
                    //TODO: notify failure
                }
            }
        }

        private void CorrectUids(UidCollector uidCollector)
        {
            if (ManageUidOperation != ManageUidOperation.UpdateUid) return;

            uidCollector.ResolveUidErrors();
        }
    }

    public enum ManageUidOperation
    {
        CheckUid,
        UpdateUid,
        RemoveUid,
        PrepareTranslation
    }

    public class XamlFileViewModel
    {
        public XamlFileViewModel(string solutionFolder, XamlFileDescription xamlFileDescription, 
            UidCollector uidCollector)
        {
            string fileName = xamlFileDescription.Name;
            Name = Path.GetFileName(fileName);

            if (fileName.StartsWith(solutionFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                SolutionPath = fileName.Substring(solutionFolder.Length+1);
            }
            else
            {
                SolutionPath = fileName;
            }

            ProjectName = xamlFileDescription.ProjectDescription.Name;

            string projFolder = Path.GetDirectoryName(xamlFileDescription.ProjectDescription.FullName);
            if (fileName.StartsWith(projFolder, StringComparison.InvariantCultureIgnoreCase))
            {
                ProjectPath = fileName.Substring(projFolder.Length + 1);
            }
            else
            {
                ProjectPath = fileName;
            }

            if (uidCollector != null)
            {
                UidEntries = new List<UidEntryViewModel>(uidCollector.Count);
                for (int i = 0; i < uidCollector.Count; i++)
                {
                    var uid = uidCollector[i];
                    UidEntries.Add(new UidEntryViewModel(uid));
                }
             
                AllAreValid = uidCollector.AllAreValid();
            }
        }

        public string SolutionPath { get; private set; }

        public string ProjectPath { get; private set; }

        public string ProjectName { get; private set; }

        public string Name { get; private set; }

        public IList<UidEntryViewModel> UidEntries { get; private set; }

        public bool AllAreValid { get; private set; }
    }

    public class UidEntryViewModel
    {
        public UidEntryViewModel(Uid uid)
        {
            Line = uid.LineNumber;
            Column = uid.LinePosition;
            ElementName = uid.ElementName;
            Status = uid.Status;
            LocalizableString = uid.LocalizableString;
            Value = uid.Value;

            if (uid.Entries != null && uid.Entries.Count > 0)
            {
                AttrName = uid.Entries[0].Name;
            }
        }

        public int Line { get; private set; }

        public int Column { get; private set; }

        public string ElementName { get; private set; }

        public string Message { get; private set; }

        public UidStatus Status { get; private set; }

        public string LocalizableString { get; private set; }

        public string Value { get; private set; }

        public string AttrName { get; private set; }
    }
}
