using System;
using System.IO;
using System.Collections;
using System.Windows.Markup.Localizer;
using EasyBamlFormats;

namespace EasyLocBaml
{
    /// <summary>
    /// Writer to write out localizable values into CSV or tab-separated txt files.     
    /// </summary>
    internal static class TranslationDictionariesWriter
    {
        /// <summary>
        /// Write the localizable key-value pairs
        /// </summary>
        /// <param name="options"></param>
        internal static void Write(LocBamlOptions options)            
        {   
            Stream output = new FileStream(options.Output, FileMode.Create);
            var bamlStreamList = new InputBamlStreamList(options);

            using (var writer = options.CreateBamlResourceWriter(output))
            {
                options.WriteLine(StringLoader.Get("WriteBamlValues"));
                for (int i = 0; i < bamlStreamList.Count; i++)
                {
                    options.Write("    ");
                    options.Write(StringLoader.Get("ProcessingBaml", bamlStreamList[i].Name));

                    // Search for comment file in the same directory. The comment file has the extension to be 
                    // "loc".
                    string commentFile = Path.ChangeExtension(bamlStreamList[i].Name, "loc");
                    TextReader commentStream = null;

                    try
                    {
                        if (File.Exists(commentFile))
                        {
                            commentStream = new StreamReader(commentFile);
                        }

                        // create the baml localizer
                        var mgr = new BamlLocalizer(
                            bamlStreamList[i].Stream,
                            new BamlLocalizabilityByReflection(options.Assemblies),
                            commentStream
                            );

                        // extract localizable resource from the baml stream
                        BamlLocalizationDictionary dict = mgr.ExtractResources();

                        // write out each resource
                        foreach (DictionaryEntry entry in dict)
                        {
                            writer.Write(bamlStreamList[i].Name, (BamlLocalizableResourceKey) entry.Key, (BamlLocalizableResource)entry.Value);
                        }

                        options.WriteLine(StringLoader.Get("Done"));
                    }
                    finally
                    {
                        if (commentStream != null)
                            commentStream.Close();
                    }
                }
                
                // close all the baml input streams, output stream is closed by writer.
                bamlStreamList.Close();            
            }   
        }
    }


    /// <summary>
    /// Reader to read the translations from CSV or tab-separated txt file    
    /// </summary> 
    internal class TranslationDictionariesReader
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="reader">resoure text reader that reads CSV or a tab-separated txt file</param>
        internal TranslationDictionariesReader(IBamlResourceReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            // hash key is case insensitive strings
            table = new Hashtable();

            // we read each Row
            while (reader.MoveNext())
            {                
                // get the dictionary 
                BamlLocalizationDictionary dictionary = this[reader.CurrentBamlName];
                if (dictionary == null)
                {   
                    // we create one if it is not there yet.
                    dictionary = new BamlLocalizationDictionary();
                    this[reader.CurrentBamlName] = dictionary;
                }
                                
                // at this point, we are good.
                // add to the dictionary.
                dictionary.Add(reader.CurrentResourceKey, reader.CurrentResource);
            }        
        }

        internal BamlLocalizationDictionary this[string key]
        {
            get{
                return (BamlLocalizationDictionary) table[key.ToLowerInvariant()];
            }
            set
            {
                table[key.ToLowerInvariant()] = value;
            }
        }

        // hashtable that maps from baml name to its ResourceDictionary
        private readonly Hashtable table;                
    }
}
