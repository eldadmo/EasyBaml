using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Resources;

namespace EasyBamlFormats.Resx
{
    public class ResourceFile
    {
        private readonly string fileName;
        private Stream stream;
        private bool writeMode;
        private SortedList<string, ResXDataNode> entries;

        public ResourceFile(string fileName)
        {
            this.fileName = fileName;
            writeMode = !File.Exists(fileName);
        }

        public ResourceFile(Stream stream, bool writeMode)
        {
            this.stream = stream;
            this.writeMode = writeMode;
        }

        public string FileName
        {
            get { return fileName; }
        }

        public SortedList<string, ResXDataNode> Resources
        {
            get
            {
                EnsureRead();
                return entries;
            }
        }

        public void SetResource(string resourceName, string resourceValue, string resourceComment)
        {
            var item = new ResXDataNode(resourceName, resourceValue);
            item.Comment = resourceComment;
            Resources[resourceName] = item;
        }

        public void RemoveResource(string resourceName)
        {
            Resources.Remove(resourceName);
        }

        public bool Contains(string resourceName)
        {
            return Resources.ContainsKey(resourceName);
        }

        public object GetValue(string resourceName)
        {
            if (!Contains(resourceName))
            {
                throw new ArgumentException("Resource could not be found", "resourceName");
            }
            return Resources[resourceName].GetValue(new AssemblyName[0]);
        }

        public string GetStringValue(string resourceName)
        {
            if (!Contains(resourceName))
            {
                throw new ArgumentException("Resource could not be found", "resourceName");
            }
            return (string) Resources[resourceName].GetValue(new AssemblyName[0]);
        }

        public string GetComment(string resourceName)
        {
            if (!Contains(resourceName))
            {
                throw new ArgumentException("Resource could not be found", "resourceName");
            }
            return Resources[resourceName].Comment;
        }

        private void EnsureRead()
        {
            if (entries != null) return;

            entries = new SortedList<string, ResXDataNode>(StringComparer.InvariantCultureIgnoreCase);

            if (writeMode) return;

            using (var reader = GetResXResourceReader())
            {
                reader.UseResXDataNodes = true;

                IDictionaryEnumerator dataEnumerator = reader.GetEnumerator();
                while (dataEnumerator.MoveNext())
                {
                    entries.Add(dataEnumerator.Key.ToString(), dataEnumerator.Value as ResXDataNode);
                }
            }
        }

        private ResXResourceReader GetResXResourceReader()
        {
            if (stream != null) return new ResXResourceReader(stream);
            return new ResXResourceReader(FileName);
        }

        public void SaveFile()
        {
            ResXResourceWriter writer = null;
            try
            {
                writer = CreateResXResourceWriter();
                EnsureRead();

                foreach (var current in entries.Values)
                {
                    writer.AddResource(current);
                }
            }
            finally
            {
                if (writer != null)
                {
                    writer.Generate();
                    writer.Close();
                }
            }
        }

        private ResXResourceWriter CreateResXResourceWriter()
        {
            if (stream != null) return new ResXResourceWriter(stream);
            return new ResXResourceWriter(FileName);
        }

        public override string ToString()
        {
            return FileName;
        }
    }
}