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
    /// <c>ReverseCodeTable</c> is a set of methods to facilitate Unicode to MARC-8
    /// char conversion, it tracks the current charset encodings that are in use, 
    /// and defines abstract methods IsCombining() and GetCharTable()which must be overridden
    /// in a sub-class to actually implement the Unicode to MARC8 char conversion.  
    /// </summary>
    public abstract class ReverseCodeTable
    {
        static readonly byte G0 = 0;
        static readonly byte G1 = 1;

        /// <summary>
        /// Abstract method that must be defined in a sub-class, used in the conversion of Unicode
        /// to MARC-8.  For a given Unicode char, determine whether that char is a combining
        /// char (an accent mark or diacritic)  
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if char is a combining char</returns>
        abstract public bool IsCombining(char c);

        /**
         * Abstract method that must be defined in a sub-class, used in the conversion of Unicode
         * to MARC-8. For a given Unicode char, return ALL of the possible MARC-8 representations
         * of that char.  These are represented in a IDictionary where the key is the ISOcode of the 
         * char set in MARC-8 that the Unicode char appears in, and the value is the MARC-8
         * char value in that char set that encodes the given Unicode char.
         *  
         * @param c - the UCS/Unicode char to look up
         * @return IDictionary - contains all of the possible MARC-8 representations of that Unicode char
         */
        abstract public IDictionary<int, char[]> GetCharTable(char c);

        protected char? lastLookupKey = null;
        protected IDictionary<int, char[]> lastLookupValue = null;
        protected byte[] g;
        protected String charsetsUsed;

        /// <summary>
        /// Default constructor for the abstract class, allocates and initializes the structures that
        /// are used to track the current char sets in use. 
        /// </summary>
        public ReverseCodeTable()
        {
            g = new byte[2];
            Init();
        }

        /// <summary>
        /// Initializes the ReverseCodeTable state to the default value for encoding a field.
        /// </summary>
        public void Init()
        {
            g[0] = 0x42;
            g[1] = 0x45;
            charsetsUsed = "BE";
        }

        /// <summary>
        /// Routine used for tracking which char set is currently in use for chars less than 0x80
        /// </summary>
        /// <returns>the current G0 char set in use.</returns>
        public byte GetPreviousG0()
        {
            return g[G0];
        }

        /// <summary>
        /// Routine used for tracking which char set is currently in use for chars greater than 0x80
        /// </summary>
        /// <returns></returns>
        public byte GetPreviousG1()
        {
            return g[G1];
        }

        /// <summary>
        /// Routine used for changing which char set is currently in use for chars less than 0x80
        /// </summary>
        /// <param name="table"></param>
        public void SetPreviousG0(int table)
        {
            g[G0] = (byte)table;
        }

        /// <summary>
        /// Routine used for changing which char set is currently in use for chars greater than 0x80
        /// </summary>
        /// <param name="table"></param>
        public void SetPreviousG1(int table)
        {
            g[G1] = (byte)table;
        }

        /// <summary>
        /// Performs a lookup of the MARC8 translation of a given Unicode char.  Caches the results 
        /// in lastLookupKey and lastLookupValue so that subsequent lookups made in processing the same
        /// char will proceed more quickly.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>contains all of the possible MARC-8 representations of that Unicode char</returns>
        public IDictionary<int, char[]> CodeTableHash(char c)
        {
            if (lastLookupKey != null && c.Equals(lastLookupKey))
            {
                return (lastLookupValue);
            }
            lastLookupKey = c;
            lastLookupValue = GetCharTable(c);
            return lastLookupValue;
        }

        /// <summary>
        /// Checks whether a MARC8 translation of a given Unicode char exists.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is one or more MARC-8 representation of the given Unicode char.</returns>
        public bool CharHasMatch(char c)
        {
            return CodeTableHash(c) != null;
        }

        /// <summary>
        /// Checks whether a MARC8 translation of a given Unicode char exists in the char set currently
        /// loaded as the G0 char set.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G0 char set</returns>
        public bool InPreviousG0CharEntry(char c)
        {
            IDictionary<int, char[]> chars = CodeTableHash(c);
            if (chars != null && chars.ContainsKey((int)GetPreviousG0()))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether a MARC8 translation of a given Unicode char exists in the char set currently 
        /// loaded as the G1 char set.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G1 char set</returns>
        public bool InPreviousG1CharEntry(char c)
        {
            IDictionary<int, char[]> chars = CodeTableHash(c);
            if (chars != null && chars.ContainsKey((int)GetPreviousG1()))
                return true;
            return false;
        }

        /// <summary>
        /// Returns the MARC8 translation of a given Unicode char from the char set currently 
        /// loaded as the G0 char set.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G0 char set</returns>
        public char[] GetCurrentG0CharEntry(char c)
        {
            return GetCharEntry(c, GetPreviousG0());
        }

        /// <summary>
        /// Returns the MARC8 translation of a given Unicode char from the char set currently 
        /// loaded as the G0 char set.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G0 char set</returns>
        public char[] GetCurrentG1CharEntry(char c)
        {
            return GetCharEntry(c, GetPreviousG1());
        }

        /// <summary>
        /// Returns the MARC8 translation of a given Unicode char from the char set currently 
        /// loaded as either the G0 or the G1 char set, as specified by the second parameter.
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <param name="charset">whether to use the current G0 charset of the current G1 charset to perform the lookup</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G0 char set</returns>
        public char[] GetCharEntry(char c, int charset)
        {
            IDictionary<int, char[]> chars = CodeTableHash(c);
            if (chars == null)
            {
                return new char[] { };
            }
            return chars[charset];
        }

        /// <summary>
        /// Lookups up the MARC8 translation of a given Unicode char and determines which of the MARC-8 
        /// char sets that have a translation for that Unicode char is the best one to use. 
        /// If one one charset has a translation, that one will be returned.  If more than one charset has a 
        /// translation
        /// <para>
        /// charset - whether to use the current G0 charset of the current G1 charset to perform the lookup
        /// </para>
        /// </summary>
        /// <param name="c">the UCS/Unicode char to look up</param>
        /// <returns>true if there is a MARC-8 representation of the given Unicode char in the current G0 char set</returns>
        public char GetBestCharSet(char c)
        {
            IDictionary<int, char[]> chars = CodeTableHash(c);
            if (chars.Keys.Count == 1)
                return (char)chars.Keys.FirstOrDefault();
            for (int i = 0; i < charsetsUsed.Length; i++)
            {
                char toUse = charsetsUsed[i];
                if (chars.ContainsKey(toUse))
                {
                    return toUse;
                }
            }
            char returnVal;
            if (chars.ContainsKey('S'))
            {
                returnVal = 'S';
            }
            else
            {
                returnVal = (char)chars.Keys.FirstOrDefault();
            }
            charsetsUsed = charsetsUsed + returnVal;
            return returnVal;
        }

        /// <summary>
        /// Utility function for translating a String consisting of one or more two char hex string
        /// of the char values into a char array containing those chars
        /// </summary>
        /// <param name="str">A string containing the two-char hex strings of chars to decode</param>
        /// <returns>char[] - an array of chars represented by the </returns>
        public static char[] DeHexify(String str)
        {
            char[] result = null;
            if (str.Length == 2)
            {
                result = new char[1];
                result[0] = (char)Convert.ToInt32(str, 16);
            }
            else if (str.Length == 6)
            {
                result = new char[3];
                result[0] = (char)Convert.ToInt32(str.Substring(0, 2), 16);
                result[1] = (char)Convert.ToInt32(str.Substring(2, 4), 16);
                result[2] = (char)Convert.ToInt32(str.Substring(4, 6), 16);
            }
            return result;
        }
    }
}