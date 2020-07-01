using System;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats
{
    public interface IBamlResourceWriter : IDisposable
    {
        void Write(string bamlName, BamlLocalizableResourceKey resourceKey, BamlLocalizableResource resource);
    }
}