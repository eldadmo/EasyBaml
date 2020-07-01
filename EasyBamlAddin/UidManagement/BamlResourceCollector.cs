using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Markup.Localizer;
using System.Xml;
using EasyBamlAddin.Services;

namespace EasyBamlAddin.UidManagement
{
    public class BamlResourceCollector
    {
        private readonly IDictionary<string, List<BamlResourceEntry>> resources = new Dictionary<string, List<BamlResourceEntry>>();

        public void Add(XamlFileDescription xamlFileDescription, UidCollector uidCollector)
        {
            if (uidCollector == null) return;

            List<BamlResourceEntry> list;
            if (!resources.TryGetValue(xamlFileDescription.ProjectDescription.UniqueName, out list))
            {
                list = new List<BamlResourceEntry>();
                resources.Add(xamlFileDescription.ProjectDescription.UniqueName, list);
            }

            var bamlName = GetBamlName(xamlFileDescription);

            for (int i = 0; i < uidCollector.Count; i++)
            {
                var uid = uidCollector[i];

                if (uid.Status != UidStatus.Valid) continue;

                if (uid.Entries != null)
                {
                    foreach (var entry in uid.Entries)
                    {
                        var key = new BamlLocalizableResourceKey(uid.Value, uid.ElementName,
                                                                 GetPropertyName(entry));
                        var resource = new BamlLocalizableResource
                        {
                            Content = entry.LocalizableString,
                            Category = LocalizationCategory.Text
                        };

                        list.Add(new BamlResourceEntry(bamlName, key, resource));
                    }
                }
            }            
        }

        private static string GetPropertyName(LocalizableEntry entry)
        {
            if (entry.NodeType == XmlNodeType.Text) return "$Content";
            return entry.Name;
        }

        private static string GetBamlName(XamlFileDescription xamlFileDescription)
        {
            var projectDir = Path.GetDirectoryName(xamlFileDescription.ProjectDescription.FullName);
            var xamlFile = xamlFileDescription.Name;
            if (xamlFile.StartsWith(projectDir, StringComparison.InvariantCultureIgnoreCase))
            {
                xamlFile = xamlFile.Substring(projectDir.Length + 1).ToLowerInvariant();
                return xamlFile.Substring(0, xamlFile.Length - 5).Replace('\\', '/');
            }
            throw new NotSupportedException("Xaml file not inside project folder");
        }
         
        public List<BamlResourceEntry> GetResources(ProjectDescription projectDescription)
        {
            List<BamlResourceEntry> res;
            resources.TryGetValue(projectDescription.UniqueName, out res);
            return res;
        }
    }

    public class BamlResourceEntry
    {
        public string BamlName;
        public BamlLocalizableResourceKey Key;
        public BamlLocalizableResource Resource;

        public BamlResourceEntry(string bamlName, BamlLocalizableResourceKey key, BamlLocalizableResource resource)
        {
            BamlName = bamlName;
            Key = key;
            Resource = resource;
        }
    }
}
