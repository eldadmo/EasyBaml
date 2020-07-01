using System;
using System.IO;
using System.Windows.Markup.Localizer;

namespace EasyBamlFormats.Resx
{
    public class BamlResourceResxReader : IBamlResourceReader
    {
        private readonly ResourceFile resourceFile;
        private int curIndex = -1;

        public BamlResourceResxReader(Stream output)
        {
            resourceFile = new ResourceFile(output, false);

        }

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            curIndex++;
            if (curIndex >= resourceFile.Resources.Count)
            {
                CurrentBamlName = null;
                CurrentResourceKey = null;
                CurrentResource = null;
                return false;
            }

            string key = resourceFile.Resources.Keys[curIndex];

            int fp = key.IndexOf(':');
            int lp = key.LastIndexOf(':');
            
            if (fp < 0 || lp < 0 || fp == lp)
            {
                throw new FormatException("Invalid format of BAML key " + key);
                //return MoveNext();
            }
            CurrentBamlName = key.Substring(0, fp);
            var uid = key.Substring(fp + 1, lp - fp - 1);
            var property = key.Substring(lp + 1);
            CurrentResourceKey = new BamlLocalizableResourceKey(uid, "", property);

            
            var resource = new BamlLocalizableResource();

            // field #4: Content
            resource.Content = resourceFile.GetStringValue(key);

            // in case content being the last column, consider null as empty.
            if (resource.Content == null)
                resource.Content = string.Empty;

            CurrentResource = resource;

            return true;
        }

        public string CurrentBamlName { get; private set; }

        public BamlLocalizableResourceKey CurrentResourceKey { get; private set; }

        public BamlLocalizableResource CurrentResource { get; private set; }
    }
}
