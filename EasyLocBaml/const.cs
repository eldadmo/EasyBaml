using System;
using System.Globalization;
using System.Diagnostics;
using System.Windows.Markup.Localizer;

namespace EasyLocBaml
{
    internal static class LocBamlConst
    {
        internal const string BamlAndResourceSeperator = ":";
        internal const string ResourceExtension        = ".resources";

        internal static char GetDelimiter(FileType fileType)
        {
            char delimiter;
            switch (fileType)
            {
                case FileType.CSV:
                {
                        delimiter = ',';
                        break;
                }

                case FileType.TXT:
                {
                        delimiter = '\t';
                        break;
                }
                default:
                {
                    Debug.Assert(false, "Un supported FileType");
                    delimiter = ','; 
                    break;
                }
            }
            
            return delimiter;
        }

        internal static bool IsValidCultureName(string name)
        {
            try 
            {
                // try create a culture to see if the name is a valid culture name
                var culture = new CultureInfo(name);
                return (culture != null);
            }
            catch (Exception )
            {
                return false;
            }
        }
    }    
}
