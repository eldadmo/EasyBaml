using System;
using System.Resources;
using System.Globalization;

namespace EasyLocBaml 
{
    internal class StringLoader
    {
         public static string Get( string id, params object[] args)
         {
             string message = ResourceManager.GetString(id);
             if (message != null)
             {
                 // Apply arguments to formatted string (if applicable)
                 if (args != null && args.Length > 0)
                 {
                     message = String.Format(CultureInfo.CurrentCulture, message, args);
                 }
             }             
             return message;
         }
         // Get exception string resources for current locale
        private static readonly ResourceManager ResourceManager = new ResourceManager("EasyLocBaml.StringTable", typeof(StringLoader).Assembly);
    }//endof class StringLoader    
}//endof namespace
