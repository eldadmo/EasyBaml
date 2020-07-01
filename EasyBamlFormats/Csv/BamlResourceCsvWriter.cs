using System;
using System.IO;
using System.Windows;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class BamlResourceCsvWriter : IBamlResourceWriter, IDisposable
    {
        private readonly CsvTextWriter writer;

        public BamlResourceCsvWriter(char delimiter, Stream output)
        {
            writer = new CsvTextWriter(delimiter, output);
        }

        public void Dispose()
        {
            writer.Close();
        }

        public void Write(string bamlName, BamlLocalizableResourceKey resourceKey, BamlLocalizableResource resource)
        {
            if (!IsLocalizable(resourceKey, resource)) return;

            // column 1: baml name
            writer.WriteColumn(bamlName);

            // column 2: localizable resource key
            writer.WriteColumn(resourceKey.Uid);

            // column 3: localizable resource property
            writer.WriteColumn(resourceKey.PropertyName);

            // column 4: localizable resource's content
            writer.WriteColumn(resource.Content);

            // Done. finishing the line
            writer.EndLine();
        }

        public static bool IsLocalizable(BamlLocalizableResourceKey resourceKey, BamlLocalizableResource resource)
        {
            if (String.IsNullOrEmpty(resource.Content)) return false;

            if (resource.Category == LocalizationCategory.NeverLocalize) return false;
            if (resource.Category != LocalizationCategory.None) return true;

            if (resourceKey.ClassName == "System.Windows.Setter" && resourceKey.PropertyName == "Value") return true;

            if (resourceKey.PropertyName == "$Content")
            {
                if (resource.Content.StartsWith("#") && resource.Content.EndsWith(";")) return false;

                return true;
            }

            return false;
        }
    }
}