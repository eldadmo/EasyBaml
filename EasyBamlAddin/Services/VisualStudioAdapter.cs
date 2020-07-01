using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Xml;
using EasyBamlAddin.Services.Settings;
using EasyBamlFormats.Resx;
using EnvDTE;
using EnvDTE80;
using Window = EnvDTE.Window;

namespace EasyBamlAddin.Services
{
    public class VisualStudioAdapter : IVisualStudioAdapter
    {
        private const string SilverlightExtenderName = "SilverlightProject";
        private const string ImportedEasyBamlTargetFile = @"$(SolutionDir)BamlLocalization\EasyLocBaml.targets";
        private const string SolutionLocalizationFolder = "BamlLocalization";
        private const string ProjectTranslationFolder = "Translation";
        private const string TranslationFilePrefix = "Translate";
        private const string TranslationFileSuffix = ".resx";
        private const string MSBuildNS = "http://schemas.microsoft.com/developer/msbuild/2003";

        private readonly DTE2 applicationObject;

        public VisualStudioAdapter(DTE2 applicationObject)
        {
            this.applicationObject = applicationObject;
            ValidateCompatibility();

            //DebugProjectsInfo();
            //DebugSolutionHierarchy();
        }

        #region IVisualStudioAdapter Members

        public string GetSolutionFolder()
        {
            string solutionFile = applicationObject.Solution.FullName;
            if (String.IsNullOrEmpty(solutionFile)) return null;
            return Path.GetDirectoryName(solutionFile);
        }

        public IList<ProjectDescription> GetProjects()
        {
            var result = new List<ProjectDescription>();
            foreach (Project project in applicationObject.Solution.Projects)
            {
                if (project.Kind == Constants.vsProjectKindMisc) continue;

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    AddSolutionFolderProjects(project, result);
                }
                else
                {
                    result.Add(CreateProjectDescription(project));
                }
            }
            return result;
        }

        public IList<XamlFileDescription> GetXamlFiles(ProjectDescription projectDescription)
        {
            Project project = GetProject(projectDescription);

            var res = new List<XamlFileDescription>();
            ProjectItems projectItems = project.ProjectItems;

            IterateProjectXamls(projectItems,
                                (fn, projItem) => res.Add(CreateXamlFileDescription(fn, projectDescription, projItem)));

            return res;
        }

        public TextReader GetXamlFileContent(XamlFileDescription xamlFile)
        {
            ProjectItem projectItem = GetProjectItem(xamlFile);

            if (projectItem.Document != null)
            {
                return new StringReader(GetDocumentText(projectItem.Document));
            }

            return File.OpenText(xamlFile.Name);
        }

        public void SetXamlFileContent(XamlFileDescription xamlFile, string content)
        {
            ProjectItem projectItem = GetProjectItem(xamlFile);

            if (projectItem.Document == null)
            {
                Window win = projectItem.Open(Constants.vsViewKindTextView);
                win.Visible = true;
            }

            SetDocumentText(projectItem.Document, content);
        }

        public bool IsProjectSaved(ProjectDescription projectDescription)
        {
            Project project = GetProject(projectDescription);
            return project.Saved;
        }

        public bool SaveProject(ProjectDescription projectDescription)
        {
            Project project = GetProject(projectDescription);
            project.Save();
            return project.Saved;
        }

        public string GetProjectUICulture(ProjectDescription projectDescription)
        {
            Project project = GetProject(projectDescription);
            var projDoc = new XmlDocument();
            projDoc.Load(project.FullName);

            var nsmgr = GetNSManager(projDoc);

            foreach (XmlElement cultNode in projDoc.SelectNodes("/b:Project/b:PropertyGroup/b:UICulture", nsmgr))
            {
                var propGroupNode = (XmlElement)cultNode.ParentNode;
                string cond = propGroupNode.GetAttribute("Condition");
                if (!String.IsNullOrEmpty(cond)) continue; //Skip conditional property

                string cult = cultNode.InnerText;
                if (!String.IsNullOrEmpty(cult)) return cult;
            }
            return null;
        }

        public void ConfigureProject(ProjectDescription projectDescription, string uiCulture, bool importTarget)
        {
            Project project = GetProject(projectDescription);

            var projDoc = new XmlDocument();
            var projectFileName = project.FullName;
            projDoc.Load(projectFileName);
            bool anyChanges = false;

            if (importTarget)
            {
                SetFallbackCulture(project, uiCulture);

                SetProjectUICulture(projDoc, uiCulture, ref anyChanges);
            }
            ConfigureEasyBamlTarget(projDoc, importTarget, ref anyChanges);

            if (anyChanges)
            {
                if (!project.Saved)
                {
                    Confirm("Project {0} need to be saved before continue", project.Name);
                    project.Save();
                }

                EnsureFileWritable(projectFileName); //Should be done before UnloadProject, 
                                                     //otherwise VS reports it as not under source control

                UnloadProject(project);

                projDoc.Save(projectFileName);

                ReloadProject();
                //MessageBox.Show("press any key to continue", "Easy BAML");
            }
        }

        private void UnloadProject(Project project)
        {
            SelectProject(project.UniqueName);

            applicationObject.ExecuteCommand("Project.UnloadProject");
        }

        private void ReloadProject()
        {
            //Project already selected
            //SelectProject(project.UniqueName);

            applicationObject.ExecuteCommand("Project.ReloadProject");
        }

        private void SelectProject(string projectUniqueName)
        {
            var uihier = (UIHierarchy)applicationObject.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
            uihier.Parent.Activate(); //Activate solution explorer window to ensure command 'Unload' will be available

            if (!SelectProject(uihier.UIHierarchyItems, projectUniqueName))
            {
                throw new Exception(String.Format("Cannot find project {0} in solution hierarchy", projectUniqueName));
            }
        }

        private static bool SelectProject(UIHierarchyItems items, string projectUniqueName)
        {
            foreach (UIHierarchyItem item in items)
            {
                bool canContainProject = false;
                var obj = item.Object;
                if (obj is Solution)
                {
                    canContainProject = true;
                }
                else if (obj is Project)
                {
                    var project = (Project)obj;
                    if (project.Kind == Constants.vsProjectKindMisc)
                    {
                    }
                    else if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        canContainProject = true;
                    }
                    else if (project.UniqueName == projectUniqueName)
                    {
                        item.Select(vsUISelectionType.vsUISelectionTypeSelect);
                        return true;
                    }
                }
                else if (obj is ProjectItem)
                {
                    var projItem = (ProjectItem)obj;
                    if (projItem.SubProject != null)
                    {
                        var project = projItem.SubProject;
                        if (project.UniqueName == projectUniqueName)
                        {
                            item.Select(vsUISelectionType.vsUISelectionTypeSelect);
                            return true;
                        }
                    }
                }

                if (canContainProject)
                {
                    if (SelectProject(item.UIHierarchyItems, projectUniqueName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private static void Confirm(string messageFmt, params object[] args)
        {
            if (MessageBox.Show(String.Format(messageFmt, args), "Easy BAML", MessageBoxButton.OKCancel) == MessageBoxResult.Cancel)
            {
                throw new Exception("Operation was canceled");
            }
        }

        public void SetProjectUICulture(ProjectDescription projectDescription, string uiCulture)
        {
            Project project = GetProject(projectDescription);

            var projDoc = new XmlDocument();
            projDoc.Load(project.FullName);
            bool anyChanges = false;

            SetProjectUICulture(projDoc, uiCulture, ref anyChanges);

            if (anyChanges)
            {
                EnsureFileWritable(project.FullName);
                projDoc.Save(project.FullName);
            }
        }

        public void ConfigureEasyBamlTarget(ProjectDescription projectDescription, bool importTarget)
        {
            Project project = GetProject(projectDescription);

            var projDoc = new XmlDocument();
            projDoc.Load(project.FullName);

            bool anyChanges = false;

            ConfigureEasyBamlTarget(projDoc, importTarget, ref anyChanges);

            if (anyChanges)
            {
                EnsureFileWritable(project.FullName);
                projDoc.Save(project.FullName);
            }
        }

        public void ConfigureSolution()
        {
            Project folder = GetSolutionFolder(SolutionLocalizationFolder, true);
            string solutionBamlDir = Path.Combine(GetSolutionFolder(), SolutionLocalizationFolder);

            string settingsFile = Path.Combine(solutionBamlDir, SettingsService.SettingsFile);
            AddSolutionItem(folder, settingsFile);

            string homeDir = Path.GetDirectoryName(Assembly.GetCallingAssembly().Location);
            string srcDir = Path.Combine(homeDir, "Build");

            foreach (string fileName in Directory.GetFiles(srcDir))
            {
                CopyAndAddFile(fileName, solutionBamlDir, folder);
            }
        }

        #endregion

        private void DebugProjectsInfo()
        {
            using (StreamWriter log = File.CreateText(@"C:\Temp\ProjectsInfo.log"))
            {
                foreach (ProjectDescription pd in GetProjects())
                {
                    Project project = GetProject(pd);
                    log.WriteLine("Dumping Project ======================");
                    log.WriteLine("Project.Name = {0}", project.Name);
                    log.WriteLine("Project.FullName = {0}", project.FullName);
                    log.WriteLine("Project.FileName = {0}", project.FileName);
                    log.WriteLine("Project.UniqueName = {0}", project.UniqueName);
                    log.WriteLine("Project.Kind = {0}", project.Kind);
                    log.WriteLine("Project is solution folder = {0}",
                                  (project.Kind == ProjectKinds.vsProjectKindSolutionFolder));
                    log.WriteLine("Project.Properties...");
                    DumpProperties(log, project.Properties);
                }
            }
        }

        private void DebugSolutionHierarchy()
        {
            using (StreamWriter log = File.CreateText(@"C:\Temp\SolutionHierarchy.log"))
            {
                var uihier = (UIHierarchy)applicationObject.Windows.Item(EnvDTE.Constants.vsWindowKindSolutionExplorer).Object;
                DumpHierarchyItems(uihier.UIHierarchyItems, log, 1);
                //UIHierarchyItem uihierItem = uihier.GetItem("ClassLibrary1\\ClassLibrary1");
            }
        }

        private static void DumpHierarchyItems(UIHierarchyItems items, StreamWriter log, int level)
        {
            foreach (UIHierarchyItem item in items)
            {
                log.Write(" ".PadRight(level * 4));

                bool canContainProject = false;
                string type = "unknown";
                var obj = item.Object;
                if (obj is Solution)
                {
                    type = "solution";
                    canContainProject = true;
                }
                else if (obj is Project)
                {
                    var project = (Project)obj;
                    if (project.Kind == Constants.vsProjectKindMisc)
                    {
                        type = "Misc folder";
                    }
                    else if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                    {
                        type = "Solution folder";
                        canContainProject = true;
                    }
                    else
                    {
                        type = "project " + project.UniqueName; ;
                    }
                }
                else if (obj is ProjectItem)
                {
                    type = "projectItem";
                    var projItem = (ProjectItem)obj;
                    if (projItem.SubProject != null)
                    {
                        var project = (Project)projItem.SubProject;
                        type += "project " + project.UniqueName;
                    }
                }

                log.WriteLine("Name = {0}, Level = {1}, Type= {2}", item.Name, level, type);
                //item.

                if (canContainProject)
                    DumpHierarchyItems(item.UIHierarchyItems, log, level + 1);
            }
        }

        private static void DumpProperties(StreamWriter log, Properties properties)
        {
            if (properties == null)
            {
                log.Write("       null");
                return;
            }
            foreach (Property property in properties)
            {
                log.Write("       {0} = ", property.Name);
                try
                {
                    log.WriteLine("{0}", property.Value);
                }
                catch (Exception e)
                {
                    log.WriteLine("(Error: {0})", e.Message);
                }
            }
        }

        public string GetActiveDocumentName()
        {
            string viewName = Path.GetFileNameWithoutExtension(applicationObject.ActiveDocument.Name);
            return viewName;
        }

        public string GetActiveDocumentText()
        {
            return GetDocumentText(applicationObject.ActiveDocument);
        }

        public void SetActiveDocumentText(string newText)
        {
            SetDocumentText(applicationObject.ActiveDocument, newText);
        }

        public string GetActiveAssemblyName()
        {
            Project project = applicationObject.ActiveDocument.ProjectItem.ContainingProject;
            string assemblyName = project.Properties.Item("AssemblyName").Value.ToString();
            //var outputType = project.Properties.Item("OutputType").Value.ToString();
            //var fullAssemblyName = assemblyName + outputType == "WinExe" ? ".exe" : ".dll";
            return assemblyName;
        }

        private static string GetDocumentText(Document document)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var textDoc = (TextDocument)document.Object(null);
            return textDoc.StartPoint.CreateEditPoint().GetText(textDoc.EndPoint.AbsoluteCharOffset);
        }

        private static void SetDocumentText(Document document, string newText)
        {
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            var textDoc = (TextDocument)document.Object(null);
            textDoc.StartPoint.CreateEditPoint().
                ReplaceText(textDoc.EndPoint.AbsoluteCharOffset, newText, 0);
        }

        public IEnumerable<string> GetAllXamlFiles(bool lookOnlyInCurrentProject)
        {
            var res = new List<string>();

            IEnumerable projects;
            if (!lookOnlyInCurrentProject)
            {
                projects = applicationObject.Solution.Projects;
            }
            else
            {
                Project project = applicationObject.ActiveDocument.ProjectItem.ContainingProject;
                projects = new ArrayList { project };
            }

            foreach (Project project in projects)
            {
                ProjectItems projectItems = project.ProjectItems;
                IterateProjectXamls(projectItems, res.Add);
            }

            return res;
        }

        //private void IterateProjectItems(List<string> res, ProjectItems projectItems)
        //{
        //    foreach (ProjectItem projectItem in projectItems)
        //    {
        //        string fileName = projectItem.FileNames[1];

        //        if (IsXaml(fileName))
        //        {
        //            res.Add(fileName);
        //        }
        //        else
        //        {
        //            if (Directory.Exists(fileName))
        //            {
        //                IterateProjectItems(res, projectItem.ProjectItems);
        //            }
        //        }
        //    }
        //}

        private static void IterateProjectXamls(ProjectItems projectItems, Action<string> xamlAction)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                string fileName = projectItem.FileNames[1];

                if (IsXaml(fileName))
                {
                    xamlAction(fileName);
                }
                else
                {
                    if (Directory.Exists(fileName))
                    {
                        IterateProjectXamls(projectItem.ProjectItems, xamlAction);
                    }
                }
            }
        }

        private static void IterateProjectXamls(ProjectItems projectItems, Action<string, ProjectItem> xamlAction)
        {
            foreach (ProjectItem projectItem in projectItems)
            {
                string fileName = projectItem.FileNames[1];

                if (IsXaml(fileName))
                {
                    xamlAction(fileName, projectItem);
                }
                else
                {
                    if (Directory.Exists(fileName))
                    {
                        IterateProjectXamls(projectItem.ProjectItems, xamlAction);
                    }
                }
            }
        }

        private static bool IsXaml(string fileName)
        {
            string ext = Path.GetExtension(fileName);
            if (String.Compare(ext, ".xaml", StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return false;
        }

        public bool IsActiveProjectSilverlight()
        {
            Project project = applicationObject.ActiveDocument.ProjectItem.ContainingProject;
            return ProjectHasExtender(project, SilverlightExtenderName);
        }

        public bool ProjectHasExtender(Project proj, string extenderName)
        {
            bool result = false;
            object[] extenderNames;

            try
            {
                // We could use proj.Extender(extenderName) but it causes an exception if not present and 
                // therefore it can cause performance problems if called multiple times. We use instead:

                extenderNames = (object[])proj.ExtenderNames;

                if (extenderNames.Length > 0)
                {
                    foreach (object extenderNameObject in extenderNames)
                    {
                        if (extenderNameObject.ToString() == extenderName)
                        {
                            result = true;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Ignore
            }
            return result;
        }

        private void ValidateCompatibility()
        {
            if (GetSolutionFolder() == null)
            {
                throw new NotSupportedException("Easy BAML can work only when solution is opened and saved.");
            }
            //if (!ActiveDocumentIsInProject())
            //{
            //    throw new NotSupportedException("Easy BAML can work only with files, included in current solution project.");
            //}
        }

        private bool ActiveDocumentIsInProject()
        {
            ProjectItem projectItem = applicationObject.ActiveDocument.ProjectItem;
            return (projectItem != null && projectItem.ContainingProject != null &&
                    projectItem.ContainingProject.UniqueName != Constants.vsMiscFilesProjectUniqueName);
        }

        private static void AddSolutionFolderProjects(Project solutionFolder, List<ProjectDescription> result)
        {
            foreach (ProjectItem projectItem in solutionFolder.ProjectItems)
            {
                Project subProject = projectItem.SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKinds.vsProjectKindSolutionFolder)
                {
                    AddSolutionFolderProjects(subProject, result);
                }
                else
                {
                    result.Add(CreateProjectDescription(subProject));
                }
            }
        }

        private static ProjectDescription CreateProjectDescription(Project project)
        {
            var projDesc = new ProjectDescription();
            projDesc.Tag = project;
            projDesc.UniqueName = project.UniqueName;
            projDesc.Name = project.Name;
            projDesc.FullName = project.FullName;
            projDesc.WpfProjectType = WpfProjectType.Wpf; // TODO;
            projDesc.IsLoaded = true; // TODO;
            return projDesc;
        }

        private static Project GetProject(ProjectDescription projectDescription)
        {
            return (Project)projectDescription.Tag;
        }

        private static XamlFileDescription CreateXamlFileDescription(string fileName,
                                                                     ProjectDescription projectDescription,
                                                                     ProjectItem projectItem)
        {
            return new XamlFileDescription
            {
                Name = fileName,
                ProjectDescription = projectDescription,
                Tag = projectItem
            };
        }

        private static ProjectItem GetProjectItem(XamlFileDescription xamlFile)
        {
            return (ProjectItem)xamlFile.Tag;
        }

        private static XmlNamespaceManager GetNSManager(XmlDocument projDoc)
        {
            //string xmlns = projDoc.DocumentElement.Attributes["xmlns"].Value;
            var nsmgr = new XmlNamespaceManager(projDoc.NameTable);
            nsmgr.AddNamespace("b", MSBuildNS);
            return nsmgr;
        }

        private static void SetProjectUICulture(XmlDocument projDoc, string uiCulture, ref bool anyChanges)
        {
            bool unconditionalCulterSet = false;
            var nsmgr = GetNSManager(projDoc);

            foreach (XmlElement cultNode in projDoc.SelectNodes("/b:Project/b:PropertyGroup/b:UICulture", nsmgr))
            {
                if (cultNode.InnerText != uiCulture)
                {
                    cultNode.InnerText = uiCulture; //Overwrite value of existing node
                    anyChanges = true;
                }

                var propGroupNode = (XmlElement)cultNode.ParentNode;
                string cond = propGroupNode.GetAttribute("Condition");
                if (String.IsNullOrEmpty(cond))
                {
                    unconditionalCulterSet = true;
                }
            }

            if (!unconditionalCulterSet)
            {
                bool added = false;
                foreach (XmlElement propGroupNode in projDoc.SelectNodes("/b:Project/b:PropertyGroup", nsmgr))
                {
                    string cond = propGroupNode.GetAttribute("Condition");
                    if (String.IsNullOrEmpty(cond))
                    {
                        XmlElement cultNode = projDoc.CreateElement("UICulture", MSBuildNS);
                        cultNode.InnerText = uiCulture;
                        propGroupNode.AppendChild(cultNode);
                        added = true;
                        break;
                    }
                }
                if (!added)
                {
                    throw new Exception("Unconditional PropertyGroup not found");
                }
                anyChanges = true;
            }
        }

        private static void ConfigureEasyBamlTarget(XmlDocument projDoc, bool importTarget, ref bool anyChanges)
        {
            bool importFound = false;
            XmlElement lastImportNode = null;
            var nsmgr = GetNSManager(projDoc);

            foreach (XmlElement importNode in projDoc.SelectNodes("/b:Project/b:Import", nsmgr))
            {
                string importFile = importNode.GetAttribute("Project");
                if (importFile == ImportedEasyBamlTargetFile)
                {
                    importFound = true;
                    if (!importTarget)
                    {
                        importNode.ParentNode.RemoveChild(importNode);
                        anyChanges = true;
                    }
                    break;
                }
                lastImportNode = importNode;
            }

            if (!importFound && importTarget)
            {
                if (lastImportNode == null)
                {
                    throw new Exception("No Import node were found");
                }

                XmlElement importElem = projDoc.CreateElement("Import", MSBuildNS);
                lastImportNode.ParentNode.InsertAfter(importElem, lastImportNode);
                //var projAttr = projDoc.CreateAttribute("Project", MSBuildNS);
                //projAttr.Value = ImportedEasyBamlTargetFile;
                //importElem.Attributes.Append(projAttr);
                //importElem.SetAttribute("Project", MSBuildNS, ImportedEasyBamlTargetFile);
                importElem.SetAttribute("Project", ImportedEasyBamlTargetFile);

                anyChanges = true;
            }
        }

        public void EnsureFileWritable(string fileName)
        {
            if (!CheckoutFile(fileName))
            {
                throw new Exception("Cannot checkout file " + fileName);
            }
            if ((File.GetAttributes(fileName) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                throw new Exception(String.Format("File '{0}' is readonly", fileName));
            }
        }

        private bool CheckoutFile(string fileName)
        {
            Debug.WriteLine("CheckoutFile: file = '{0}'", fileName);

            SourceControl sourceControl = applicationObject.SourceControl;
            if (sourceControl == null)
            {
                Debug.WriteLine("CheckoutFile: SourceControl is null");
                return true;
            }
            Debug.WriteLine("CheckoutFile: SourceControl is not null");

            if (!sourceControl.IsItemUnderSCC(fileName))
            {
                Debug.WriteLine("CheckoutFile: file is not under source control");
                return true;
            }

            Debug.WriteLine("CheckoutFile: file is under source control");
            if (sourceControl.IsItemCheckedOut(fileName))
            {
                Debug.WriteLine("CheckoutFile: file is already checked out");
                return true;
            }

            var result = sourceControl.CheckOutItem(fileName);
            Debug.WriteLine(result ? "CheckoutFile: file successfully checked out" : "CheckoutFile: failed check out file");
            return result;
            //return sourceControl.IsItemCheckedOut(fileName);
        }

        private void CopyAndAddFile(string fileName, string solutionBamlDir, Project folder)
        {
            string targetFileName = Path.Combine(solutionBamlDir, Path.GetFileName(fileName));
            bool fileExists = File.Exists(targetFileName);
            if (!fileExists || FilesAreDifferent(fileName, targetFileName))
            {
                if (fileExists)
                {
                    EnsureFileWritable(fileName);
                }
                File.Copy(fileName, targetFileName, true);
                File.SetAttributes(targetFileName, FileAttributes.Normal);
                File.SetCreationTime(targetFileName, File.GetCreationTime(fileName));
            }
            AddSolutionItem(folder, targetFileName);
        }

        private static bool FilesAreDifferent(string fileName, string targetFileName)
        {
            var srcInfo = new FileInfo(fileName);
            var targInfo = new FileInfo(targetFileName);

            return (srcInfo.Length != targInfo.Length ||
                    srcInfo.CreationTime > targInfo.CreationTime);
        }

        private Project GetSolutionFolder(string folderName, bool create)
        {
            foreach (Project project in applicationObject.Solution.Projects)
            {
                if (project.Kind == Constants.vsProjectKindMisc) continue;

                if (project.Kind == ProjectKinds.vsProjectKindSolutionFolder
                    && String.Compare(project.Name, folderName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return project;
                }
            }
            if (!create) return null;

            var solution = (Solution2)applicationObject.Solution;
            return solution.AddSolutionFolder(folderName);
        }

        private static void AddSolutionItem(Project solutionFolder, string fileName)
        {
            foreach (ProjectItem projectItem in solutionFolder.ProjectItems)
            {
                string itemFileName = projectItem.FileNames[1];
                if (String.Compare(itemFileName, fileName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return;
                }
            }
            solutionFolder.ProjectItems.AddFromFile(fileName);
        }

        public void ConfigureTranslationFiles(ProjectDescription projectDescription, string develepmentCulture, List<string> localizationCultures, bool localizable)
        {
            var project = GetProject(projectDescription);

            var translationFolder = GetFolder(project, ProjectTranslationFolder, localizable);
            if (translationFolder == null)
            {
                if (!localizable) return;
                throw new Exception("GetFolder didn't create folder");
            }

            //Collect existing files
            var existingFiles = new Dictionary<string, ProjectItem>();
            foreach (ProjectItem projectItem in translationFolder.ProjectItems)
            {
                string fileName = Path.GetFileName(projectItem.FileNames[1]);
                if (fileName.StartsWith(TranslationFilePrefix, StringComparison.InvariantCultureIgnoreCase)
                    && fileName.EndsWith(TranslationFileSuffix, StringComparison.InvariantCultureIgnoreCase))
                {
                    existingFiles.Add(fileName, projectItem);
                }
            }

            //Prepare list of needed files
            var neededFiles = new List<string>();
            if (localizable)
            {
                neededFiles.Add(TranslationFilePrefix + TranslationFileSuffix); //Dev culture

                foreach (var localizationCulture in localizationCultures)
                {
                    if (localizationCulture == develepmentCulture) continue;
                    neededFiles.Add(String.Format("{0}.{1}{2}", TranslationFilePrefix, localizationCulture, TranslationFileSuffix)); //Dev culture
                }
            }

            //Add missing files
            string folderPath = translationFolder.FileNames[1];
            for (int i = 0; i < neededFiles.Count; i++)
            {
                var neededFile = neededFiles[i];
                if (existingFiles.ContainsKey(neededFile))
                {
                    CheckTranslationFileProperties(existingFiles[neededFile], (i == 0));
                    existingFiles.Remove(neededFile);
                    continue;
                }

                string fileName = Path.Combine(folderPath, neededFile);
                if (!File.Exists(fileName))
                {
                    var resourceFile = new ResourceFile(fileName);
                    resourceFile.SaveFile();
                }
                var projectItem = translationFolder.ProjectItems.AddFromFile(fileName);
                CheckTranslationFileProperties(projectItem, (i == 0));
            }

            //Now existingFiles contains files to remove - Remove not needed files
            foreach (var existingFileEntry in existingFiles)
            {
                existingFileEntry.Value.Remove(); //Exclude from project, but leave the file
            }
        }

        private static void CheckTranslationFileProperties(ProjectItem projectItem, bool isDevCulture)
        {
            string itemType = isDevCulture ? "None" : "LocBamlResx";
            if (projectItem.Properties.Item("ItemType").Value.ToString() != itemType ||
                projectItem.Properties.Item("CustomTool").Value.ToString() != "")
            {
                projectItem.Properties.Item("ItemType").Value = itemType;
                projectItem.Properties.Item("CustomTool").Value = "";
            }
        }

        private static ProjectItem GetFolder(Project project, string folderName, bool create)
        {
            foreach (ProjectItem projectItem in project.ProjectItems)
            {
                if (String.Compare(projectItem.Name, folderName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    string fileName = projectItem.FileNames[1];

                    if (!Directory.Exists(fileName))
                    {
                        throw new Exception(String.Format("Project item '{0}' exists but folder does not", fileName));
                    }
                    return projectItem;
                }
            }

            if (!create) return null;

            string folderPath = Path.Combine(Path.GetDirectoryName(project.FullName), folderName);
            if (Directory.Exists(folderPath))
            {
                //This will add also all files, but no other way to add existing folder
                return project.ProjectItems.AddFromDirectory(folderPath);
            }
            return project.ProjectItems.AddFolder(folderName);
        }

        private static ProjectItem GetFile(ProjectItem folder, string fileName)
        {
            foreach (ProjectItem projectItem in folder.ProjectItems)
            {
                if (String.Compare(projectItem.Name, fileName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    return projectItem;
                }
            }

            return null;
        }

        public ResourceFile GetTranslationFile(ProjectDescription projDescr, string fileName)
        {
            var project = GetProject(projDescr);
            var translationFolder = GetFolder(project, ProjectTranslationFolder, false);
            if (translationFolder == null)
            {
                throw new Exception("Translation folder doesn't exist in project " + projDescr.Name);
            }

            foreach (ProjectItem projectItem in translationFolder.ProjectItems)
            {
                string fullPath = projectItem.FileNames[1];
                string fn = Path.GetFileName(fullPath);
                if (String.Compare(fn, fileName, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    if (!projectItem.Saved)
                    {
                        Confirm("File {0}/Translation/{1} need to be saved before continue", project.Name, fn);
                        projectItem.Save();
                    }
                    return new ResourceFile(fullPath);
                }
            }

            throw new Exception(String.Format("Translation file {0} doesn't exist in project {1}", fileName, projDescr.Name));
        }

        private static void SetFallbackCulture(Project project, string uiCulture)
        {
            if (IsCSProject(project))
            {
                ModifyAssemblyInfo(project, uiCulture, "Properties", "AssemblyInfo.cs",
                    "[assembly: NeutralResourcesLanguage(\"{0}\", UltimateResourceFallbackLocation.Satellite)]",
                    "[assembly: NeutralResourcesLanguage");
            }
            else if (IsVBProject(project))
            {
                ModifyAssemblyInfo(project, uiCulture, "My Project", "AssemblyInfo.vb",
                    "<Assembly: NeutralResourcesLanguage(\"{0}\", UltimateResourceFallbackLocation.Satellite)>",
                    "<Assembly: NeutralResourcesLanguage");
            }
            else
            {
                MessageBox.Show("Not supported project type.\nPlease configure NeutralResourcesLanguage attribute manually.", "Easy BAML");
            }
        }

        private static void ModifyAssemblyInfo(Project project, string uiCulture,
            string propsFolderName, string assemblyInfoFileName,
            string attrStringTemplate, string attrStringCheck)
        {
            var propsFolder = GetFolder(project, propsFolderName, false);
            if (propsFolder != null)
            {
                var asmInfoFile = GetFile(propsFolder, assemblyInfoFileName);
                if (asmInfoFile != null)
                {
                    var filePath = asmInfoFile.FileNames[1];
                    var attrString = String.Format(attrStringTemplate, uiCulture);

                    using (var reader = File.OpenText(filePath))
                    {
                        string s;
                        while ((s = reader.ReadLine()) != null)
                        {
                            if (s.Trim().StartsWith(attrString))
                            {
                                //Attribute found - nothing to do
                                return;
                            }
                        }
                    }

                    if (asmInfoFile.Document == null)
                    {
                        Window win = asmInfoFile.Open(Constants.vsViewKindTextView);
                        win.Visible = true;
                    }

                    using (var reader = new StringReader(GetDocumentText(asmInfoFile.Document)))
                    {
                        var writer = new StringWriter();

                        string s;
                        bool attrUpdated = false;
                        while ((s = reader.ReadLine()) != null)
                        {
                            if (s.Contains(attrStringCheck))
                            {
                                //found commented attribute or with different language
                                s = attrString;
                                attrUpdated = true;
                            }
                            writer.WriteLine(s);
                        }
                        if (!attrUpdated)
                        {
                            writer.WriteLine(attrString);
                        }

                        writer.Flush();
                        SetDocumentText(asmInfoFile.Document, writer.ToString());
                        asmInfoFile.Document.Save();
                        return;
                    }
                }
            }
            MessageBox.Show("Pluging failed to configure NeutralResourcesLanguage attribute.\nPlease configure NeutralResourcesLanguage attribute manually.", "Easy BAML");
        }

        private static bool IsVBProject(Project project)
        {
            return
                (String.Compare(Path.GetExtension(project.FullName), ".vbproj",
                                StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        private static bool IsCSProject(Project project)
        {
            return
                (String.Compare(Path.GetExtension(project.FullName), ".csproj",
                                StringComparison.InvariantCultureIgnoreCase) == 0);
        }
    }
}