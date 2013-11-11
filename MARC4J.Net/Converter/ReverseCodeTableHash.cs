using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using MARC4J.Net.Properties;
using System.Net;

namespace MARC4J.Net.Converter
{
    /// <summary>
    ///  <c>ReverseCodeTableHash</c> defines a data structure to facilitate
    ///  UnicodeToAnsel character conversion.
    /// </summary>
    public class ReverseCodeTableHash : ReverseCodeTable
    {
        #region Default Instance

        private static ReverseCodeTableHash _defaultInstance;
        public static ReverseCodeTableHash DefaultInstance
        {
            get 
            {
                if (_defaultInstance == null)
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(Resources.codetables)))
                    {
                        _defaultInstance = new ReverseCodeTableHash(stream);
                    }
                }
                return _defaultInstance; 
            }
        }

        #endregion

        #region Fields
        
        protected IDictionary<char, IDictionary<int, char[]>> charsets = null;

        protected HashSet<char> combining = null; 

        #endregion

        public override bool IsCombining(char c)
        {
            return combining.Contains(c);
        }

        public override IDictionary<int, char[]> GetCharTable(char c)
        {
            return charsets[c];
        }

        public ReverseCodeTableHash(Stream byteStream)
        {
            try
            {
                var reader = new ReverseCodeTableXmlReader();
                reader.Read(byteStream);
                charsets = reader.CharSets;
                combining = reader.CombiningChars;
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }

        }

        public ReverseCodeTableHash(String filename)
        {
            try
            {
                using (var fs = new FileStream(filename, FileMode.Open))
                {
                    var reader = new ReverseCodeTableXmlReader();
                    reader.Read(fs);
                    charsets = reader.CharSets;
                    combining = reader.CombiningChars;
                }
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
        }

        public ReverseCodeTableHash(Uri uri)
        {
            try
            {
                using (var stream = WebRequest.Create(uri).GetResponse().GetResponseStream())
                {
                    var reader = new ReverseCodeTableXmlReader();
                    reader.Read(stream);
                    charsets = reader.CharSets;
                    combining = reader.CombiningChars;
                }
            }
            catch (Exception e)
            {
                throw new MarcException(e.Message, e);
            }
        }
    }
}