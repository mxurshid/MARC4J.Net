using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MARC4J.Net.Properties;

namespace MARC4J.Net.Converter
{
    /// <summary>
    /// <para/>
    /// A utility to convert MARC-8 data to non-precomposed UCS/Unicode.
    /// <para/>
    /// The MARC-8 to Unicode mapping used is the version with the March 2005
    /// revisions.
    /// <para/>
    /// </summary>
    public class AnselToUnicode : CharConverter
    {
        class CodeTracker
        {
            public int offset;

            public int g0;

            public int g1;

            public bool multibyte;

            public override string ToString()
            {
                return "Offset: " + offset + " G0: " + g0.ToString("X")
                        + " G1: " + g1.ToString("X") + " Multibyte: "
                        + multibyte;
            }
        }

        protected ICodeTableInterface ct;

        protected bool loadedMultibyte = false;

        // flag that indicates whether Numeric Character References of the form &#XXXX; should be translated to the
        // unicode code point specified by the 4 hexidecimal digits. As described on this page 
        //  http://www.loc.gov/marc/specifications/speccharconversion.html#lossless
        protected bool translateNCR = false;

        public bool ShouldTranslateNCR()
        {
            return translateNCR;
        }

        public void SetTranslateNCR(bool translateNCR)
        {
            this.translateNCR = translateNCR;
        }

        /// <summary>
        /// Should return true if the CharConverter outputs Unicode encoded characters
        /// </summary>
        public override bool OutputsUnicode()
        {
            return true;
        }

        protected ErrorHandler errorList = null;

        /// <summary>
        ///  Creates a new instance and loads the MARC4J.Net supplied
        ///  conversion tables based on the official LC tables.
        /// </summary>
        public AnselToUnicode()
        {
            ct = LoadGeneratedTable(false);
        }

        /// <summary>
        /// Creates a new instance and loads the MARC4J.Net supplied
        /// conversion tables based on the official LC tables.
        /// </summary>
        /// <param name="loadMultibyte"></param>
        public AnselToUnicode(bool loadMultibyte)
        {
            ct = LoadGeneratedTable(loadMultibyte);
        }
        /// <summary>
        /// Creates a new instance and loads the MARC4J.Net supplied
        /// conversion tables based on the official LC tables.
        /// </summary>
        /// <param name="errorList"></param>
        public AnselToUnicode(ErrorHandler errorList)
        {
            ct = LoadGeneratedTable(false);
            this.errorList = errorList;
        }

        /// <summary>
        /// Creates a new instance and loads the MARC4J.Net supplied
        /// conversion tables based on the official LC tables.
        /// </summary>
        /// <param name="errorList"></param>
        /// <param name="loadMultibyte"></param>
        public AnselToUnicode(ErrorHandler errorList, bool loadMultibyte)
        {
            ct = LoadGeneratedTable(loadMultibyte);
            this.errorList = errorList;
        }

        private ICodeTableInterface LoadGeneratedTable(bool loadMultibyte)
        {
            ICodeTableInterface ct;
            if (loadMultibyte)
                ct = Converter.CodeTable.DefaultMultibyteLoadedCodeTable;
            else
                ct = Converter.CodeTable.DefaultCodeTable;
            loadedMultibyte = loadMultibyte;
            return ct;
        }

        /// <summary>
        /// Constructs an instance with the specified pathname.
        /// Use this constructor to create an instance with a customized code table
        /// mapping. The mapping file should follow the structure of LC's XML MARC-8
        /// to Unicode mapping (see:
        /// http://www.loc.gov/marc/specifications/codetables.xml).
        /// </summary>
        /// <param name="pathname"></param>
        public AnselToUnicode(String pathname)
        {
            ct = new CodeTable(pathname);
            loadedMultibyte = true;
        }

        /// <summary>
        /// Constructs an instance with the specified input stream.
        /// 
        /// Use this constructor to create an instance with a customized code table
        /// mapping. The mapping file should follow the structure of LC's XML MARC-8
        /// to Unicode mapping (see:
        /// http://www.loc.gov/marc/specifications/codetables.xml).
        /// </summary>
        /// <param name="?"></param>
        public AnselToUnicode(Stream stream)
        {
            ct = new CodeTable(stream);
            loadedMultibyte = true;
        }

        /// <summary>
        /// Loads the entire mapping (including multibyte characters) from the Library of Congress.
        /// </summary>
        private void LoadMultibyte()
        {
            ct = CodeTable.DefaultMultibyteLoadedCodeTable;
        }

        private void CheckMode(char[] data, CodeTracker cdt)
        {
            int extra = 0;
            int extra2 = 0;
            while (cdt.offset + extra + extra2 < data.Length && IsEscape(data[cdt.offset]))
            {
                if (cdt.offset + extra + extra2 + 1 == data.Length)
                {
                    cdt.offset += 1;
                    if (errorList != null)
                    {
                        errorList.AddError(Error.MINOR_ERROR, "Escape character found at end of field, discarding it.");
                    }
                    else
                    {
                        throw new MarcException("Escape character found at end of field");
                    }
                    break;
                }
                switch (data[cdt.offset + 1 + extra])
                {
                    case (char)0x28:  // '('
                    case (char)0x2c:  // ','
                        Set_cdt(cdt, 0, data, 2 + extra, false);
                        break;
                    case (char)0x29:  // ')'
                    case (char)0x2d:  // '-'
                        Set_cdt(cdt, 1, data, 2 + extra, false);
                        break;
                    case (char)0x24:  // '$'
                        if (!loadedMultibyte)
                        {
                            LoadMultibyte();
                            loadedMultibyte = true;
                        }
                        switch (data[cdt.offset + 2 + extra + extra2])
                        {
                            case (char)0x29:  // ')'
                            case (char)0x2d:  // '-'
                                Set_cdt(cdt, 1, data, 3 + extra + extra2, true);
                                break;
                            case (char)0x2c:  // ','
                                Set_cdt(cdt, 0, data, 3 + extra + extra2, true);
                                break;
                            case (char)0x31:  // '1'
                                cdt.g0 = data[cdt.offset + 2 + extra + extra2];
                                cdt.offset += 3 + extra + extra2;
                                cdt.multibyte = true;
                                break;
                            case (char)0x20:  // ' ' 
                                // space found in escape code: look ahead and try to proceed
                                extra2++;
                                break;
                            default:
                                // unknown code character found: discard escape sequence and return
                                cdt.offset += 1;
                                if (errorList != null)
                                {
                                    errorList.AddError(Error.MINOR_ERROR, "Unknown character set code found following escape character. Discarding escape character.");
                                }
                                else
                                {
                                    throw new MarcException("Unknown character set code found following escape character.");
                                }
                                break;
                        }
                        break;
                    case (char)0x67:  // 'g'
                    case (char)0x62:  // 'b'
                    case (char)0x70:  // 'p'
                        cdt.g0 = data[cdt.offset + 1 + extra];
                        cdt.offset += 2 + extra;
                        cdt.multibyte = false;
                        break;
                    case (char)0x73:  // 's'
                        cdt.g0 = 0x42;
                        cdt.offset += 2 + extra;
                        cdt.multibyte = false;
                        break;
                    case (char)0x20:  // ' ' 
                        // space found in escape code: look ahead and try to proceed
                        if (errorList == null)
                        {
                            throw new MarcException("Extraneous space character found within MARC8 character set escape sequence");
                        }
                        extra++;
                        break;
                    default:
                        // unknown code character found: discard escape sequence and return
                        cdt.offset += 1;
                        if (errorList != null)
                        {
                            errorList.AddError(Error.MINOR_ERROR, "Unknown character set code found following escape character. Discarding escape character.");
                        }
                        else
                        {
                            throw new MarcException("Unknown character set code found following escape character.");
                        }
                        break;
                }
            }
            if (errorList != null && (extra != 0 || extra2 != 0))
            {
                errorList.AddError(Error.ERROR_TYPO, "" + (extra + extra2) + " extraneous space characters found within MARC8 character set escape sequence");
            }
        }

        private void Set_cdt(CodeTracker cdt, int g0_or_g1, char[] data, int addnlOffset, bool multibyte)
        {
            if (data[cdt.offset + addnlOffset] == '!' && data[cdt.offset + addnlOffset + 1] == 'E')
            {
                addnlOffset++;
            }
            else if (data[cdt.offset + addnlOffset] == ' ')
            {
                if (errorList != null)
                {
                    errorList.AddError(Error.ERROR_TYPO, "Extraneous space character found within MARC8 character set escape sequence. Skipping over space.");
                }
                else
                {
                    throw new MarcException("Extraneous space character found within MARC8 character set escape sequence");
                }
                addnlOffset++;
            }
            else if ("(,)-$!".IndexOf(data[cdt.offset + addnlOffset]) != -1)
            {
                if (errorList != null)
                {
                    errorList.AddError(Error.MINOR_ERROR, "Extraneaous intermediate character found following escape character. Discarding intermediate character.");
                }
                else
                {
                    throw new MarcException("Extraneaous intermediate character found following escape character.");
                }
                addnlOffset++;
            }
            if ("34BE1NQS2".IndexOf(data[cdt.offset + addnlOffset]) == -1)
            {
                cdt.offset += 1;
                cdt.multibyte = false;
                if (errorList != null)
                {
                    errorList.AddError(Error.MINOR_ERROR, "Unknown character set code found following escape character. Discarding escape character.");
                }
                else
                {
                    throw new MarcException("Unknown character set code found following escape character.");
                }
            }
            else  // All is well, proceed normally
            {
                if (g0_or_g1 == 0) cdt.g0 = data[cdt.offset + addnlOffset];
                else cdt.g1 = data[cdt.offset + addnlOffset];
                cdt.offset += 1 + addnlOffset;
                cdt.multibyte = multibyte;
            }
        }
        /// <summary>
        /// Converts MARC-8 data to UCS/Unicode.
        /// </summary>
        /// <param name="data">the MARC-8 data in an array of char</param>
        /// <returns>the UCS/Unicode data</returns>
        public override String Convert(char[] data)
        {
            StringBuilder sb = new StringBuilder();
            int len = data.Length;

            CodeTracker cdt = new CodeTracker();

            cdt.g0 = 0x42;
            cdt.g1 = 0x45;
            cdt.multibyte = false;

            cdt.offset = 0;

            CheckMode(data, cdt);

            var diacritics = new Queue<char>();

            while (cdt.offset < data.Length)
            {
                if (ct.IsCombining(data[cdt.offset], cdt.g0, cdt.g1)
                        && HasNext(cdt.offset, len))
                {

                    while (cdt.offset < len && ct.IsCombining(data[cdt.offset], cdt.g0, cdt.g1)
                            && HasNext(cdt.offset, len))
                    {
                        char c = GetChar(data[cdt.offset], cdt.g0, cdt.g1);
                        if (c != 0) diacritics.Enqueue(c);
                        cdt.offset++;
                        CheckMode(data, cdt);
                    }
                    if (cdt.offset >= len)
                    {
                        if (errorList != null)
                        {
                            errorList.AddError(Error.MINOR_ERROR, "Diacritic found at the end of field, without the character that it is supposed to decorate");
                            break;
                        }
                    }
                    char c2 = GetChar(data[cdt.offset], cdt.g0, cdt.g1);
                    cdt.offset++;
                    CheckMode(data, cdt);
                    if (c2 != 0) sb.Append(c2);

                    while (!diacritics.Any())
                    {
                        char c1 = diacritics.Dequeue();
                        sb.Append(c1);
                    }
                }
                else if (cdt.multibyte)
                {
                    if (data[cdt.offset] == 0x20)
                    {
                        // if a 0x20 byte occurs amidst a sequence of multibyte characters
                        // skip over it and output a space.
                        sb.Append(GetChar(data[cdt.offset], cdt.g0, cdt.g1));
                        cdt.offset += 1;
                    }
                    else if (cdt.offset + 3 <= data.Length && (errorList == null || data[cdt.offset + 1] != 0x20 && data[cdt.offset + 2] != 0x20))
                    {
                        char c = GetMBChar(MakeMultibyte(data[cdt.offset], data[cdt.offset + 1], data[cdt.offset + 2]));
                        if (errorList == null || c != 0)
                        {
                            sb.Append(c);
                            cdt.offset += 3;
                        }
                        else if (cdt.offset + 6 <= data.Length && data[cdt.offset + 4] != 0x20 && data[cdt.offset + 5] != 0x20 &&
                                GetMBChar(MakeMultibyte(data[cdt.offset + 3], data[cdt.offset + 4], data[cdt.offset + 5])) != 0)
                        {
                            if (errorList != null)
                            {
                                errorList.AddError(Error.MINOR_ERROR, "Erroneous MARC8 multibyte character, Discarding bad character and continuing reading Multibyte characters");
                                sb.Append("[?]");
                                cdt.offset += 3;
                            }
                        }
                        else if (cdt.offset + 4 <= data.Length && data[cdt.offset] > 0x7f &&
                                GetMBChar(MakeMultibyte(data[cdt.offset + 1], data[cdt.offset + 2], data[cdt.offset + 3])) != 0)
                        {
                            if (errorList != null)
                            {
                                errorList.AddError(Error.MINOR_ERROR, "Erroneous character in MARC8 multibyte character, Copying bad character and continuing reading Multibyte characters");
                                sb.Append(GetChar(data[cdt.offset], 0x42, 0x45));
                                cdt.offset += 1;
                            }
                        }
                        else
                        {
                            if (errorList != null)
                            {
                                errorList.AddError(Error.MINOR_ERROR, "Erroneous MARC8 multibyte character, inserting change to default character set");
                            }
                            cdt.multibyte = false;
                            cdt.g0 = 0x42;
                            cdt.g1 = 0x45;
                        }
                    }
                    else if (errorList != null && cdt.offset + 4 <= data.Length && (data[cdt.offset + 1] == 0x20 || data[cdt.offset + 2] == 0x20))
                    {
                        int multiByte = MakeMultibyte(data[cdt.offset], ((data[cdt.offset + 1] != 0x20) ? data[cdt.offset + 1] : data[cdt.offset + 2]), data[cdt.offset + 3]);
                        char c = GetMBChar(multiByte);
                        if (c != 0)
                        {
                            if (errorList != null)
                            {
                                errorList.AddError(Error.ERROR_TYPO, "Extraneous space found within MARC8 multibyte character");
                            }
                            sb.Append(c);
                            sb.Append(' ');
                            cdt.offset += 4;
                        }
                        else
                        {
                            if (errorList != null)
                            {
                                errorList.AddError(Error.MINOR_ERROR, "Erroneous MARC8 multibyte character, inserting change to default character set");
                            }
                            cdt.multibyte = false;
                            cdt.g0 = 0x42;
                            cdt.g1 = 0x45;
                        }
                    }
                    else if (cdt.offset + 3 > data.Length ||
                             cdt.offset + 3 == data.Length && (data[cdt.offset + 1] == 0x20 || data[cdt.offset + 2] == 0x20))
                    {
                        if (errorList != null)
                        {
                            errorList.AddError(Error.MINOR_ERROR, "Partial MARC8 multibyte character, inserting change to default character set");
                            cdt.multibyte = false;
                            cdt.g0 = 0x42;
                            cdt.g1 = 0x45;
                        }
                        // if a field ends with an incomplete encoding of a multibyte character
                        // simply discard that final partial character.
                        else
                        {
                            cdt.offset += 3;
                        }
                    }
                }
                else
                {
                    char c = GetChar(data[cdt.offset], cdt.g0, cdt.g1);
                    if (c != 0) sb.Append(c);
                    else
                    {
                        String val = "0000" + ((int)data[cdt.offset]).ToString("X");
                        sb.Append("<U+" + (val.Substring(val.Length - 4, 4)) + ">");
                    }
                    cdt.offset += 1;
                }
                if (HasNext(cdt.offset, len))
                {
                    CheckMode(data, cdt);
                }
            }
            String dataElement = sb.ToString();
            if (translateNCR && Regex.IsMatch(dataElement, "[^&]*&#x[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f];.*"))
            {
                dataElement = Regex.Replace(dataElement, "&#x([0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]);", a => GetCharFromCodePoint(a.Value));
            }
            return dataElement;
        }

        private string GetCharFromCodePoint(string charCodePoint)
        {
            int charNum = System.Convert.ToInt32(charCodePoint, 16);
            return "" + ((char)charNum);
        }

        //    private int MakeMultibyte(char[] data) {
        //        int[] chars = new int[3];
        //        chars[0] = data[0] << 16;
        //        chars[1] = data[1] << 8;
        //        chars[2] = data[2];
        //        return chars[0] | chars[1] | chars[2];
        //    }

        public int MakeMultibyte(char c1, char c2, char c3)
        {
            int[] chars = new int[3];
            chars[0] = c1 << 16;
            chars[1] = c2 << 8;
            chars[2] = c3;
            return chars[0] | chars[1] | chars[2];
        }

        private char GetChar(int ch, int g0, int g1)
        {
            if (ch <= 0x7E)
                return ct.GetChar(ch, g0);
            else
                return ct.GetChar(ch, g1);
        }

        public char GetMBChar(int ch)
        {
            return ct.GetChar(ch, 0x31);
        }

        private static bool HasNext(int pos, int len)
        {
            if (pos < (len - 1))
                return true;
            return false;
        }

        private static bool IsEscape(int i)
        {
            if (i == 0x1B)
                return true;
            return false;
        }
    }
}