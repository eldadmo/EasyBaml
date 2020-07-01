using System;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class MsBamlResourceCsvFinder : IBamlResourceFinder
    {
        public BamlLocalizableResourceKey FindCorrespondenceKey(BamlLocalizableResourceKey key, BamlLocalizationDictionary dictionary)
        {
            return key;
        }

        public BamlLocalizableResource FindCorrespondence(BamlLocalizableResourceKey key, BamlLocalizationDictionary dictionary)
        {
            if (dictionary.Contains(key))
            {
                return dictionary[key];
            }
            return null;
        }
    }
}