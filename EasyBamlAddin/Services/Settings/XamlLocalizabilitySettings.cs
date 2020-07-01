using System.Collections.Generic;
using System.Xml.Serialization;

namespace EasyBamlAddin.Services.Settings
{
    public class XamlLocalizabilitySettings
    {
        [XmlArrayItem("Element")]
        public List<XamlElementLocalizabilitySettings> Elements { get; set; }
    }

    public class XamlElementLocalizabilitySettings
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string Namespace { get; set; }

        [XmlAttribute]
        public bool IsContentLocalizable { get; set; }

        [XmlArrayItem("Attribute")]
        public List<XamlAttributeLocalizabilitySetting> Attributes { get; set; }        
    }

    public class XamlAttributeLocalizabilitySetting
    {
        [XmlAttribute]
        public string Name { get; set; }
    }
}
