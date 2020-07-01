using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics;
using System.Resources;
using System.Threading;
using System.Windows.Markup.Localizer;
using EasyBamlFormats;

namespace EasyLocBaml
{
    /// <summary>
    /// ResourceGenerator class
    /// </summary>
    internal class ResourceGenerator
    {
        private readonly LocBamlOptions options;
        private readonly TranslationDictionariesReader dictionaries;
        private readonly TranslationDictionariesReader fallbackDictionaries;
        private readonly IBamlResourceFinder bamlResourceFinder;

        /// <param name="options">EasyLocBaml options</param>
        /// <param name="dictionaries">the translation dictionaries</param>
        /// <param name="fallbackDictionaries"></param>
        /// <param name="bamlResourceFinder"></param>
        public ResourceGenerator(LocBamlOptions options, TranslationDictionariesReader dictionaries, 
            TranslationDictionariesReader fallbackDictionaries, IBamlResourceFinder bamlResourceFinder)
        {
            this.options = options;
            this.dictionaries = dictionaries;
            this.fallbackDictionaries = fallbackDictionaries;
            this.bamlResourceFinder = bamlResourceFinder;
        }

        /// <summary>
        /// Generates localized Baml from translations
        /// </summary>
        internal void Generate()
        {   
            // base on the input, we generate differently            
            switch(options.InputType)
            {
                case FileType.BAML :                    
                {                    
                    // input file name
                    string bamlName = Path.GetFileName(options.Input);

                    // outpuf file name is Output dir + input file name
                    string outputFileName = GetOutputFileName();

                    // construct the full path
                    string fullPathOutput = Path.Combine(options.Output, outputFileName);

                    options.Write(StringLoader.Get("GenerateBaml", fullPathOutput));
                                                                                
                    using (Stream input = File.OpenRead(options.Input))
                    {
                        using (Stream output = new FileStream(fullPathOutput, FileMode.Create))
                        {
                            GenerateBamlStream(input, output, GetDictionaries(bamlName));
                        }                               
                    }

                    options.WriteLine(StringLoader.Get("Done"));
                    break;
                }
                case FileType.RESOURCES :
                {
                    string outputFileName = GetOutputFileName();
                    string fullPathOutput = Path.Combine(options.Output, outputFileName);
                                       
                    using (Stream input = File.OpenRead(options.Input))
                    {
                        using (Stream output = File.OpenWrite(fullPathOutput))
                        {
                            // create a Resource reader on the input;
                            IResourceReader reader = new ResourceReader(input);
    
                            // create a writer on the output;
                            IResourceWriter writer = new ResourceWriter(output);

                            GenerateResourceStream(
                                options.Input,   // resources name
                                reader,          // resource reader
                                writer);         // resource writer

                            reader.Close();
                            
                            // now generate and close
                            writer.Generate();
                            writer.Close();
                        }
                    }

                    options.WriteLine(StringLoader.Get("DoneGeneratingResource", outputFileName));
                    break;
                }
		case FileType.EXE:
                case FileType.DLL:
                {   
                    GenerateAssembly();
                    break;
                }
                default:
                {
                    Debug.Assert(false, "Can't generate to this type");       
                    break;
                }
            }            
        }


        private void GenerateBamlStream(Stream input, Stream output, Dictionaries curDictionaries)
        {
            string commentFile = Path.ChangeExtension(options.Input, "loc");
            TextReader commentStream = null;           

            try
            {
                if (File.Exists(commentFile))
                {
                    commentStream = new StreamReader(commentFile);
                }

                // create a localizabilty resolver based on reflection
                var localizabilityReflector =
                    new BamlLocalizabilityByReflection(options.Assemblies); 

                // create baml localizer
                var mgr = new BamlLocalizer(
                    input,
                    localizabilityReflector,
                    commentStream
                    );

                // get the resources
                var source = mgr.ExtractResources();
                var translations = new BamlLocalizationDictionary();

                foreach (DictionaryEntry entry in source)
                {
                    var key = (BamlLocalizableResourceKey) entry.Key;
                    var translatedResource = curDictionaries.FindCorrespondence(key);
                    if (translatedResource != null)
                    {
                        string translatedContent = translatedResource.Content;

                        if (!String.IsNullOrEmpty(translatedContent))
                        {
                            var curResource = (BamlLocalizableResource) entry.Value;
                            if (curResource.Content != translatedContent)
                            {
                                translations.Add(key, translatedResource);
                            }
                        }
                    }
                }
                
                // update baml
                mgr.UpdateBaml(output, translations);
            }
            finally
            {
                if (commentStream != null)
                {
                    commentStream.Close();
                }
            }
        }

        private void GenerateResourceStream(
                string resourceName,                        // the name of the .resources file
                IResourceReader reader,                     // the reader for the .resources
                IResourceWriter writer                     // the writer for the output .resources
            )
        {

            options.WriteLine(StringLoader.Get("GenerateResource", resourceName));
            // enumerate through each resource and generate it
            foreach (DictionaryEntry entry in reader)
            {
                var name = entry.Key as string;
                object resourceValue = null;

                // See if it looks like a Baml resource
                if (BamlStream.IsResourceEntryBamlStream(name, entry.Value))
                {
                    Stream targetStream = null;
                    options.Write("    ");
                    options.Write(StringLoader.Get("GenerateBaml", name));

                    // grab the localizations available for this Baml
                    string bamlName = BamlStream.CombineBamlStreamName(resourceName, name, options.IsMsBamlMode);
                    Dictionaries localizations = GetDictionaries(bamlName);
                    if (!localizations.IsEmpty)
                    {
                        targetStream = new MemoryStream();

                        // generate into a new Baml stream
                        GenerateBamlStream((Stream) entry.Value, targetStream, localizations);
                    }
                    options.WriteLine(StringLoader.Get("Done"));

                    // sets the generated object to be the generated baml stream
                    resourceValue = targetStream;
                }

                if (resourceValue == null)
                {
                    //
                    // The stream is not localized as Baml yet, so we will make a copy of this item into 
                    // the localized resources
                    //

                    // We will add the value as is if it is serializable. Otherwise, make a copy
                    resourceValue = entry.Value;

                    object[] serializableAttributes =
                        resourceValue.GetType().GetCustomAttributes(typeof (SerializableAttribute), true);
                    if (serializableAttributes.Length == 0)
                    {
                        // The item returned from resource reader is not serializable
                        // If it is Stream, we can wrap all the values in a MemoryStream and 
                        // add to the resource. Otherwise, we had to skip this resource.
                        var resourceStream = resourceValue as Stream;
                        if (resourceStream != null)
                        {
                            var buffer = new byte[resourceStream.Length];
                            resourceStream.Read(buffer, 0, buffer.Length);
                            Stream targetStream = new MemoryStream(buffer);
                            resourceValue = targetStream;
                        }
                    }
                }

                if (resourceValue != null)
                {
                    writer.AddResource(name, resourceValue);
                }
            }
        }

        private static void GenerateStandaloneResource(string fullPathName, Stream resourceStream)
        {
            // simply do a copy for the stream
            using (var file = new FileStream(fullPathName, FileMode.Create, FileAccess.Write))
            {
                const int BUFFER_SIZE = 4096;
                var buffer = new byte[BUFFER_SIZE];
                int bytesRead = 1;
                while (bytesRead > 0)
                {
                     bytesRead = resourceStream.Read(buffer, 0, BUFFER_SIZE);
                     file.Write(buffer, 0, bytesRead);
                }                                                           
            }            
        }

        //--------------------------------------------------
        // The function follows Managed code parser
        // implementation. in the future, maybe they should 
        // share the same code
        //--------------------------------------------------
        private void GenerateAssembly()
        {
            // there are many names to be used when generating an assembly
            string sourceAssemblyFullName   = options.Input;                // source assembly full path 
            string outputAssemblyDir        = options.Output;               // output assembly directory
            string outputAssemblyLocalName  = GetOutputFileName();   // output assembly name
            string moduleLocalName          = GetAssemblyModuleLocalName(outputAssemblyLocalName); // the module name within the assmbly
                
            // get the source assembly
            Assembly srcAsm = Assembly.LoadFrom(sourceAssemblyFullName);

            // obtain the assembly name
            AssemblyName targetAssemblyNameObj = srcAsm.GetName();

            // store the culture info of the source assembly
            CultureInfo srcCultureInfo  = targetAssemblyNameObj.CultureInfo;
            
            // update it to use it for target assembly
            targetAssemblyNameObj.Name        = Path.GetFileNameWithoutExtension(outputAssemblyLocalName);
            targetAssemblyNameObj.CultureInfo = options.CultureInfo;

            // we get a assembly builder
            AssemblyBuilder targetAssemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(
                targetAssemblyNameObj,                  // name of the assembly
                AssemblyBuilderAccess.RunAndSave,       // access rights
                outputAssemblyDir                       // storage dir
                );            

            // we create a module builder for embeded resource modules
            ModuleBuilder moduleBuilder = targetAssemblyBuilder.DefineDynamicModule(
                moduleLocalName,
                outputAssemblyLocalName
                );

            options.WriteLine(StringLoader.Get("GenerateAssembly"));
                      
            // now for each resource in the assembly
            foreach (string resourceName in srcAsm.GetManifestResourceNames())
            {                
                // get the resource location for the resource
                ResourceLocation resourceLocation = srcAsm.GetManifestResourceInfo(resourceName).ResourceLocation;
                               
                // if this resource is in another assemlby, we will skip it
                if ((resourceLocation & ResourceLocation.ContainedInAnotherAssembly) != 0)
                {
                    continue;   // in resource assembly, we don't have resource that is contained in another assembly
                }

                // gets the neutral resource name, giving it the source culture info
                string neutralResourceName  = GetNeutralResModuleName(resourceName, srcCultureInfo);

                // gets the target resource name, by giving it the target culture info
                string targetResourceName   = GetCultureSpecificResourceName(neutralResourceName, options.CultureInfo);

                // resource stream              
                Stream resourceStream       = srcAsm.GetManifestResourceStream(resourceName);
                
                // see if it is a .resources
                if (neutralResourceName.ToLower(CultureInfo.InvariantCulture).EndsWith(".resources"))
                {                                   
                    // now we think we have resource stream 
                    // get the resource writer
                    IResourceWriter writer;
                    // check if it is a embeded assembly
                    if ((resourceLocation & ResourceLocation.Embedded) != 0)
                    {
                        // gets the resource writer from the module builder
                        writer = moduleBuilder.DefineResource(
                            targetResourceName,         // resource name
                            targetResourceName,         // resource description
                            ResourceAttributes.Public   // visibilty of this resource to other assembly
                            );
                    }                                
                    else
                    {
                        // it is a standalone resource, we get the resource writer from the assembly builder
                        writer =  targetAssemblyBuilder.DefineResource(
                            targetResourceName,         // resource name 
                            targetResourceName,         // description
                            targetResourceName,         // file name to save to   
                            ResourceAttributes.Public   // visibility of this resource to other assembly
                        );
                    }

                    // get the resource reader
                    IResourceReader reader = new ResourceReader(resourceStream);

                    // generate the resources
                    GenerateResourceStream(resourceName, reader, writer);

                    // we don't call writer.Generate() or writer.Close() here 
                    // because the AssemblyBuilder will call them when we call Save() on it.
                }
                else
                {
                    // else it is a stand alone untyped manifest resources.
                    string extension = Path.GetExtension(targetResourceName);                    

                    string fullFileName = Path.Combine(outputAssemblyDir, targetResourceName);
                    
                    // check if it is a .baml, case-insensitive
                    if (string.Compare(extension, ".baml", true, CultureInfo.InvariantCulture) == 0)
                    {
                        // try to localized the the baml
                        // find the resource dictionary
                        var curDictionaries = GetDictionaries(resourceName);

                        // if it is null, just create an empty dictionary.
                        if (!curDictionaries.IsEmpty)
                        {
                            // it is a baml stream
                            using (Stream output = File.OpenWrite(fullFileName))
                            {
                                options.Write("    ");
                                options.WriteLine(StringLoader.Get("GenerateStandaloneBaml", fullFileName));
                                GenerateBamlStream(resourceStream, output, curDictionaries);
                                options.WriteLine(StringLoader.Get("Done"));
                            }
                        }
                        else
                        {
                            // can't find localization of it, just copy it
                            GenerateStandaloneResource( fullFileName, resourceStream);
                        }
                    }
                    else
                    {
                        // it is an untyped resource stream, just copy it
                        GenerateStandaloneResource( fullFileName, resourceStream);
                    }
    
                    // now add this resource file into the assembly
                    targetAssemblyBuilder.AddResourceFile(
                        targetResourceName,           // resource name
                        targetResourceName,           // file name
                        ResourceAttributes.Public     // visibility of the resource to other assembly
                    );
                    
                }  
            }

            // at the end, generate the assembly
            targetAssemblyBuilder.Save(outputAssemblyLocalName);
            options.WriteLine(StringLoader.Get("DoneGeneratingAssembly"));
        }


        //-----------------------------------------
        // private function dealing with naming 
        //-----------------------------------------

        // return the local output file name, i.e. without directory
        private string GetOutputFileName()
        {
            string outputFileName;
            string inputFileName = Path.GetFileName(options.Input);

            switch (options.InputType)
            {
                case FileType.BAML:
                    {
                        return inputFileName;
                    }
                case FileType.EXE:
                    {
                        inputFileName = inputFileName.Remove(inputFileName.LastIndexOf('.')) + ".resources.dll";
                        return inputFileName;
                    }
                case FileType.DLL:
                    {
                        return inputFileName;
                    }
                case FileType.RESOURCES:
                    {
                        // get the output file name
                        outputFileName = inputFileName;

                        // get to the last dot seperating filename and extension
                        int lastDot = outputFileName.LastIndexOf('.');
                        int secondLastDot = outputFileName.LastIndexOf('.', lastDot - 1);
                        if (secondLastDot > 0)
                        {
                            string cultureName = outputFileName.Substring(secondLastDot + 1, lastDot - secondLastDot - 1);
                            if (LocBamlConst.IsValidCultureName(cultureName))
                            {
                                string extension = outputFileName.Substring(lastDot);
                                string frontPart = outputFileName.Substring(0, secondLastDot + 1);
                                outputFileName = frontPart + options.CultureInfo.Name + extension;
                            }
                            else
                            {
                                //Input is in invariant culture
                                string extension = outputFileName.Substring(lastDot);
                                string frontPart = outputFileName.Substring(0, lastDot + 1);
                                outputFileName = frontPart + options.CultureInfo.Name + extension;
                            }
                        }
                        return outputFileName;
                    }
                default:
                    {
                        throw new NotSupportedException();
                    }
            }
        }

        private string GetAssemblyModuleLocalName(string targetAssemblyName)
        {
            string moduleName;
            if (targetAssemblyName.ToLower(CultureInfo.InvariantCulture).EndsWith(".resources.dll"))                
            {
                // we create the satellite assembly name
                moduleName = string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}.{1}.{2}", 
                    targetAssemblyName.Substring(0, targetAssemblyName.Length - ".resources.dll".Length),
                    options.CultureInfo.Name,
                    "resources.dll"
                    );               
            }
            else
            {
                moduleName = targetAssemblyName;
            }
            return moduleName;
        }



        // return the neutral resource name
        private static string GetNeutralResModuleName(string resourceName, CultureInfo cultureInfo)
        {
            if (cultureInfo.Equals(CultureInfo.InvariantCulture))
            {
                return resourceName;
            }
            // if it is an satellite assembly, we need to strip out the culture name
            string normalizedName = resourceName.ToLower(CultureInfo.InvariantCulture);
            int end = normalizedName.LastIndexOf(".resources");

            if (end < 0)
            {
                return resourceName;
            }

            int start = normalizedName.LastIndexOf('.', end - 1);

            if (start > 0 && end - start > 0)
            {
                string cultureStr = resourceName.Substring( start + 1, end - start - 1);

                if (string.Compare(cultureStr, cultureInfo.Name, true) == 0)
                {
                    // it has the correct culture name, so we can take it out
                    return resourceName.Remove(start, end - start);
                }
            }        
            return resourceName;
        }

        private static string GetCultureSpecificResourceName(string neutralResourceName, CultureInfo culture)
        {
            // gets the extension
            string extension    = Path.GetExtension(neutralResourceName);

            // swap in culture name
            string cultureName  = Path.ChangeExtension(neutralResourceName, culture.Name);

            // return the new name with the same extension
            return cultureName + extension;
        }

        private Dictionaries GetDictionaries(string bamlName)
        {
            var res = new Dictionaries(bamlResourceFinder);
            res.AddDictionary(dictionaries[bamlName]);
            if (fallbackDictionaries != null)
            {
                res.AddDictionary(fallbackDictionaries[bamlName]);
            }
            return res;
        }

        private class Dictionaries
        {
            private readonly IBamlResourceFinder bamlResourceFinder;
            private readonly List<BamlLocalizationDictionary> dictionariesList = new List<BamlLocalizationDictionary>();

            public Dictionaries(IBamlResourceFinder bamlResourceFinder)
            {
                this.bamlResourceFinder = bamlResourceFinder;
            }

            public void AddDictionary(BamlLocalizationDictionary dictionary)
            {
                if (dictionary != null)
                {
                    dictionariesList.Add(dictionary);
                }
            }

            public bool IsEmpty
            {
                get { return dictionariesList.Count == 0; }
            }
            
            public BamlLocalizableResource FindCorrespondence(BamlLocalizableResourceKey key)
            {
                foreach (var dictionary in dictionariesList)
                {
                    var resource = bamlResourceFinder.FindCorrespondence(key, dictionary);
                    if (resource != null && !String.IsNullOrEmpty(resource.Content))
                    {
                        return resource;
                    }
                }
                return null;
            }
        }
    }   
}
