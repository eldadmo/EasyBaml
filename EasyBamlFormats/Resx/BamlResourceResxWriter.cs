using System;
using System.IO;
using System.Windows.Markup.Localizer;
using EasyBamlFormats.Csv;

namespace EasyBamlFormats.Resx
{
    public class BamlResourceResxWriter : IBamlResourceWriter
    {
        private readonly ResourceFile resourceFile;

        public BamlResourceResxWriter(Stream output)
        {
            resourceFile = new ResourceFile(output, true);
        }

        public void Dispose()
        {
            resourceFile.SaveFile();
        }

        public void Write(string bamlName, BamlLocalizableResourceKey resourceKey, BamlLocalizableResource resource)
        {
            if (!BamlResourceCsvWriter.IsLocalizable(resourceKey, resource)) return;

            resourceFile.SetResource(
                GetResourceKey(bamlName, resourceKey),
                resource.Content, 
                resource.Comments);
        }

        public static string GetResourceKey(string bamlName, BamlLocalizableResourceKey resourceKey)
        {
            return String.Format("{0}:{1}:{2}", bamlName, resourceKey.Uid, resourceKey.PropertyName);
        }
    }
}
