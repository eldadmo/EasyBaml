using System;
using System.IO;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class BamlResourceCsvReader : IBamlResourceReader, IDisposable
    {
        private readonly CsvTextReader reader;

        public BamlResourceCsvReader(char delimiter, Stream stream)
        {
            reader = new CsvTextReader(delimiter, stream);
        }

        public void Dispose()
        {
            reader.Close();
        }

        public bool MoveNext()
        {
            while (reader.ReadRow())
            {
                // field #1 is the baml name.
                string bamlName = reader.GetColumn(0);

                // it can't be null
                if (bamlName == null)
                    throw new ApplicationException("Empty Row Encountered");

                if (string.IsNullOrEmpty(bamlName))
                {
                    // allow for comment lines in csv file.
                    // each comment line starts with ",". It will make the first entry as String.Empty.
                    // and we will skip the whole line.
                    continue; // if the first column is empty, take it as a comment line
                }
                CurrentBamlName = bamlName;

                // field #2: Uid to the localizable resource
                string uid = reader.GetColumn(1);
                if (uid == null)
                    throw new ApplicationException("Null Baml Key Name In Row");

                // field #3: Property to the localizable resource
                string property = reader.GetColumn(2);
                if (property == null)
                    throw new ApplicationException("Null Baml Key Name In Row");

                CurrentResourceKey = new BamlLocalizableResourceKey(uid, "", property);

                ParseResource();

                return true;
            }
            return false;
        }

        public string CurrentBamlName { get; private set; }

        public BamlLocalizableResourceKey CurrentResourceKey { get; private set; }

        public BamlLocalizableResource CurrentResource { get; private set; }

        private void ParseResource()
        {
            var resource = new BamlLocalizableResource();

            // field #4: Content
            resource.Content = reader.GetColumn(3);

            // in case content being the last column, consider null as empty.
            if (resource.Content == null)
                resource.Content = string.Empty;

            CurrentResource = resource;
        }
    }
}