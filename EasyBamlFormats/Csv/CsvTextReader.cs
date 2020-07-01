using System;
using System.Collections;
using System.IO;
using System.Text;

namespace EasyBamlFormats.Csv
{
    /// <summary>
    /// Reader that reads value from a CSV file or Tab-separated TXT file
    /// </summary>
    internal class CsvTextReader : IDisposable
    {
        private readonly int delimiter; // delimiter
        private readonly TextReader reader; // internal text reader
        private ArrayList columns; // An arraylist storing all the columns of a row

        internal CsvTextReader(char delimiter, Stream stream)
        {
            this.delimiter = delimiter;
            if (stream == null)
                throw new ArgumentNullException("stream");

            reader = new StreamReader(stream);
        }

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Close();
        }

        #endregion

        internal bool ReadRow()
        {
            // currentChar is the first char after newlines
            int currentChar = SkipAllNewLine();

            if (currentChar < 0)
            {
                // nothing else to read
                return false;
            }

            ReadState currentState = ReadState.TokenStart;
            columns = new ArrayList();

            var buffer = new StringBuilder();

            while (currentState != ReadState.LineEnd)
            {
                switch (currentState)
                {
                        // start of a token
                    case ReadState.TokenStart:
                        {
                            if (currentChar == delimiter)
                            {
                                // it is the end of the token when we see a delimeter
                                // Store token, and reset state. and ignore this char
                                StoreTokenAndResetState(ref buffer, ref currentState);
                            }
                            else if (currentChar == '\"')
                            {
                                // jump to Quoted content if it token starts with a quote.
                                // and also ignore this quote
                                currentState = ReadState.QuotedContent;
                            }
                            else if (currentChar == '\n' ||
                                     (currentChar == '\r' && reader.Peek() == '\n'))
                            {
                                // we see a '\n' or '\r\n' sequence. Go to LineEnd
                                // ignore these chars
                                currentState = ReadState.LineEnd;
                            }
                            else
                            {
                                // safe to say that this is part of a unquoted content
                                buffer.Append((Char) currentChar);
                                currentState = ReadState.UnQuotedContent;
                            }
                            break;
                        }

                        // inside of an unquoted content
                    case ReadState.UnQuotedContent:
                        {
                            if (currentChar == delimiter)
                            {
                                // It is then end of a toekn.
                                // Store the token value and reset state
                                // igore this char as well
                                StoreTokenAndResetState(ref buffer, ref currentState);
                            }
                            else if (currentChar == '\n' ||
                                     (currentChar == '\r' && reader.Peek() == '\n'))
                            {
                                // see a new line
                                // igorne these chars and jump to LineEnd
                                currentState = ReadState.LineEnd;
                            }
                            else
                            {
                                // we are good. store this char
                                // notice, even we see a '\"', we will just treat it like 
                                // a normal char
                                buffer.Append((Char) currentChar);
                            }
                            break;
                        }

                        // inside of a quoted content
                    case ReadState.QuotedContent:
                        {
                            if (currentChar == '\"')
                            {
                                // now it depends on whether the next char is quote also
                                if (reader.Peek() == '\"')
                                {
                                    // we will ignore the next quote.
                                    currentChar = reader.Read();
                                    buffer.Append((Char) currentChar);
                                }
                                else
                                {
                                    // we have a single quote. We fall back to unquoted content state
                                    // and igorne the curernt quote
                                    currentState = ReadState.UnQuotedContent;
                                }
                            }
                            else
                            {
                                // we are still inside of a quote, anything is accepted
                                buffer.Append((Char) currentChar);
                            }
                            break;
                        }
                }

                // read in the next char
                currentChar = reader.Read();

                if (currentChar < 0)
                {
                    // break out of the state machine if we reach the end of the file
                    break;
                }
            }

            // we got to here either we are at LineEnd, or we are end of file
            if (buffer.Length > 0)
            {
                columns.Add(buffer.ToString());
            }
            return true;
        }

        internal string GetColumn(int index)
        {
            if (columns != null && index < columns.Count && index >= 0)
            {
                return (string) columns[index];
            }
            return null;
        }

        internal void Close()
        {
            if (reader != null)
            {
                reader.Close();
            }
        }

        //---------------------------------
        // private functions
        //---------------------------------

        private void StoreTokenAndResetState(ref StringBuilder buffer, ref ReadState currentState)
        {
            // add the token into buffer. The token can be empty
            columns.Add(buffer.ToString());

            // create a new buffer for the next token.
            buffer = new StringBuilder();

            // we continue to token state state
            currentState = ReadState.TokenStart;
        }

        // skip all new line and return the first char after newlines.
        // newline means '\r\n' or '\n'
        private int SkipAllNewLine()
        {
            int c;
            while ((c = reader.Read()) >= 0)
            {
                if (c == '\n')
                {
                    continue; // continue if it is '\n'
                }
                if (c == '\r' && reader.Peek() == '\n')
                {
                    // skip the '\n' in the next position
                    reader.Read();

                    // and continue
                    continue;
                }
                // stop here
                break;
            }
            return c;
        }

        #region Nested type: ReadState

        /// <summary>
        /// Enum representing internal states of the reader when reading 
        /// the CSV or tab-separated TXT file
        /// </summary>
        private enum ReadState
        {
            /// <summary>
            /// State in which the reader is at the start of a column
            /// </summary>
            TokenStart,

            /// <summary>
            /// State in which the reader is reading contents that are quoted
            /// </summary>
            QuotedContent,

            /// <summary>
            /// State in which the reader is reading contents not in quotes
            /// </summary>
            UnQuotedContent,

            /// <summary>
            /// State in which the end of a line is reached
            /// </summary>
            LineEnd,
        }

        #endregion
    }
}