using System;
using System.Collections.Generic;
using System.Xml;

namespace EasyBamlAddin.UidManagement
{
    public class XamlElementsWalker
    {
        private XmlTextReader reader;
        private Stack<XmlElementDescr> elementsTree = new Stack<XmlElementDescr>();

        public XmlElementDescr EndedElement;

        public XamlElementsWalker(XmlTextReader reader)
        {
            this.reader = reader;
        }

        public bool IsElement()
        {
            if (reader.NodeType == XmlNodeType.Element)
            {
                var elementDescr = new XmlElementDescr(reader);
                elementsTree.Push(elementDescr);
                return true;
            }
            return false;
        }

        public void PopElement()
        {
            if (elementsTree.Count > 0)
            {
                EndedElement = elementsTree.Pop();
            }
        }

        public bool IsEndElement()
        {
            if (reader.NodeType == XmlNodeType.EndElement)
            {
                PopElement();
                return true;
            }
            return false;
        }

        public bool IsText()
        {
            if (reader.NodeType == XmlNodeType.Text)
            {
                if (elementsTree.Count > 1)
                {
                    var topElement = elementsTree.Peek();
                    int p = topElement.LocalName.IndexOf('.');
                    if (p > 0)
                    {
                        var topClassName = topElement.LocalName.Substring(0, p);
                        var propName = topElement.LocalName.Substring(p + 1);

                        var elements = elementsTree.ToArray();
                        var clsElement = elements[1];
                        if (clsElement.LocalName.IndexOf('.') < 0)
                        {
                            if (clsElement.LocalName != topClassName ||
                                clsElement.NamespaceURI != topElement.NamespaceURI)
                            {
                                propName = topElement.LocalName;
                            }
                            clsElement.SetAttribute(
                                new XmlAttributeDescr(topElement.NamespaceURI, propName, reader.Value));
                        }
                    }
                }
                return true;
            }
            return false;
        }

        public void AddAttribute()
        {
            if (elementsTree.Count > 0)
            {
                var elementDescr = elementsTree.Peek();
                elementDescr.SetAttribute(reader);
            }
        }

        public string GetAttribute(string attributeName)
        {
            if (elementsTree.Count == 0) return null;

            var element = elementsTree.Peek();

            return element.GetAttribute(attributeName);
        }

        public XmlElementDescr FindParentElement(string elementNs, string elementLocalName)
        {
            const int maxLevels = 3;
            var elements = elementsTree.ToArray();
            for (int i = 0; i < maxLevels && i < elements.Length; i++)
            {
                var element = elements[i];
                if (element.LocalName == elementLocalName && element.NamespaceURI == elementNs)
                {
                    return element;
                }
            }
            return null;
        }

        public class XmlElementDescr
        {
            public string NamespaceURI;
            public string LocalName;
            public IList<XmlAttributeDescr> Attributes;

            public XmlElementDescr(XmlTextReader reader)
            {
                NamespaceURI = reader.NamespaceURI;
                LocalName = reader.LocalName;
            }

            public void SetAttribute(XmlTextReader reader)
            {
                var namespaceURI = reader.NamespaceURI;
                var localName = reader.LocalName;
                var value = reader.Value;

                int p = localName.IndexOf('.');
                if (p > 0)
                {
                    var clsName = localName.Substring(0, p);
                    if (clsName == LocalName &&
                        (String.IsNullOrEmpty(namespaceURI) || namespaceURI == NamespaceURI))
                    {
                        //Remove redundant element specification in attribute name
                        localName = localName.Substring(p + 1);
                    }
                }

                SetAttribute(new XmlAttributeDescr(namespaceURI, localName, value));
            }

            public void SetAttribute(XmlAttributeDescr attributeDescr)
            {
                if (Attributes == null)
                {
                    Attributes = new List<XmlAttributeDescr>();
                }
                Attributes.Add(attributeDescr);
            }

            public string GetAttribute(string attributeName)
            {
                if (Attributes == null) return null;

                foreach (var attr in Attributes)
                {
                    if (attr.LocalName == attributeName &&
                        (String.IsNullOrEmpty(attr.NamespaceURI) || attr.NamespaceURI == NamespaceURI))
                    {
                        return attr.Value;
                    }
                }
                return null;
            }

        }

        public class XmlAttributeDescr
        {
            public string NamespaceURI;
            public string LocalName;
            public string Value;

            //public XmlAttributeDescr(XmlTextReader reader)
            //{
            //    NamespaceURI = reader.NamespaceURI;
            //    LocalName = reader.LocalName;
            //    Value = reader.Value;
            //}

            public XmlAttributeDescr(string namespaceUri, string localName, string value)
            {
                NamespaceURI = namespaceUri;
                LocalName = localName;
                Value = value;
            }
        }
    }
}