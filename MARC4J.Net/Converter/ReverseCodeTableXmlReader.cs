using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Linq;
using MARC4J.Net.Properties;

namespace MARC4J.Net.Converter
{
    /// <summary>
    /// Builds a data structure to facilitate <c>UnicodeToAnsel</c>
    /// char conversion.
    /// </summary>
    public class ReverseCodeTableXmlReader
    {
        #region Fields

        private IDictionary<char, IDictionary<int, char[]>> charsets;

        private HashSet<char> combiningchars;

        private bool useAlt = false;

        /** Data element identifier */
        private int isocode;

        private char[] marc;

        private char ucs;

        private bool combining; 

        #endregion

        #region Properties

        public IDictionary<char, IDictionary<int, char[]>> CharSets
        {
            get
            {
                return charsets;
            }
        }

        public HashSet<char> CombiningChars
        {
            get
            {
                return combiningchars;
            }
        }

        #endregion

        #region Methods
        public void Read(Stream stream)
        {
            var root = XElement.Load(stream);
            combiningchars = new HashSet<char>();
            charsets = root.Descendants("characterSet")
                       .SelectMany(a =>
                       {
                           isocode = Convert.ToInt32((a.Attribute("ISOcode").Value), 16);
                           return a.Elements("code")
                                   .Select(c =>
                                   {
                                       #region Code
                                       combining = c.Elements("isCombining").Any() ?
                                                   c.Element("isCombining").Value == "true" : false;
                                       var marcStr = c.Element("marc").Value;
                                       if (marcStr.Length == 6)
                                       {
                                           marc = new char[3];
                                           marc[0] = (char)Convert.ToInt32(marcStr.Substring(0, 2), 16);
                                           marc[1] = (char)Convert.ToInt32(marcStr.Substring(2, 2), 16);
                                           marc[2] = (char)Convert.ToInt32(marcStr.Substring(4, 2), 16);
                                       }
                                       else
                                       {
                                           marc = new char[1];
                                           marc[0] = (char)Convert.ToInt32(marcStr, 16);
                                       }
                                       if (c.Elements("ucs").Any())
                                       {
                                           var ucsVal = c.Element("ucs").Value;
                                           if (!string.IsNullOrEmpty(ucsVal))
                                               ucs = (char)Convert.ToInt32(ucsVal, 16);
                                           else
                                               useAlt = true;
                                       }
                                       else
                                           if (c.Elements("alt").Any())
                                           {
                                               var altVal = c.Element("alt").Value;
                                               if (useAlt && !string.IsNullOrEmpty(altVal))
                                               {
                                                   ucs = (char)Convert.ToInt32(altVal, 16);
                                                   useAlt = false;
                                               }
                                           }

                                       if (combining)
                                           combiningchars.Add(ucs);
                                       return new
                                       {
                                           ucs = ucs,
                                           set = new
                                           {
                                               isocode = isocode,
                                               marc = marc
                                           }
                                       };
                                       #endregion
                                   }).ToArray();

                       })
                       .GroupBy(a => a.ucs)
                       .ToDictionary(d =>
                           d.Key,
                           d => d.Select(a=>a.set)
                                 .GroupBy(a=>a.isocode)
                                 .ToDictionary(v => v.Key, v => v.SelectMany(c=>c.marc).ToArray()) as IDictionary<int, char[]>);
        } 

        #endregion
    }
}