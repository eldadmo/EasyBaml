using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class MsBamlResourceCsvReader : IBamlResourceReader, IDisposable
    {
        private static readonly TypeConverter BoolTypeConverter = TypeDescriptor.GetConverter(true);
        private static readonly TypeConverter StringCatConverter = TypeDescriptor.GetConverter(LocalizationCategory.Text);

        private readonly CsvTextReader reader;

        public MsBamlResourceCsvReader(char delimiter, Stream stream)
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

                // field #2: key to the localizable resource
                string key = reader.GetColumn(1);
                if (key == null)
                    throw new ApplicationException("Null Baml Key Name In Row");

                CurrentResourceKey = StringToResourceKey(key);

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
            BamlLocalizableResource resource;

            // the rest of the fields are either all null,
            // or all non-null. If all null, it means the resource entry is deleted.

            // get the string category
            string categoryString = reader.GetColumn(2);
            if (categoryString == null)
            {
                // it means all the following fields are null starting from column #3.
                resource = null;
            }
            else
            {
                // the rest must all be non-null.
                // the last cell can be null if there is no content
                for (int i = 3; i < 6; i++)
                {
                    if (reader.GetColumn(i) == null)
                        throw new Exception("Invalid Row");
                }

                // now we know all are non-null. let's try to create a resource
                resource = new BamlLocalizableResource();

                // field #3: Category
                resource.Category = (LocalizationCategory)StringCatConverter.ConvertFrom(categoryString);

                // field #4: Readable
                resource.Readable = (bool)BoolTypeConverter.ConvertFrom(reader.GetColumn(3));

                // field #5: Modifiable
                resource.Modifiable = (bool)BoolTypeConverter.ConvertFrom(reader.GetColumn(4));

                // field #6: Comments
                resource.Comments = reader.GetColumn(5);

                // field #7: Content
                resource.Content = reader.GetColumn(6);

                // in case content being the last column, consider null as empty.
                if (resource.Content == null)
                    resource.Content = string.Empty;

                // field > #7: Ignored.
            }
            CurrentResource = resource;
        }

        internal static BamlLocalizableResourceKey StringToResourceKey(string value)
        {
            int nameEnd = value.LastIndexOf(':');
            if (nameEnd < 0)
            {
                throw new ArgumentException("Resource Key Format Error");
            }

            string name = value.Substring(0, nameEnd);
            int classEnd = value.LastIndexOf('.');

            if (classEnd < 0 || classEnd < nameEnd || classEnd == value.Length)
            {
                throw new ArgumentException("Resource Key Format Error");
            }

            string className = value.Substring(nameEnd + 1, classEnd - nameEnd - 1);
            string propertyName = value.Substring(classEnd + 1);

            return new BamlLocalizableResourceKey(
                name,
                className,
                propertyName
                );
        }
    }
}