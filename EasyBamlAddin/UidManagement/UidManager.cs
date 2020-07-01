using System;
using System.IO;
using System.Xml;
using EasyBamlAddin.Services;

namespace EasyBamlAddin.UidManagement
{
    public class UidManager : IDisposable
    {
        private const string XamlNamespaceX = "http://schemas.microsoft.com/winfx/2006/xaml";
        private const string XamlNamespaceDefault = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
        private const string AttributeNameUid = "Uid";
        private const string AttributeNameName = "Name";
        private const string ElementNameSetter = "Setter";
        private const string AttributeNameSetterProperty = "Property";
        private const string AttributeNameSetterValue = "Value";
        private const string ElementNameStyle = "Style";
        private const string AttributeNameStyleTargetType = "TargetType";
        private const string PerefixXmlns = "xmlns";

        private readonly ILocalizabilityChecker localizabilityChecker;
        private TextReader stream;
        private UidCollector collector;
        //private NameTable nameTable;
        //private XmlNamespaceManager nsMgr;
        private IXmlNamespaceResolver nsMgr;
        private XmlTextReader reader;
        private XamlElementsWalker elementsTree;

        public UidManager(string fileName, ILocalizabilityChecker localizabilityChecker)
            : this(fileName, File.OpenText(fileName), localizabilityChecker)
        {
        }

        public UidManager(string fileName, TextReader stream, ILocalizabilityChecker localizabilityChecker)
        {
            this.localizabilityChecker = localizabilityChecker;
            this.stream = stream;
            collector = new UidCollector(fileName);
            //nameTable = new NameTable();
            //nsMgr = new XmlNamespaceManager(nameTable);
            //reader = new XmlTextReader(stream, nameTable);
            reader = new XmlTextReader(stream);
            elementsTree = new XamlElementsWalker(reader);
            nsMgr = reader;

            InitLog(fileName);
        }

        private void InitLog(string fileName)
        {
            Log("===========================================");
            Log("Processings file {0}", fileName);
        }

        private void Log(string str, params object[] args)
        {
            //string fn = @"C:\Temp\EasyBaml.log";
            //using (var wrt = new StreamWriter(fn, true))
            //{
            //    wrt.WriteLine(str, args);
            //}            
        }


        public void Dispose()
        {
            if (stream != null)
            {
                stream.Close();
                stream = null;
            }
        }

        public UidCollector ParseFile()
        {
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (collector.RootElementLineNumber < 0)
                    {
                        collector.RootElementLineNumber = reader.LineNumber;
                        collector.RootElementLinePosition = reader.LinePosition;
                    }
                    if (reader.Name.IndexOf('.') < 0)
                    {
                        var uid = new Uid(reader.LineNumber, reader.LinePosition + reader.Name.Length, reader.Name,
                                          SpaceInsertion.BeforeUid);
                        if (reader.HasAttributes)
                        {
                            reader.MoveToNextAttribute();
                            uid.LineNumber = reader.LineNumber;
                            uid.LinePosition = reader.LinePosition;
                            uid.Space = SpaceInsertion.AfterUid;
                            do
                            {
                                string ns = nsMgr.LookupNamespace(reader.Prefix);
                                //string ns = reader.NamespaceURI;
                                if ((reader.LocalName == AttributeNameUid) && (ns == XamlNamespaceX))
                                {
                                    uid.Value = reader.Value;
                                    uid.LineNumber = reader.LineNumber;
                                    uid.LinePosition = reader.LinePosition;
                                }
                                else if ((reader.LocalName == AttributeNameName) && (ns == XamlNamespaceDefault))
                                {
                                    uid.FrameworkElementName = reader.Value;
                                }
                                else if ((reader.LocalName == AttributeNameName) && (ns == XamlNamespaceX))
                                {
                                    uid.FrameworkElementName = reader.Value;
                                }
                                else if (reader.Prefix == PerefixXmlns)
                                {
                                    collector.AddNamespacePrefix(reader.LocalName);
                                }
                            } while (reader.MoveToNextAttribute());
                        }
                        if (uid.Value == null)
                        {
                            string prefix = nsMgr.LookupPrefix(XamlNamespaceX);
                            //string prefix = reader.LookupPrefix(XamlNamespaceX);
                            if (prefix != string.Empty)
                            {
                                uid.NamespacePrefix = prefix;
                            }
                        }
                        collector.AddUid(uid, true);
                    }
                }
            }
            return collector;
        }
        
        public UidCollector ParseFileSmart()
        {
            while (reader.Read())
            {
                if (elementsTree.IsElement())
                {
                    if (collector.RootElementLineNumber < 0)
                    {
                        collector.RootElementLineNumber = reader.LineNumber;
                        collector.RootElementLinePosition = reader.LinePosition;
                    }
                    ProcessElement(null);
                }
            }
            return collector;
        }

        private bool ProcessElement(Uid parentUid)
        {
            string elementName = reader.Name;
            string elementNs = reader.NamespaceURI;
            bool isEmptyElement = reader.IsEmptyElement;
            if (elementName.IndexOf('.') < 0)
            {
                var uid = new Uid(reader.LineNumber, reader.LinePosition + reader.Name.Length, 
                                  elementName, SpaceInsertion.BeforeUid);
                collector.AddUid(uid, false);
                
                bool requiresLocalization = ProcessAttributes(elementNs, elementName, uid);

                if (!isEmptyElement)
                {
                    if (ProcessElementContents(elementNs, elementName, uid))
                    {
                        requiresLocalization = true;
                    }
                }
                else
                {
                    elementsTree.PopElement();
                }

                if (ProcessStyleSetterElement(elementNs, elementName, uid))
                {
                    requiresLocalization = true;                    
                }

                if (requiresLocalization)
                {
                    if (uid.Value == null)
                    {
                        string prefix = nsMgr.LookupPrefix(XamlNamespaceX);
                        if (prefix != string.Empty)
                        {
                            uid.NamespacePrefix = prefix;
                        }
                    }
                    collector.RegisterUid(uid);
                }
                else
                {
                    collector.RemoveUid(uid);
                }
                return false;
            }
            else
            {
                return ProcessElementContents(elementNs, elementName, parentUid);
            }
        }

        private bool ProcessStyleSetterElement(string elementNs, string elementName, Uid parentUid)
        {
            if (elementNs == XamlNamespaceDefault && GetLocalName(elementName) == ElementNameSetter)
            {
                var propertyName = elementsTree.EndedElement.GetAttribute(AttributeNameSetterProperty);
                var value = elementsTree.EndedElement.GetAttribute(AttributeNameSetterValue);

                string targetTypeNs = "*";
                string targetType = "*";
                var styleElement = elementsTree.FindParentElement(XamlNamespaceDefault, ElementNameStyle);
                if (styleElement != null)
                {
                    targetType = styleElement.GetAttribute(AttributeNameStyleTargetType);
                    if (targetType != null)
                    {
                        const string xTypeSpec = "{x:Type ";
                        if (targetType.StartsWith(xTypeSpec))
                        {
                            targetType =
                                targetType.Substring(xTypeSpec.Length, targetType.Length - xTypeSpec.Length - 1).Trim();
                        }

                        int p = targetType.IndexOf(':');
                        if (p > 0)
                        {
                            var prefix = targetType.Substring(0, p);
                            targetTypeNs = nsMgr.LookupNamespace(prefix);
                            targetType = targetType.Substring(p + 1);
                        }
                        else
                        {
                            targetTypeNs = XamlNamespaceDefault;
                        }
                    }
                }

                if (localizabilityChecker.IsAttributeLocalizable(targetTypeNs, targetType, propertyName) &&
                    !String.IsNullOrEmpty(value) && !value.StartsWith("{") &&
                    IsSignificant(value))
                {
                    parentUid.AddEntry(new LocalizableEntry(XmlNodeType.Attribute, null, AttributeNameSetterValue, value));
                    return true;
                }

            }
            return false;
        }

        private bool ProcessAttributes(string elementNs, string elementName, Uid uid)
        {
            bool requiresLocalization = false;
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                uid.LineNumber = reader.LineNumber;
                uid.LinePosition = reader.LinePosition;
                uid.Space = SpaceInsertion.AfterUid;
                do
                {
                    elementsTree.AddAttribute();
                    string ns = nsMgr.LookupNamespace(reader.Prefix);
                    if ((reader.LocalName == AttributeNameUid) && (ns == XamlNamespaceX))
                    {
                        uid.Value = reader.Value;
                        uid.LineNumber = reader.LineNumber;
                        uid.LinePosition = reader.LinePosition;
                        requiresLocalization = true;
                    }
                    else if (reader.LocalName == AttributeNameName && 
                             (ns == XamlNamespaceDefault || ns == XamlNamespaceX))
                    {
                        uid.FrameworkElementName = reader.Value;
                    }
                    else if (reader.Prefix == PerefixXmlns)
                    {
                        collector.AddNamespacePrefix(reader.LocalName);
                    }
                    else
                    {
                        if (CheckAttributeLocalizable(elementNs, elementName, reader.Name, reader.Value))
                        {
                            uid.AddEntry(new LocalizableEntry(XmlNodeType.Attribute, reader.NamespaceURI, reader.LocalName, reader.Value));
                            requiresLocalization = true;
                        }
                    }
                } while (reader.MoveToNextAttribute());
            }
            return requiresLocalization;
        }

        private bool ProcessElementContents(string elementNs, string elementName, Uid parentUid)
        {
            bool hasLocalizableContent = false;
            while (reader.Read())
            {
                if (elementsTree.IsEndElement())
                {
                    return hasLocalizableContent;
                }

                if (elementsTree.IsElement())
                {
                    if (ProcessElement(parentUid))
                    {
                        hasLocalizableContent = true;
                    }
                }
                else if (elementsTree.IsText())
                {
                    bool localizable;
                    int p = elementName.IndexOf('.');
                    if (p > 0)
                    {
                        string clsSpec = elementName.Substring(0, p);
                        string clsName = GetLocalName(clsSpec);
                        string attributeName = elementName.Substring(p + 1);

                        localizable = localizabilityChecker.IsAttributeLocalizable(elementNs, clsName, attributeName);
                    }
                    else
                    {
                        localizable = localizabilityChecker.IsContentLocalizable(elementNs, GetLocalName(elementName));
                    }

                    if (localizable)
                    {
                        string s = reader.Value.Trim();

                        if (IsSignificant(s))
                        {
                            hasLocalizableContent = true;

                            parentUid.AddEntry(new LocalizableEntry(XmlNodeType.Text, elementNs, elementName, s));

                            Log("Element Text");
                            Log("    ElementName = {0}", elementName);
                            Log("    ElementNs = {0}", elementNs);
                            Log("    TextValue = {0}", s);
                            Log("    Namespace = {0}", ResolveNamespace(elementName));
                            Log("    NamespaceURI = {0}", reader.NamespaceURI);
                        }
                    }
                }
            }
            return hasLocalizableContent;
        }

        private static string GetLocalName(string fullName)
        {
            int p = fullName.IndexOf(':');
            if (p > 0)
            {
                return fullName.Substring(p + 1);
            }
            return fullName;
        }

        private string ResolveNamespace(string fullName)
        {
            int p = fullName.IndexOf(':');
            if (p > 0)
            {
                var ns = fullName.Substring(0, p);
                return nsMgr.LookupNamespace(ns);
            }
            return reader.NamespaceURI;
            //    nsMgr.DefaultNamespace;
        }

        private bool CheckAttributeLocalizable(string elementNs, string elementName, string attributeName, string attributeValue)
        {
            bool localizable;
            int p = attributeName.IndexOf('.');
            if (p > 0)
            {
                string clsNs = reader.NamespaceURI;
                string clsSpec = attributeName.Substring(0, p);
                string clsName = GetLocalName(clsSpec);
                string propName = attributeName.Substring(p + 1);

                localizable = localizabilityChecker.IsAttributeLocalizable(clsNs, clsName, propName);
            }
            else
            {
                string ns = reader.NamespaceURI;
                if (String.IsNullOrEmpty(ns) || ns == elementNs)
                {
                    localizable = localizabilityChecker.IsAttributeLocalizable(elementNs, GetLocalName(elementName), GetLocalName(attributeName));
                }
                else
                {
                    localizable = false;
                }
            }

            if (localizable)
            {
                string s = attributeValue.Trim();

                if (s.StartsWith("{")) //Skip markup extensions
                {
                    return false;
                }

                if (IsSignificant(s))
                {
                    Log("Attribute");
                    Log("    ElementName = {0}", elementName);
                    Log("    ElementNs = {0}", elementNs);
                    Log("    AttributeName = {0}", attributeName);
                    Log("    AttributeValue = {0}", attributeValue);
                    Log("    Namespace = {0}", ResolveNamespace(attributeName));
                    Log("    NamespaceURI = {0}", reader.NamespaceURI);

                    return true;
                }
            }
            return false;
        }
         
        private static bool IsSignificant(string s)
        {
            if (String.IsNullOrEmpty(s)) return false;

            for (int i = 0; i < s.Length; i++)
            {
                if (Char.IsLetter(s, i))
                {
                    return true;
                }
            }
            return false;
        }

    }
}
