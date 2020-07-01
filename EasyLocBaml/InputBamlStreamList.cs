using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Resources;

namespace EasyLocBaml
{
    /// <summary>
    /// Class that enumerates all the baml streams in the input file
    /// </summary>
    internal class InputBamlStreamList
    {
        /// <summary>
        /// constructor
        /// </summary>        
        internal InputBamlStreamList(LocBamlOptions options)
        {
            bamlStreams  = new ArrayList();
            switch(options.InputType)
            {
                case FileType.BAML:
                {
                    bamlStreams.Add(
                        new BamlStream(
                            Path.GetFileName(options.Input),
                            File.OpenRead(options.Input)
                            )
                    );
                    break;
                }
                case FileType.RESOURCES:
                {
                    using (var resourceReader = new ResourceReader(options.Input))
                    {
                        // enumerate all bamls in a resources
                        EnumerateBamlInResources(resourceReader, options.Input, options.IsMsBamlMode);
                    }
                    break;
                }
		case FileType.EXE:
                case FileType.DLL:
                {
                    // for a dll, it is the same idea
                    Assembly assembly = Assembly.LoadFrom(options.Input);
                    foreach (string resourceName in assembly.GetManifestResourceNames())
                    {
                        ResourceLocation resourceLocation = assembly.GetManifestResourceInfo(resourceName).ResourceLocation;
                               
                        // if this resource is in another assemlby, we will skip it
                        if ((resourceLocation & ResourceLocation.ContainedInAnotherAssembly) != 0)
                        {
                            continue;   // in resource assembly, we don't have resource that is contained in another assembly
                        }
 
                        Stream resourceStream = assembly.GetManifestResourceStream(resourceName);
                        using (var reader = new ResourceReader(resourceStream))
                        {
                            EnumerateBamlInResources(reader, resourceName, options.IsMsBamlMode);                              
                        }
                    }                    
                    break;
                }
                default:
                {
                    
                    Debug.Assert(false, "Not supported type");
                    break;
                }                    
            }                  
        }

        /// <summary>
        /// return the number of baml streams found
        /// </summary>
        internal int Count
        {
            get{return bamlStreams.Count;}
        }

        /// <summary>
        /// Gets the baml stream in the input file through indexer
        /// </summary>        
        internal BamlStream this[int i]
        {
            get { return (BamlStream) bamlStreams[i];}
        }

        /// <summary>
        /// Close the baml streams enumerated
        /// </summary>
        internal void Close()
        {
            foreach (BamlStream t in bamlStreams)
            {
                t.Close();
            }
        }

        //--------------------------------
        // private function
        //--------------------------------
        /// <summary>
        /// Enumerate baml streams in a resources file
        /// </summary>        
        private void EnumerateBamlInResources(ResourceReader reader, string resourceName, bool isMsBamlMode)
        {                       
            foreach (DictionaryEntry entry in reader)
            {
                var name = entry.Key as string;
                if (BamlStream.IsResourceEntryBamlStream(name, entry.Value))
                {    
                    bamlStreams.Add( 
                        new BamlStream(
                            BamlStream.CombineBamlStreamName(resourceName, name, isMsBamlMode),
                            (Stream) entry.Value
                        )
                    );
                }    
            }
        }

        private readonly ArrayList bamlStreams;
    }

    /// <summary>
    /// BamlStream class which represents a baml stream
    /// </summary>
    internal class BamlStream
    {
        /// <summary>
        /// constructor
        /// </summary>
        internal BamlStream(string name, Stream stream)
        {
            Name = name;
            Stream = stream;
        }

        /// <summary>
        /// name of the baml 
        /// </summary>
        internal string Name { get; private set; }

        /// <summary>
        /// The actual Baml stream
        /// </summary>
        internal Stream Stream { get; private set; }

        /// <summary>
        /// close the stream
        /// </summary>
        internal void Close()
        {
            if (Stream != null)
            {
                Stream.Close();
            }           
        }

        /// <summary>
        /// Helper method which determines whether a stream name and value pair indicates a baml stream
        /// </summary>
        internal static bool IsResourceEntryBamlStream(string name, object value)
        {             
            string extension = Path.GetExtension(name);
            if (string.Compare(
                    extension, 
                    "." + FileType.BAML.ToString(), 
                    true, 
                    CultureInfo.InvariantCulture
                    ) == 0
                )                            
            {
                   //it has .Baml at the end
                Type type = value.GetType();

                if (typeof(Stream).IsAssignableFrom(type))
                return true;
            }            
            return false;                
        }

        /// <summary>
        /// Combine baml stream name and resource name to uniquely identify a baml within a 
        /// localization project
        /// </summary>
        internal static string CombineBamlStreamName(string resource, string bamlName, bool isMsBamlMode)
        {
            Debug.Assert(resource != null && bamlName != null, "Resource name and baml name can't be null");

            if (isMsBamlMode)
            {
                string suffix = Path.GetFileName(bamlName);
                string prefix = Path.GetFileName(resource);

                return prefix + LocBamlConst.BamlAndResourceSeperator + suffix;
            }
            else
            {
                return bamlName.Substring(0, bamlName.Length - 5); //Remove .baml extension
            }
        }
    }
}
