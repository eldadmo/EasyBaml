using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EasyBamlAddin.Services
{
    public interface ILocalizabilityChecker
    {
        bool IsContentLocalizable(string elementNs, string elementName);

        bool IsAttributeLocalizable(string elementNs, string elementName, string attributeName);
    }
}
