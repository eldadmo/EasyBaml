using System;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats
{
    public interface IBamlResourceReader : IDisposable
    {
        bool MoveNext();

        string CurrentBamlName { get; }

        BamlLocalizableResourceKey CurrentResourceKey { get; }

        BamlLocalizableResource CurrentResource { get; }
    }
}