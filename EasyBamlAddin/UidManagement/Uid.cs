using System.Collections.Generic;
using System.Xml;

namespace EasyBamlAddin.UidManagement
{
    public sealed class Uid
    {
        // Fields
        internal string ElementName;
        internal string FrameworkElementName;
        internal int LineNumber;
        internal int LinePosition;
        internal string NamespacePrefix;
        internal SpaceInsertion Space;
        internal UidStatus Status;
        internal string Value;
        public IList<LocalizableEntry> Entries;
        
        internal string LocalizableString
        {
            get
            {
                if (Entries == null || Entries.Count == 0) return null;
                return Entries[0].LocalizableString;
            }
        }

        // Methods
        internal Uid(int lineNumber, int linePosition, string elementName, SpaceInsertion spaceInsertion)
        {
            LineNumber = lineNumber;
            LinePosition = linePosition;
            ElementName = elementName;
            Value = null;
            NamespacePrefix = null;
            FrameworkElementName = null;
            Status = UidStatus.Valid;
            Space = spaceInsertion;
        }

        public void AddEntry(LocalizableEntry entry)
        {
            if (Entries == null)
            {
                Entries = new List<LocalizableEntry>();
            }
            Entries.Add(entry);
        }
    }

    internal enum SpaceInsertion : byte
    {
        BeforeUid,
        AfterUid
    }

    public enum UidStatus : byte
    {
        Valid,
        Absent,
        Duplicate,
        Unknown
    }

    public class LocalizableEntry
    {
        public XmlNodeType NodeType;
        public string Namespace;
        public string Name;
        public string LocalizableString;

        public LocalizableEntry(XmlNodeType nodeType, string ns, string name, string localizableString)
        {
            NodeType = nodeType;
            Namespace = ns;
            Name = name;
            LocalizableString = localizableString != null ? localizableString.Trim() : null;
        }
    }
}
