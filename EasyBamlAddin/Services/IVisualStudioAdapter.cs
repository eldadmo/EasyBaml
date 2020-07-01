using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EasyBamlFormats.Resx;

namespace EasyBamlAddin.Services
{
    public interface IVisualStudioAdapter
    {
        string GetSolutionFolder();

        IList<ProjectDescription> GetProjects();

        IList<XamlFileDescription> GetXamlFiles(ProjectDescription projectDescription);

        TextReader GetXamlFileContent(XamlFileDescription xamlFile);

        void SetXamlFileContent(XamlFileDescription xamlFile, string content);

        bool IsProjectSaved(ProjectDescription projectDescription);

        bool SaveProject(ProjectDescription projectDescription);

        string GetProjectUICulture(ProjectDescription projectDescription);

        void SetProjectUICulture(ProjectDescription projectDescription, string uiCulture);

        void ConfigureEasyBamlTarget(ProjectDescription projectDescription, bool importTarget);

        void ConfigureProject(ProjectDescription projectDescription, string uiCulture, bool importTarget);

        void ConfigureSolution();

        void ConfigureTranslationFiles(ProjectDescription projectDescription, string develepmentCulture, List<string> localizationCultures, bool localizable);

        ResourceFile GetTranslationFile(ProjectDescription projDescr, string fileName);

        void EnsureFileWritable(string fileName);
    }

    public class ProjectDescription
    {
        public string UniqueName { get; set; }

        public string Name { get; set; }

        public string FullName { get; set; }

        public WpfProjectType WpfProjectType { get; set; }

        public bool IsLoaded { get; set; }

        public object Tag { get; set; }
    }

    public class XamlFileDescription
    {
        public ProjectDescription ProjectDescription { get; set; }

        public string Name { get; set; }

        public object Tag { get; set; }
    }

    public enum WpfProjectType
    {
        Unknown,
        NotWpf,
        Wpf,
        Silverlight
    }
}
