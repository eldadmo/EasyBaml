using System;
using System.IO;
using System.Text;

namespace EasyBamlFormats.Csv
{
    /// <summary>
    /// the class that writes to a text file either tab delimited or comma delimited. 
    /// </summary>
    internal sealed class CsvTextWriter : IDisposable
    {
        //-------------------------------
        // constructor 
        //-------------------------------
        internal CsvTextWriter(char delimiter, Stream output)
        {
            this.delimiter = delimiter;

            if (output == null)
                throw new ArgumentNullException("output");

            // show utf8 byte order marker
            var encoding = new UTF8Encoding(true);
            writer = new StreamWriter(output, encoding);
            firstColumn = true;
        }

        #region internal methods

        //-----------------------------------
        // Internal methods
        //-----------------------------------
        internal void WriteColumn(string value)
        {
            if (value == null)
                value = string.Empty;

            // if it contains delimeter, quote, newline, we need to escape them
            if (value.IndexOfAny(new[] {'\"', '\r', '\n', delimiter}) >= 0)
            {
                // make a string builder at the minimum required length;
                var builder = new StringBuilder(value.Length + 2);

                // put in the opening quote
                builder.Append('\"');

                // double quote each quote
                for (int i = 0; i < value.Length; i++)
                {
                    builder.Append(value[i]);
                    if (value[i] == '\"')
                    {
                        builder.Append('\"');
                    }
                }

                // put in the closing quote
                builder.Append('\"');
                value = builder.ToString();
            }

            if (!firstColumn)
            {
                // if we are not the first column, we write delimeter
                // to seperate the new cell from the previous ones.
                writer.Write(delimiter);
            }
            else
            {
                firstColumn = false; // set false
            }

            writer.Write(value);
        }

        internal void EndLine()
        {
            // write a new line
            writer.WriteLine();

            // set first column to true    
            firstColumn = true;
        }

        internal void Close()
        {
            if (writer != null)
            {
                writer.Close();
            }
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            Close();
        }

        #endregion

        #region private members

        private readonly char delimiter;
        private readonly TextWriter writer;
        private bool firstColumn;

        #endregion
    }
}