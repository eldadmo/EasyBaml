using System;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Csv
{
    public class BamlResourceCsvFinder : IBamlResourceFinder
    {
        public BamlLocalizableResourceKey FindCorrespondenceKey(BamlLocalizableResourceKey key, BamlLocalizationDictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                var entryKey = (BamlLocalizableResourceKey) entry.Key;
                if (entryKey.Uid == key.Uid && entryKey.PropertyName == key.PropertyName)
                {
                    return entryKey;
                }
            }
            return null;
        }

        public BamlLocalizableResource FindCorrespondence(BamlLocalizableResourceKey key, BamlLocalizationDictionary dictionary)
        {
            foreach (var entry in dictionary)
            {
                var entryKey = (BamlLocalizableResourceKey)entry.Key;
                if (entryKey.Uid == key.Uid && entryKey.PropertyName == key.PropertyName)
                {
                    return (BamlLocalizableResource)entry.Value;
                }
            }
            return null;
        }
    }
}