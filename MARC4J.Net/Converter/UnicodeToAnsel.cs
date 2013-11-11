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
    /// <para>
    /// A utility to convert UCS/Unicode data to MARC-8.
    /// </para>
    /// <para>
    /// The MARC-8 to Unicode mapping used is the version with the March 2005
    /// revisions.
    /// </para>
    /// 
    /// </summary>
    public class UnicodeToAnsel : CharConverter
    {
        protected ReverseCodeTable rct;

        static readonly char ESC = (char)0x1b;

        static readonly char G0 = (char)0x28;
        static readonly char G0multibyte = (char)0x24;
        static readonly char G1 = (char)0x29;
        static readonly int ASCII = (char)0x42;

        /// <summary>
        /// Creates a new instance and loads the MARC4J supplied Ansel/Unicode
        /// conversion tables based on the official LC tables. Loads in the generated class
        /// ReverseCodeTableGenerated which contains switch statements to lookup 
        /// the MARC-8 encodings for given Unicode characters.
        /// </summary>
        public UnicodeToAnsel()
        {
            rct = LoadGeneratedTable();
            //this(UnicodeToAnsel.class
            //        .getResourceAsStream("resources/codetables.xml"));
        }


        /// <summary>
        ///  Constructs an instance with the specified pathname.
        /// 
        ///  Use this constructor to create an instance with a customized code table
        ///  mapping. The mapping file should follow the structure of LC's XML MARC-8
        ///  to Unicode mapping (see:
        ///  http://www.loc.gov/marc/specifications/codetables.xml).
        /// </summary>
        /// <param name="pathname"></param>
        public UnicodeToAnsel(String pathname)
        {
            rct = new ReverseCodeTableHash(pathname);
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
        public UnicodeToAnsel(Stream stream)
        {
            rct = new ReverseCodeTableHash(stream);
        }

        private ReverseCodeTable LoadGeneratedTable()
        {
            return ReverseCodeTableHash.DefaultInstance;
        }

        /// <summary>
        /// Converts UCS/Unicode data to MARC-8.
        /// <para>
        /// If there is no match for a Unicode character, it will be encoded as &#xXXXX;
        /// so that if the data is translated back into Unicode, the original data 
        /// can be recreated. 
        /// </para>
        /// </summary>
        /// <param name="data">the UCS/Unicode data in an array of char</param>
        /// <returns>the MARC-8 data</returns>
        public override string Convert(char[] data)
        {
            var sb = new StringBuilder();
            rct.Init();

            ConvertPortion(data, sb);

            if (rct.GetPreviousG0() != ASCII)
            {
                sb.Append(ESC);
                sb.Append(G0);
                sb.Append((char)ASCII);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Does the actual work of converting UCS/Unicode data to MARC-8.
        /// 
        /// <para>
        /// If the Unicode data has been normalized into composed form, and the composed character 
        /// does not have a corresponding MARC8 character, this routine will normalize that character into
        /// its decomposed form, and try to translate that equivalent string into MARC8. 
        /// </para>
        /// </summary>
        /// <param name="data">the UCS/Unicode data in an array of char</param>
        /// <param name="sb">the MARC-8 data</param>
        private void ConvertPortion(char[] data, StringBuilder sb)
        {
            for (int i = 0; i < data.Length; i++)
            {
                var c = data[i];
                var marc = new StringBuilder();
                int charValue = (int)c;
                if (charValue == 0x20 && rct.GetPreviousG0() != (int)'1')
                {
                    if (rct.GetPreviousG0() == (int)'1')
                    {
                        sb.Append(ESC);
                        sb.Append(G0);
                        sb.Append((char)ASCII);
                        rct.SetPreviousG0(ASCII);
                    }
                    marc.Append(" ");
                }
                else if (!rct.CharHasMatch(c))
                {
                    // Unicode character c has no match in the Marc8 tables.  Try unicode-decompose on it
                    // to see whether the decomposed form can be represented.  If when decomposed, all of
                    // the characters can be translated to marc8, then use that.  If not and the decomposed form
                    // if three (or more) characters long (which indicates multiple diacritic marks), then 
                    // re-compose the the main character with the first diacritic, and check whether that 
                    // and the remaining diacritics can be translated. If so go with that, otherwise, give up
                    // and merely use the &#xXXXX; Numeric char Reference form to represent the original
                    // unicode character
                    String tmpnorm = c.ToString();
                    String tmpNormed = tmpnorm.Normalize(NormalizationForm.FormD);
                    if (!tmpNormed.Equals(tmpnorm))
                    {
                        if (AllCharsHaveMatch(rct, tmpNormed))
                        {
                            ConvertPortion(tmpNormed.ToCharArray(), sb);
                            continue;
                        }
                        else if (tmpNormed.Length > 2)
                        {
                            String firstTwo = tmpNormed.Substring(0, 2);
                            String partialNormed = firstTwo.Normalize(NormalizationForm.FormC);
                            if (!partialNormed.Equals(firstTwo) && AllCharsHaveMatch(rct, partialNormed) &&
                                    AllCharsHaveMatch(rct, tmpNormed.Substring(2)))
                            {
                                ConvertPortion((partialNormed + tmpNormed.Substring(2)).ToCharArray(), sb);
                                continue;
                            }
                        }
                    }
                    if (rct.GetPreviousG0() != ASCII)
                    {
                        sb.Append(ESC);
                        sb.Append(G0);
                        sb.Append((char)ASCII);
                        rct.SetPreviousG0(ASCII);
                    }
                    if (charValue < 0x1000)
                        sb.Append("&#x" + (charValue + 0x10000).ToString("X").ToUpper().Substring(1) + ";");
                    else
                        sb.Append("&#x" + charValue.ToString("X").ToUpper() + ";");
                    continue;
                }
                else if (rct.InPreviousG0CharEntry(c))
                {
                    marc.Append(rct.GetCurrentG0CharEntry(c));
                }
                else if (rct.InPreviousG1CharEntry(c))
                {
                    marc.Append(rct.GetCurrentG1CharEntry(c));
                }
                else // need to change character set
                {
                    // if several MARC-8 character sets contain the given Unicode character, select the
                    // best char set to use for encoding the character.  Preference is given to character
                    // sets that have been used previously in the field being encoded.  Since the default
                    // character sets for Basic and extended latin are pre-loaded, usually if a character
                    // can be encoded by one of those character sets, that is what will be chosen.
                    int charset = rct.GetBestCharSet(c);
                    char[] marc8 = rct.GetCharEntry(c, charset);

                    if (marc8.Length == 3)
                    {
                        marc.Append(ESC);
                        marc.Append(G0multibyte);
                        rct.SetPreviousG0(charset);
                    }
                    else if (marc8[0] < 0x80)
                    {
                        marc.Append(ESC);
                        if (charset == 0x62 || charset == 0x70)
                        {
                            //technique1 = true;
                        }
                        else
                        {
                            marc.Append(G0);
                        }
                        rct.SetPreviousG0(charset);
                    }
                    else
                    {
                        marc.Append(ESC);
                        marc.Append(G1);
                        rct.SetPreviousG1(charset);
                    }
                    marc.Append((char)charset);
                    marc.Append(marc8);
                }

                if (rct.IsCombining(c) && sb.Length > 0)
                {
                    sb.Insert(sb.Length - 1, marc);

                    // Special case handling to handle the COMBINING DOUBLE INVERTED BREVE 
                    // and the COMBINING DOUBLE TILDE where a single double wide accent character
                    // in unicode is represented by two half characters in Marc8
                    if (((int)c) == 0x360) sb.Append((char)(0xfb));
                    if (((int)c) == 0x361) sb.Append((char)(0xec));
                }
                else
                {
                    sb.Append(marc);
                }
            }
        }

        private static bool AllCharsHaveMatch(ReverseCodeTable rct, String str)
        {
            foreach (var c in str)
            {
                if (!rct.CharHasMatch(c)) return (false);
            }
            return true;
        }
    }
}