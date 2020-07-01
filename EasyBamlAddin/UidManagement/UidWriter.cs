using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace EasyBamlAddin.UidManagement
{
    internal sealed class UidWriter
    {
        private static readonly Regex EscapedXmlEntities = new Regex("(<|>|\"|'|&)",
                                                                     RegexOptions.Compiled |
                                                                     RegexOptions.CultureInvariant);

        private static readonly MatchEvaluator EscapeMatchEvaluator = EscapeMatch;
        private readonly UidCollector collector;
        private readonly LineBuffer lineBuffer;
        private readonly TextReader sourceReader;
        private readonly TextWriter targetWriter;
        private int currentLineNumber = 1;
        private int currentLinePosition = 1;

        internal UidWriter(UidCollector collector, TextReader source, TextWriter target)
        {
            this.collector = collector;
            sourceReader = source;
            //var encoding = new UTF8Encoding(true);
            targetWriter = target;
            lineBuffer = new LineBuffer(sourceReader.ReadLine());
        }

        internal bool UpdateUidWrite(IUidUpdateHandleStrategy uidUpdateHandleStrategy)
        {
            bool result;
            try
            {
                WriteNewNamespace(uidUpdateHandleStrategy);

                for (int i = 0; i < collector.Count; i++)
                {
                    Uid uid = collector[i];
                    var action = uidUpdateHandleStrategy.GetAction(uid);
                    switch (action)
                    {
                        case UidUpdateHandleAction.Skip:
                            break;
                        case UidUpdateHandleAction.Add:
                            WriteTillSourcePosition(uid.LineNumber, uid.LinePosition);
                            if (uid.Space == SpaceInsertion.BeforeUid)
                            {
                                WriteSpace();
                            }
                            WriteNewUid(uid);
                            if (uid.Space == SpaceInsertion.AfterUid)
                            {
                                WriteSpace();
                            }
                            break;
                        case UidUpdateHandleAction.UpdateValue:
                            WriteTillSourcePosition(uid.LineNumber, uid.LinePosition);
                            ProcessAttributeStart(WriterAction.Write);
                            SkipSourceAttributeValue();
                            WriteNewAttributeValue(uid.Value);
                            break;
                        case UidUpdateHandleAction.Remove:
                            WriteTillSourcePosition(uid.LineNumber, uid.LinePosition - 1);
                            ProcessAttributeStart(WriterAction.Skip);
                            SkipSourceAttributeValue();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
                WriteTillEof();
                result = true;
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException || exception is SEHException)
                {
                    throw;
                }
                result = false;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        private void WriteNewNamespace(IUidUpdateHandleStrategy uidUpdateHandleStrategy)
        {
            if (uidUpdateHandleStrategy.GetAddNewNamespace(collector))
            {
                WriteTillSourcePosition(collector.RootElementLineNumber, collector.RootElementLinePosition);
                WriteElementTag();
                WriteSpace();
                WriteNewNamespace();
            }
        }

        /*
        internal bool RemoveUidWrite()
        {
            bool result;
            try
            {
                for (int i = 0; i < collector.Count; i++)
                {
                    Uid uid = collector[i];
                    if (uid.Status == UidStatus.Duplicate || uid.Status == UidStatus.Valid)
                    {
                        WriteTillSourcePosition(uid.LineNumber, uid.LinePosition - 1);
                        ProcessAttributeStart(WriterAction.Skip);
                        SkipSourceAttributeValue();
                    }
                }
                WriteTillEof();
                result = true;
            }
            catch (Exception exception)
            {
                if (exception is NullReferenceException || exception is SEHException)
                {
                    throw;
                }
                result = false;
            }
            catch
            {
                result = false;
            }
            return result;
        }
        */

        private void WriteTillSourcePosition(int lineNumber, int linePosition)
        {
            while (currentLineNumber < lineNumber)
            {
                targetWriter.WriteLine(lineBuffer.ReadToEnd());
                currentLineNumber++;
                currentLinePosition = 1;
                lineBuffer.SetLine(sourceReader.ReadLine());
            }
            while (currentLinePosition < linePosition)
            {
                targetWriter.Write(lineBuffer.Read());
                currentLinePosition++;
            }
        }

        private void WriteElementTag()
        {
            if (lineBuffer.EOL)
            {
                AdvanceTillNextNonEmptyLine(WriterAction.Write);
            }
            char c = lineBuffer.Peek();
            while (!char.IsWhiteSpace(c) && c != '/' && c != '>')
            {
                targetWriter.Write(c);
                currentLinePosition++;
                lineBuffer.Read();
                if (lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Write);
                }
                c = lineBuffer.Peek();
            }
        }

        private void WriteNewUid(Uid uid)
        {
            string text = (uid.NamespacePrefix == null)
                              ? (collector.NamespaceAddedForMissingUid + ":Uid")
                              : (uid.NamespacePrefix + ":Uid");
            string text2 = EscapedXmlEntities.Replace(uid.Value, EscapeMatchEvaluator);
            string value = string.Format("{0}=\"{1}\"", text, text2);
            targetWriter.Write(value);
        }

        private void WriteNewNamespace()
        {
            string value = string.Format("xmlns:{0}=\"{1}\"",
                                         collector.NamespaceAddedForMissingUid,
                                         "http://schemas.microsoft.com/winfx/2006/xaml");
            targetWriter.Write(value);
        }

        private void WriteNewAttributeValue(string value)
        {
            EscapedXmlEntities.Replace(value, EscapeMatchEvaluator);
            targetWriter.Write(string.Format("\"{0}\"", value));
        }

        private void WriteSpace()
        {
            targetWriter.Write(" ");
        }

        private void WriteTillEof()
        {
            targetWriter.WriteLine(lineBuffer.ReadToEnd());
            targetWriter.Write(sourceReader.ReadToEnd());
            targetWriter.Flush();
        }

        private void SkipSourceAttributeValue()
        {
            char c = '\0';
            while (c != '"' && c != '\'')
            {
                if (lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Skip);
                }
                c = lineBuffer.Read();
                currentLinePosition++;
            }
            char c2 = c;
            c = '\0';
            while (c != c2)
            {
                if (lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(WriterAction.Skip);
                }
                c = lineBuffer.Read();
                currentLinePosition++;
            }
        }

        private void AdvanceTillNextNonEmptyLine(WriterAction action)
        {
            do
            {
                if (action == WriterAction.Write)
                {
                    targetWriter.WriteLine();
                }
                lineBuffer.SetLine(sourceReader.ReadLine());
                currentLineNumber++;
                currentLinePosition = 1;
            } while (lineBuffer.EOL);
        }

        private void ProcessAttributeStart(WriterAction action)
        {
            if (lineBuffer.EOL)
            {
                AdvanceTillNextNonEmptyLine(action);
            }
            char c;
            do
            {
                c = lineBuffer.Read();
                if (action == WriterAction.Write)
                {
                    targetWriter.Write(c);
                }
                currentLinePosition++;
                if (lineBuffer.EOL)
                {
                    AdvanceTillNextNonEmptyLine(action);
                }
            } while (c != '=');
        }

        private static string EscapeMatch(Match match)
        {
            string value;
            if ((value = match.Value) != null)
            {
                if (value == "<")
                {
                    return "&lt;";
                }
                if (value == ">")
                {
                    return "&gt;";
                }
                if (value == "&")
                {
                    return "&amp;";
                }
                if (value == "\"")
                {
                    return "&quot;";
                }
                if (value == "'")
                {
                    return "&apos;";
                }
            }
            return match.Value;
        }

        #region Nested type: LineBuffer

        private sealed class LineBuffer
        {
            private string content;
            private int index;

            public LineBuffer(string line)
            {
                SetLine(line);
            }

            public bool EOL
            {
                get { return index == content.Length; }
            }

            public void SetLine(string line)
            {
                content = (line ?? string.Empty);
                index = 0;
            }

            public char Read()
            {
                if (!EOL)
                {
                    return content[index++];
                }
                throw new InvalidOperationException();
            }

            public char Peek()
            {
                if (!EOL)
                {
                    return content[index];
                }
                throw new InvalidOperationException();
            }

            public string ReadToEnd()
            {
                if (!EOL)
                {
                    int p = index;
                    index = content.Length;
                    return content.Substring(p);
                }
                return string.Empty;
            }
        }

        #endregion

        #region Nested type: WriterAction

        private enum WriterAction
        {
            Write,
            Skip
        }

        #endregion
    }

    public enum UidUpdateHandleAction
    {
        Skip,
        Add,
        UpdateValue,
        Remove
    }

    public interface IUidUpdateHandleStrategy
    {
        bool GetAddNewNamespace(UidCollector uidCollector);

        UidUpdateHandleAction GetAction(Uid uid);
    }

    public class DefaultUidUpdateHandleStrategy : IUidUpdateHandleStrategy
    {
        private readonly bool isRemoving;

        public DefaultUidUpdateHandleStrategy(bool isRemoving)
        {
            this.isRemoving = isRemoving;
        }

        public bool GetAddNewNamespace(UidCollector uidCollector)
        {
            if (isRemoving) return false;

            return (uidCollector.NamespaceAddedForMissingUid != null);
        }

        public UidUpdateHandleAction GetAction(Uid uid)
        {
            if (isRemoving)
            {
                switch (uid.Status)
                {
                    case UidStatus.Duplicate:
                    case UidStatus.Valid:
                        return UidUpdateHandleAction.Remove;
                    case UidStatus.Absent:
                        return UidUpdateHandleAction.Skip;
                    case UidStatus.Unknown:
                        throw new InvalidOperationException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            else
            {
                switch (uid.Status)
                {
                    case UidStatus.Duplicate:
                        return UidUpdateHandleAction.UpdateValue;
                    case UidStatus.Valid:
                        return UidUpdateHandleAction.Skip;
                    case UidStatus.Absent:
                        return UidUpdateHandleAction.Add;
                    case UidStatus.Unknown:
                        throw new InvalidOperationException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }                
            }
        }
    }
}