using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MARC4J.Net.Properties;

namespace MARC4J.Net.Converter
{
    /// <summary>
    /// <c>CodeTable</c> defines a data structure to facilitate
    /// <c>AnselToUnicode</c> char conversion.
    /// </summary>
    public class CodeTable : ICodeTableInterface
    {
        #region Default Instances

        private static ICodeTableInterface _defaultMultibyteLoadedCodeTable = null;
        public static ICodeTableInterface DefaultMultibyteLoadedCodeTable
        {
            get
            {
                if (_defaultMultibyteLoadedCodeTable == null)
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.codetables)))
                    {
                        _defaultMultibyteLoadedCodeTable = new CodeTable(stream);
                    }
                }
                return _defaultMultibyteLoadedCodeTable;
            }
        }

        private static ICodeTableInterface _defaultCodeTable = null;
        public static ICodeTableInterface DefaultCodeTable
        {
            get
            {
                if (_defaultCodeTable == null)
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.codetablesnocjk)))
                    {
                        _defaultCodeTable = new CodeTable(stream);
                    }
                }
                return _defaultCodeTable;
            }
        }

        #endregion

        #region Fields

        protected IDictionary<int, IDictionary<int, char>> charsets = null;

        protected IDictionary<int, HashSet<int>> combining = null;

        #endregion

        #region Ctors

        public CodeTable(Stream byteStream)
        {
            try
            {
                var codeTableReader = new CodeTableXmlReader();
                codeTableReader.Read(byteStream);
                charsets = codeTableReader.CharSets;
                combining = codeTableReader.CombiningChars;
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
        }

        public CodeTable(String filename)
        {
            try
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    var codeTableReader = new CodeTableXmlReader();
                    codeTableReader.Read(fs);
                    charsets = codeTableReader.CharSets;
                    combining = codeTableReader.CombiningChars;
                }
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
        }

        public CodeTable(Uri uri)
        {
            try
            {
                var response = WebRequest.Create(uri).GetResponse();
                using (var stream = response.GetResponseStream())
                {
                    var codeTableReader = new CodeTableXmlReader();
                    codeTableReader.Read(stream);
                    charsets = codeTableReader.CharSets;
                    combining = codeTableReader.CombiningChars;
                }

                response.Close();
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
        }

        #endregion

        #region ICodeTableInterface implementation
        public bool IsCombining(int i, int g0, int g1)
        {
            if (i <= 0x7E)
            {
                var v = combining[g0];
                return (v != null && v.Contains(i));
            }
            else
            {
                var v = combining[g1];
                return v != null && v.Contains(i);
            }
        }

        public char GetChar(int c, int mode)
        {
            if (c == 0x20)
                return (char)c;
            else
            {
                var charset = charsets[mode];

                if (charset == null)
                {
                    return (char)c;
                }
                else
                {
                    char ch = charset[c];
                    if (ch == null)
                    {
                        int newc = (c < 0x80) ? c + 0x80 : c - 0x80;
                        ch = charset[newc];
                        if (ch == null)
                        {
                            return (char)0;
                        }
                        else
                            return ch;
                    }
                    else
                        return ch;
                }
            }
        }

        #endregion

        #region Disposing

        private bool _disposed = false;
        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool isDisposing)
        {
            if (!_disposed)
            {
                if (isDisposing)
                {
                    //if (charsets != null)
                    //{
                    //    charsets.Clear();
                    //    charsets = null;
                    //}
                    //if (combining != null)
                    //{
                    //    combining.Clear();
                    //    combining = null;
                    //}

                    //GC.SuppressFinalize(this);
                    //_disposed = true;
                }
            }
        }

        ~CodeTable()
        {
            Dispose();
        }

        #endregion
    }
}