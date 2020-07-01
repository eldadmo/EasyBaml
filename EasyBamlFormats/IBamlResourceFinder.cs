using System.Windows.Markup.Localizer;

namespace EasyBamlFormats
{
    public interface IBamlResourceFinder
    {
        BamlLocalizableResourceKey FindCorrespondenceKey(BamlLocalizableResourceKey key,
                                                      BamlLocalizationDictionary dictionary);

        BamlLocalizableResource FindCorrespondence(BamlLocalizableResourceKey key,
                                              BamlLocalizationDictionary dictionary);        
    }
}