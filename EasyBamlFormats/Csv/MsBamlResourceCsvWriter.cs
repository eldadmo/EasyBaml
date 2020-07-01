using System;
using System.Globalization;
using System.IO;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class MsBamlResourceCsvWriter : IBamlResourceWriter, IDisposable
    {
        private readonly CsvTextWriter writer;

        public MsBamlResourceCsvWriter(char delimiter, Stream output)
        {
            writer = new CsvTextWriter(delimiter, output);
        }

        public void Dispose()
        {
            writer.Close();
        }

        public void Write(string bamlName, BamlLocalizableResourceKey resourceKey, BamlLocalizableResource resource)
        {
            writer.WriteColumn(bamlName);

            // column 2: localizable resource key
            writer.WriteColumn(ResourceKeyToString(resourceKey));

            // column 3: localizable resource's category
            writer.WriteColumn(resource.Category.ToString());

            // column 4: localizable resource's readability
            writer.WriteColumn(resource.Readable.ToString());

            // column 5: localizable resource's modifiability
            writer.WriteColumn(resource.Modifiable.ToString());

            // column 6: localizable resource's localization comments
            writer.WriteColumn(resource.Comments);

            // column 7: localizable resource's content
            writer.WriteColumn(resource.Content);

            // Done. finishing the line
            writer.EndLine();
        }

        internal static string ResourceKeyToString(BamlLocalizableResourceKey key)
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}:{1}.{2}",
                key.Uid,
                key.ClassName,
                key.PropertyName
                );
        }
    }
}