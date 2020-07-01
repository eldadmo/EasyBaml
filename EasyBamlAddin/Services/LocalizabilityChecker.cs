using System;
using System.Linq;
using EasyBamlAddin.Services.Settings;

namespace EasyBamlAddin.Services
{
    public class LocalizabilityChecker : ILocalizabilityChecker
    {
        private readonly ISettingsService settingsService;

        public LocalizabilityChecker(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
        }

        public bool IsContentLocalizable(string elementNs, string elementName)
        {
            var settings = settingsService.GetGlobalLocalizabilitySettings();
            if (settings != null && settings.Elements != null)
            {
                //First check exact match - it will have priority
                var settingsElememt = settings.Elements.FirstOrDefault(elm => IsExactElementMatch(elm, elementNs, elementName));
                if (settingsElememt != null)
                {
                    return settingsElememt.IsContentLocalizable;
                }

                return settings.Elements.Where(elm => IsElementMatch(elm, elementNs, elementName)).Any(elm => elm.IsContentLocalizable);
            }
            return false;
        }

        public bool IsAttributeLocalizable(string elementNs, string elementName, string attributeName)
        {
            var settings = settingsService.GetGlobalLocalizabilitySettings();
            if (settings != null && settings.Elements != null)
            {
                return settings.Elements.Where(elm => IsElementMatch(elm, elementNs, elementName)).
                    Where(elm => elm.Attributes != null).
                    Any(elm => elm.Attributes.Any(att => IsMatchTemplate(attributeName, att.Name)));
            }
            return false;
        }

        private static bool IsExactElementMatch(XamlElementLocalizabilitySettings elm, string elementNs, string elementName)
        {
            return (elementNs == elm.Namespace && elementName == elm.Name);
        }

        private static bool IsElementMatch(XamlElementLocalizabilitySettings elm, string elementNs, string elementName)
        {
            return (IsMatchTemplate(elementNs, elm.Namespace) &&
                    IsMatchTemplate(elementName, elm.Name));
        }

        private static bool IsMatchTemplate(string name, string template)
        {
            return (String.IsNullOrEmpty(template) || template == "*" || template == name);
        }
    }
}
