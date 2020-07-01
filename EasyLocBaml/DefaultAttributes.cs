using System;
using System.Collections.Generic;
using System.Windows;

namespace EasyLocBaml
{
    /// <summary>
    /// Defines all the static localizability attributes
    /// </summary>
    internal static class DefaultAttributes
    {
        static DefaultAttributes()
        {
            // predefined localizability attributes
            DefinedAttributes = new Dictionary<object, LocalizabilityAttribute>(32);

            // nonlocalizable attribute
            var notReadable = new LocalizabilityAttribute(LocalizationCategory.None)
                                  {Readability = Readability.Unreadable};

            var notModifiable = new LocalizabilityAttribute(LocalizationCategory.None)
                                    {Modifiability = Modifiability.Unmodifiable};

            // not localizable CLR types
            DefinedAttributes.Add(typeof(Boolean),   notReadable);
            DefinedAttributes.Add(typeof(Byte),      notReadable);
            DefinedAttributes.Add(typeof(SByte),     notReadable);
            DefinedAttributes.Add(typeof(Char),      notReadable);
            DefinedAttributes.Add(typeof(Decimal),   notReadable);
            DefinedAttributes.Add(typeof(Double),    notReadable);            
            DefinedAttributes.Add(typeof(Single),    notReadable);            
            DefinedAttributes.Add(typeof(Int32),     notReadable);            
            DefinedAttributes.Add(typeof(UInt32),    notReadable);            
            DefinedAttributes.Add(typeof(Int64),     notReadable);
            DefinedAttributes.Add(typeof(UInt64),    notReadable);            
            DefinedAttributes.Add(typeof(Int16),     notReadable);            
            DefinedAttributes.Add(typeof(UInt16),    notReadable);    
            DefinedAttributes.Add(typeof(Uri),       notModifiable);
        }   
        
        /// <summary>
        /// Get the localizability attribute for a type
        /// </summary>
        internal static LocalizabilityAttribute GetDefaultAttribute(object type)
        {
            if (DefinedAttributes.ContainsKey(type))
            {
                LocalizabilityAttribute predefinedAttribute = DefinedAttributes[type];

                // create a copy of the predefined attribute and return the copy
                var result = new LocalizabilityAttribute(predefinedAttribute.Category)
                                 {
                                     Readability = predefinedAttribute.Readability,
                                     Modifiability = predefinedAttribute.Modifiability
                                 };
                return result;
            }
            var targetType = type as Type;
            if ( targetType != null && targetType.IsValueType)
            {
                // It is looking for the default value of a value type (i.e. struct and enum)
                // we use this default.
                var attribute = new LocalizabilityAttribute(LocalizationCategory.Inherit)
                                    {Modifiability = Modifiability.Unmodifiable};
                return attribute;                    
            }
            return DefaultAttribute;
        }

        internal static LocalizabilityAttribute DefaultAttribute
        {
            get 
            {
                return new LocalizabilityAttribute(LocalizationCategory.Inherit);
            }            
        }   

        private static readonly Dictionary<object, LocalizabilityAttribute> DefinedAttributes;     // stores pre-defined attribute for types
    }
}
