using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EasyBamlAddin.Services;
using EasyBamlAddin.Services.Settings;
using EasyBamlAddin.UidManagement;
using EasyBamlFormats.Resx;

namespace EasyBamlAddin.ViewModels
{
    public class TranslationFilesUpdater
    {
        private readonly IVisualStudioAdapter visualStudioAdapter;
        private readonly ISettingsService settingsService;
        //private readonly ILocalizabilityChecker localizabilityChecker;
        //private ManageUidOperation manageUidOperation;
        //private IUidUpdateHandleStrategy uidUpdateHandleStrategy;
        private BamlResourceCollector bamlResourceCollector;
        private IOperationProgressMonitor progress;

        public TranslationFilesUpdater(IVisualStudioAdapter visualStudioAdapter, ISettingsService settingsService, 
            BamlResourceCollector bamlResourceCollector, IOperationProgressMonitor progress)
        {
            this.visualStudioAdapter = visualStudioAdapter;
            this.settingsService = settingsService;
            this.bamlResourceCollector = bamlResourceCollector;
            this.progress = progress;
        }

        public void UpdateTranslationFiles()
        {
            progress.OperationDescription = "Updating translation files";
            progress.StepDescription = String.Format("Preparing files list...");
            progress.CurrentStep = 0;

            var projects = visualStudioAdapter.GetProjects().Where(projDescr => settingsService.IsProjectHandled(projDescr)).ToList();
            var settings = settingsService.GetSolutionSettings();
            var cultures = new List<string>();
            cultures.Add(settings.DevelepmentCulture);
            cultures.AddRange(settings.LocalizationCultures.Where(cul => cul != settings.DevelepmentCulture));
            progress.TotalSteps = projects.Count * cultures.Count;

            foreach (var projDescr in projects)
            {
                var resources = bamlResourceCollector.GetResources(projDescr);
                if (resources == null) continue; //Skip projects without localizable resources

                for (int i = 0; i < cultures.Count; i++)
                {
                    var culture = cultures[i];
                    bool isDevCulture = (i == 0);
                    string fileName = isDevCulture ? "Translate.resx" : String.Format("Translate.{0}.resx", culture);
                    progress.StepDescription = String.Format("Processing Project: {0}, File: {1}", projDescr.Name, fileName);

                    var resourceFile = visualStudioAdapter.GetTranslationFile(projDescr, fileName);

                    UpdateResourceFile(resources, resourceFile, isDevCulture, settings.DevelepmentCulture);
                }
            }

            progress.CurrentStep = progress.TotalSteps;
            progress.OperationDescription = "Updating translation files complete";
            progress.StepDescription = "";
        }

        private void UpdateResourceFile(List<BamlResourceEntry> resources, ResourceFile resourceFile, bool isDevCulture, string devCulture)
        {
            bool changed = false;
            foreach (var bamlResourceEntry in resources)
            {
                string resKey = BamlResourceResxWriter.GetResourceKey(bamlResourceEntry.BamlName, bamlResourceEntry.Key);

                if (resourceFile.Contains(resKey))
                {
                    //if (!isDevCulture)
                    //{
                    //    var value = resourceFile.GetStringValue(resKey);
                    //    var comment = resourceFile.GetComment(resKey);
                    //}
                }
                else
                {
                    resourceFile.SetResource(
                        resKey,
                        isDevCulture ? bamlResourceEntry.Resource.Content : "",
                        isDevCulture ? "" : String.Format("{0}:{1}", devCulture, bamlResourceEntry.Resource.Content));
                    changed = true;
                }
            }
            if (changed)
            {
                visualStudioAdapter.EnsureFileWritable(resourceFile.FileName);
                resourceFile.SaveFile();
            }
        }
    }
}
