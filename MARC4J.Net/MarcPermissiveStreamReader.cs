using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using MARC4J.Net.Converter;
using MARC4J.Net.MARC;

namespace MARC4J.Net
{
    /// <summary>
    ///  An iterator over a collection of MARC records in ISO 2709 format, that is designed
    ///  to be able to handle MARC records that have errors in their structure or their encoding.
    ///  If the permissive flag is set in the call to the constructor, or if a ErrorHandler object
    ///  is passed in as a parameter to the constructor, this reader will do its best to detect 
    ///  and recover from a number of structural or encoding errors that can occur in a MARC record.
    ///  Note that if this reader is not set to read permissively, its will operate pretty much 
    ///  identically to the MarcStreamReader class.
    /// 
    ///  Note that no attempt is made to validate the contents of the record at a semantic level.
    ///  This reader does not know and does not care whether the record has a 245 field, or if the
    ///  008 field is the right length, but if the record claims to be UTF-8 or MARC8 encoded and 
    ///  you are seeing gibberish in the output, or if the reader is throwing an exception in trying
    ///  to read a record, then this reader may be able to produce a usable record from the bad 
    ///  data you have.
    /// 
    ///  The ability to directly translate the record to UTF-8 as it is being read in is useful in
    ///  cases where the UTF-8 version of the record will be used directly by the program that is
    ///  reading the MARC data, for instance if the marc records are to be indexed into a SOLR search
    ///  engine.  Previously the MARC record could only be translated to UTF-8 as it was being written 
    ///  out via a MarcStreamWriter or a MarcXmlWriter.
    /// 
    ///  <para>
    ///  Example usage:
    /// 
    ///  <code>
    ///  <para>InputStream input = new FileInputStream("file.mrc");                  </para>
    ///  <para>MarcReader reader = new MarcPermissiveStreamReader(input, true, true);</para>
    ///  <para>while (reader.hasNext()) {                                            </para>
    ///  <para>    Record record = reader.next();                                    </para>
    ///  <para>    // Process record
    ///  <para>}</para>
    ///  </code>
    ///  <para></para>
    ///  <para>
    ///  When no encoding is given as an constructor argument the parser tries to
    ///  resolve the encoding by looking at the character coding scheme (leader
    ///  position 9) in MARC21 records. For UNIMARC records this position is not
    ///  defined.   If the reader is operating in permissive mode and no encoding 
    ///  is given as an constructor argument the reader will look at the leader, 
    ///  and also at the data of the record to determine to the best of its ability 
    ///  what character encoding scheme has been used to encode the data in a 
    ///  particular MARC record.
    ///  </para>
    /// 
    /// </summary>
    public class MarcPermissiveStreamReader : IMarcReader
    {
        #region Fields
        private InputStream input = null;

        private IRecord record;

        private readonly MarcFactory factory;

        private String encoding = "ISO-8859-1";

        /// <summary>
        /// This represents the expected encoding of the data when a 
        /// MARC record does not have a 'a' in character 9 of the leader. 
        /// </summary>
        private String defaultEncoding = "ISO-8859-1";

        private bool convertToUTF8 = false;

        private bool permissive = false;

        private int marc_file_lookahead_buffer = 200000;

        private CharConverter converterAnsel = null;

        private CharConverter converterUnimarc = null;

        // These are used to algorithmically determine what encoding scheme was 
        // used to encode the data in the Marc record
        private String conversionCheck1 = null;
        private String conversionCheck2 = null;
        private String conversionCheck3 = null;

        private ErrorHandler errors;
        static String validSubfieldCodes = "abcdefghijklmnopqrstuvwxyz0123456789";

        #endregion

        #region Ctors
        /// <summary>
        /// Constructs an instance with the specified input stream with possible additional functionality
        /// being enabled by setting permissive and/or convertToUTF8 to true.
        /// <para></para>
        /// If permissive and convertToUTF8 are both set to false, it functions almost identically to the
        /// MarcStreamReader class.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="permissive"></param>
        /// <param name="convertToUTF8"></param>
        public MarcPermissiveStreamReader(Stream input, bool permissive, bool convertToUTF8)
        {
            this.permissive = permissive;
            this.input = new InputStream(input);
            factory = MarcFactory.Instance;
            this.convertToUTF8 = convertToUTF8;
            errors = null;
            if (permissive)
            {
                errors = new ErrorHandler();
                defaultEncoding = "BESTGUESS";
            }
        }

        /// <summary>
        /// Constructs an instance with the specified input stream with possible additional functionality
        /// being enabled by passing in an ErrorHandler object and/or setting convertToUTF8 to true.
        /// <para></para>
        /// If errors and convertToUTF8 are both set to false, it functions almost identically to the
        /// MarcStreamReader class.
        /// <para></para>
        /// If an ErrorHandler object is passed in, that object will be used to log and track any errors 
        /// in the records as the records are decoded.  After the next() function returns, you can query 
        /// to determine whether any errors were detected in the decoding process.
        /// 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="errors"></param>
        /// <param name="convertToUTF8"></param>
        public MarcPermissiveStreamReader(Stream input, ErrorHandler errors, bool convertToUTF8)
        {
            if (errors != null)
            {
                permissive = true;
                defaultEncoding = "BESTGUESS";
            }
            this.input = new InputStream(input); ;
            factory = MarcFactory.Instance;
            this.convertToUTF8 = convertToUTF8;
            this.errors = errors;
        }

        /// <summary>
        /// Constructs an instance with the specified input stream with possible additional functionality
        /// being enabled by setting permissive and/or convertToUTF8 to true.
        /// <para></para>
        /// If permissive and convertToUTF8 are both set to false, it functions almost identically to the
        /// MarcStreamReader class.
        /// <para></para>
        /// The parameter defaultEncoding is used to specify the character encoding that is used in the records
        /// that will be read from the input stream.   If permissive is set to true, you can specify "BESTGUESS"
        /// as the default encoding, and the reader will attempt to determine the character encoding used in the 
        /// records being read from the input stream.   This is especially useful if you are working with records 
        /// downloaded from an external source and the encoding is either unknown or the encoding is different from
        /// what the records claim to be.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="permissive"></param>
        /// <param name="convertToUTF8"></param>
        /// <param name="defaultEncoding"></param>
        public MarcPermissiveStreamReader(Stream input, bool permissive, bool convertToUTF8, String defaultEncoding)
        {
            this.permissive = permissive;
            this.input = new InputStream(input); ;
            factory = MarcFactory.Instance;
            this.convertToUTF8 = convertToUTF8;
            this.defaultEncoding = defaultEncoding;
            errors = null;
            if (permissive) errors = new ErrorHandler();
        }

        /// <summary>
        ///  Constructs an instance with the specified input stream with possible additional functionality
        ///  being enabled by setting permissive and/or convertToUTF8 to true.
        /// <para></para>
        ///  If errors and convertToUTF8 are both set to false, it functions almost identically to the
        ///  MarcStreamReader class.
        /// <para></para>
        ///  The parameter defaultEncoding is used to specify the character encoding that is used in the records
        ///  that will be read from the input stream.   If permissive is set to true, you can specify "BESTGUESS"
        ///  as the default encoding, and the reader will attempt to determine the character encoding used in the 
        ///  records being read from the input stream.   This is especially useful if you are working with records 
        ///  downloaded from an external source and the encoding is either unknown or the encoding is different from
        ///  what the records claim to be.
        /// <para></para>
        ///  If an ErrorHandler object is passed in, that object will be used to log and track any errors 
        ///  in the records as the records are decoded.  After the next() function returns, you can query 
        ///  to determine whether any errors were detected in the decoding process.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="errors"></param>
        /// <param name="convertToUTF8"></param>
        /// <param name="defaultEncoding"></param>

        public MarcPermissiveStreamReader(Stream input, ErrorHandler errors, bool convertToUTF8, String defaultEncoding)
        {
            this.permissive = true;
            this.input = new InputStream(input); ;
            factory = MarcFactory.Instance;
            this.convertToUTF8 = convertToUTF8;
            this.defaultEncoding = defaultEncoding;
            this.errors = errors;
        }

        #endregion
        /// <summary>
        /// Returns true if the iteration has more records, false otherwise.
        /// </summary>
        /// <returns></returns>
        private bool HasNext()
        {
            try
            {
                input.Mark();
                int byteread = input.ReadByte();
                if (byteread == -1)
                    return false;
                //    byte[] recLengthBuf = new byte[5];
                int numBadBytes = 0;
                while (byteread < '0' || byteread > '9')
                {
                    byteread = input.ReadByte();
                    numBadBytes++;
                    if (byteread == -1) return false;
                }

                input.Reset();
                while (numBadBytes > 0)
                {
                    byteread = input.ReadByte();
                    numBadBytes--;
                }
            }
            catch (IOException e)
            {
                throw new MarcException(e.Message, e);
            }
            return true;
        }

        /**
         * Returns the next record in the iteration.
         * 
         * @return Record - the record object
         */
        private IRecord Next()
        {
            record = factory.NewRecord();
            if (errors != null) errors.Reset();

            try
            {
                byte[] byteArray = new byte[24];

                input.Read(byteArray);
                int recordLength = RarseRecordLength(byteArray);
                byte[] recordBuf = new byte[recordLength - 24];
                if (permissive)
                {
                    input.Mark(marc_file_lookahead_buffer);
                    input.Read(recordBuf);
                    if (recordBuf[recordBuf.Length - 1] != Constants.RT)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "Record terminator character not found at end of record length");
                        recordBuf = ReReadPermissively(input, recordBuf, recordLength);
                        recordLength = recordBuf.Length + 24;
                    }
                }
                else
                {
                    input.Read(recordBuf);
                }
                var tmp = recordBuf.ConvertToString();
                RarseRecord(record, byteArray, recordBuf, recordLength);

                if (this.convertToUTF8)
                {
                    var l = record.Leader;
                    l.CharCodingScheme = 'a';
                    record.Leader = l;
                }
                return record;
            }
            catch (EndOfStreamException e)
            {
                throw new MarcException("Premature end of file encountered", e);
            }
            catch (IOException e)
            {
                throw new MarcException("an error occured reading input", e);
            }
        }

        private byte[] ReReadPermissively(InputStream input, byte[] recordBuf, int recordLength)
        {
            int loc = ArrayContainsAt(recordBuf, Constants.RT);
            if (loc != -1)  // stated record length is too long
            {
                errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                "Record terminator appears before stated record length, using shorter record");
                recordLength = loc + 24;
                input.Reset();
                recordBuf = new byte[recordLength - 24];
                input.Read(recordBuf);
            }
            else  // stated record length is too short read ahead
            {
                loc = recordLength - 24;
                bool done = false;
                while (!done)
                {
                    int c = 0;
                    do
                    {
                        c = input.ReadByte();
                        loc++;
                    } while (loc < (marc_file_lookahead_buffer - 24) && c != Constants.RT && c != -1);

                    if (c == Constants.RT)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "Record terminator appears after stated record length, reading extra bytes");
                        recordLength = loc + 24;
                        input.Reset();
                        recordBuf = new byte[recordLength - 24];
                        input.Read(recordBuf);
                        done = true;
                    }
                    else if (c == -1)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "No Record terminator found, end of file reached, Terminator appended");
                        recordLength = loc + 24;
                        input.Reset();
                        recordBuf = new byte[recordLength - 24 + 1];
                        input.Read(recordBuf);
                        recordBuf[recordBuf.Length - 1] = Constants.RT;
                        done = true;
                    }
                    else
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                        "No Record terminator found within " + marc_file_lookahead_buffer + " bytes of start of record, getting desperate.");
                        input.Reset();
                        marc_file_lookahead_buffer *= 2;
                        input.Mark(marc_file_lookahead_buffer);
                        loc = 0;
                    }
                }
            }
            return (recordBuf);
        }

        private void RarseRecord(IRecord record, byte[] byteArray, byte[] recordBuf, int recordLength)
        {
            ILeader ldr = factory.NewLeader();
            ldr.RecordLength = recordLength;
            int directoryLength = 0;
            // These variables are used when the permissive reader is trying to make its best guess 
            // as to what character encoding is actually used in the record being processed.
            conversionCheck1 = "";
            conversionCheck2 = "";
            conversionCheck3 = "";

            try
            {
                ParseLeader(ldr, byteArray);
                directoryLength = ldr.BaseAddressOfData - (24 + 1);
            }
            catch (IOException e)
            {
                throw new MarcException("error parsing leader with data: "
                        + byteArray.ConvertToString(), e);
            }
            catch (MarcException e)
            {
                if (permissive)
                {
                    if (recordBuf[recordBuf.Length - 1] == Constants.RT && recordBuf[recordBuf.Length - 2] == Constants.FT)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "Error parsing leader, trying to re-read leader either shorter or longer");
                        // make an attempt to recover record.
                        int offset = 0;
                        while (offset < recordBuf.Length)
                        {
                            if (recordBuf[offset] == Constants.FT)
                            {
                                break;
                            }
                            offset++;
                        }
                        if (offset % 12 == 1)
                        {
                            // move one byte from body to leader, make new leader, and try again
                            errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                            "Leader appears to be too short, moving one byte from record body to leader, and trying again");
                            byte[] oldBody = recordBuf;
                            recordBuf = new byte[oldBody.Length - 1];
                            Array.Copy(oldBody, 1, recordBuf, 0, oldBody.Length - 1);
                            directoryLength = offset - 1;
                            ldr.IndicatorCount = 2;
                            ldr.SubfieldCodeLength = 2;
                            ldr.ImplDefined1 = ("" + (char)byteArray[7] + " ").ToCharArray();
                            ldr.ImplDefined2 = ("" + (char)byteArray[18] + (char)byteArray[19] + (char)byteArray[20]).ToCharArray();
                            ldr.EntryMap = "4500".ToCharArray();
                            if (byteArray[10] == (byte)' ' || byteArray[10] == (byte)'a') // if its ' ' or 'a'
                            {
                                ldr.CharCodingScheme = (char)byteArray[10];
                            }
                        }
                        else if (offset % 12 == 11)
                        {
                            errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                            "Leader appears to be too long, moving one byte from leader to record body, and trying again");
                            byte[] oldBody = recordBuf;
                            recordBuf = new byte[oldBody.Length + 1];
                            Array.Copy(oldBody, 0, recordBuf, 1, oldBody.Length);
                            recordBuf[0] = (byte)'0';
                            directoryLength = offset + 1;
                            ldr.IndicatorCount = 2;
                            ldr.SubfieldCodeLength = 2;
                            ldr.ImplDefined1 = ("" + (char)byteArray[7] + " ").ToCharArray();
                            ldr.ImplDefined2 = ("" + (char)byteArray[16] + (char)byteArray[17] + (char)byteArray[18]).ToCharArray();
                            ldr.EntryMap = "4500".ToCharArray();
                            if (byteArray[8] == (byte)' ' || byteArray[8] == (byte)'a') // if its ' ' or 'a'
                            {
                                ldr.CharCodingScheme = (char)byteArray[10];
                            }
                            if (byteArray[10] == (byte)' ' || byteArray[10] == (byte)'a') // if its ' ' or 'a'
                            {
                                ldr.CharCodingScheme = (char)byteArray[10];
                            }
                        }
                        else
                        {
                            errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                           "error parsing leader with data: " + byteArray.ConvertToString());
                            throw new MarcException("error parsing leader with data: "
                                    + byteArray.ConvertToString(), e);
                        }
                    }
                }
                else
                {
                    throw new MarcException("error parsing leader with data: "
                            + byteArray.ConvertToString(), e);
                }
            }
            char[] tmp = ldr.EntryMap;
            if (permissive && !("" + tmp[0] + tmp[1] + tmp[2] + tmp[3]).Equals("4500"))
            {
                if (tmp[0] >= '0' && tmp[0] <= '9' &&
                        tmp[1] >= '0' && tmp[1] <= '9' &&
                        tmp[2] >= '0' && tmp[2] <= '9' &&
                        tmp[3] >= '0' && tmp[3] <= '9')
                {
                    errors.AddError("unknown", "n/a", "n/a", Error.ERROR_TYPO,
                                "Unusual character found at end of leader [ " + tmp[0] + tmp[1] + tmp[2] + tmp[3] + " ]");
                }
                else
                {
                    errors.AddError("unknown", "n/a", "n/a", Error.ERROR_TYPO,
                                    "Erroneous character found at end of leader [ " + tmp[0] + tmp[1] + tmp[2] + tmp[3] + " ]; changing them to the standard \"4500\"");
                    ldr.EntryMap = "4500".ToCharArray();
                }
            }

            // if MARC 21 then check encoding
            switch (ldr.CharCodingScheme)
            {
                case 'a':
                    encoding = "UTF-8";
                    break;
                case ' ':
                    if (convertToUTF8)
                        encoding = defaultEncoding;
                    else
                        encoding = "ISO-8859-1";
                    break;
                default:
                    if (convertToUTF8)
                        if (permissive)
                        {
                            errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                            "Record character encoding should be 'a' or ' ' in this record it is '" + ldr.CharCodingScheme + "'. Attempting to guess the correct encoding.");
                            encoding = "BESTGUESS";
                        }
                        else
                            encoding = defaultEncoding;
                    else
                        encoding = "ISO-8859-1";
                    break;

            }
            String utfCheck;
            if (encoding.Equals("BESTGUESS", StringComparison.CurrentCultureIgnoreCase))
            {
                try
                {
                    String marc8EscSeqCheck = Encoding.GetEncoding("ISO-8859-1").GetString(recordBuf);
                    //  If record has MARC8 character set selection strings, it must be MARC8 encoded
                    if (Regex.Split(marc8EscSeqCheck, "\\e[-(,)$bsp]").Length > 1)
                    {
                        encoding = "MARC8";
                    }
                    else
                    {
                        bool hasHighBitChars = false;
                        for (int i = 0; i < recordBuf.Length; i++)
                        {
                            if (recordBuf[i] < 0) // the high bit is set
                            {
                                hasHighBitChars = true;
                                break;
                            }
                        }
                        if (!hasHighBitChars)
                        {
                            encoding = "ISO-8859-1";  //  You can choose any encoding you want here, the results will be the same.
                        }
                        else
                        {
                            utfCheck = Encoding.UTF8.GetString(recordBuf);
                            byte[] byteCheck = Encoding.UTF8.GetBytes(utfCheck);
                            encoding = "UTF-8";
                            if (recordBuf.Length == byteCheck.Length)
                            {
                                for (int i = 0; i < recordBuf.Length; i++)
                                {
                                    if (byteCheck[i] != recordBuf[i])
                                    {
                                        encoding = "MARC8-Maybe";
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                encoding = "MARC8-Maybe";
                            }
                        }
                    }
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
            }
            else if (permissive && encoding.Equals("UTF-8"))
            {
                try
                {
                    utfCheck = Encoding.UTF8.GetString(recordBuf);
                    byte[] byteCheck = Encoding.UTF8.GetBytes(utfCheck);
                    if (recordBuf.Length != byteCheck.Length)
                    {
                        bool foundESC = false;
                        for (int i = 0; i < recordBuf.Length; i++)
                        {
                            if (recordBuf[i] == 0x1B)
                            {
                                errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                                "Record claims to be UTF-8, but its not. Its probably MARC8.");
                                encoding = "MARC8-Maybe";
                                foundESC = true;
                                break;
                            }
                            if (byteCheck[i] != recordBuf[i])
                            {
                                encoding = "MARC8-Maybe";
                            }

                        }
                        if (!foundESC)
                        {
                            errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                    "Record claims to be UTF-8, but its not. It may be MARC8, or maybe UNIMARC, or maybe raw ISO-8859-1 ");
                        }
                    }
                    if (utfCheck.Contains("a$1!"))
                    {
                        encoding = "MARC8-Broken";
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                    "Record claims to be UTF-8, but its not. It seems to be MARC8-encoded but with missing escape codes.");
                    }
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
            }
            else if (permissive && !encoding.Equals("UTF-8") && convertToUTF8)
            {
                try
                {
                    utfCheck = Encoding.UTF8.GetString(recordBuf);
                    byte[] byteCheck = Encoding.UTF8.GetBytes(utfCheck);
                    if (recordBuf.Length == byteCheck.Length)
                    {
                        for (int i = 0; i < recordBuf.Length; i++)
                        {
                            // need to check for byte < 0 to see if the high bit is set, because Java doesn't have unsigned types.
                            if (recordBuf[i] < 0x00 || byteCheck[i] != recordBuf[i])
                            {
                                errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                            "Record claims not to be UTF-8, but it seems to be.");
                                encoding = "UTF8-Maybe";
                                break;
                            }
                        }
                    }
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
            }
            record.Leader = ldr;

            bool discardOneAtStartOfDirectory = false;
            bool discardOneSomewhereInDirectory = false;

            if ((directoryLength % 12) != 0)
            {
                if (permissive && directoryLength == 99974 && recordLength > 200000) //  which Equals 99999 - (24 + 1) its a BIG record (its directory is over 100000 bytes)
                {
                    directoryLength = 0;
                    int tmpLength = 0;
                    for (tmpLength = 0; tmpLength < recordLength; tmpLength += 12)
                    {
                        if (recordBuf[tmpLength] == Constants.FT)
                        {
                            directoryLength = tmpLength;
                            break;
                        }
                    }
                    if (directoryLength == 0)
                    {
                        throw new MarcException("Directory is too big (> 99999 bytes) and it doesn't end with a field terminator character, I give up. Unable to continue.");
                    }
                }
                else if (permissive && directoryLength % 12 == 11 && recordBuf[1] != (byte)'0')
                {
                    errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                    "Directory length is not a multiple of 12 bytes long.  Prepending a zero and trying to continue.");
                    byte[] oldBody = recordBuf;
                    recordBuf = new byte[oldBody.Length + 1];
                    Array.Copy(oldBody, 0, recordBuf, 1, oldBody.Length);
                    recordBuf[0] = (byte)'0';
                    directoryLength = directoryLength + 1;
                }
                else
                {
                    if (permissive && directoryLength % 12 == 1 && recordBuf[1] == (byte)'0' && recordBuf[2] == (byte)'0')
                    {
                        discardOneAtStartOfDirectory = true;
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "Directory length is not a multiple of 12 bytes long. Discarding byte from start of directory and trying to continue.");
                    }
                    else if (permissive && directoryLength % 12 == 1 && recordLength > 10000 && recordBuf[0] == (byte)'0' &&
                             recordBuf[1] == (byte)'0' && recordBuf[2] > (byte)'0' && recordBuf[2] <= (byte)'9')
                    {
                        discardOneSomewhereInDirectory = true;
                        errors.AddError("unknown", "n/a", "n/a", Error.MAJOR_ERROR,
                                        "Directory length is not a multiple of 12 bytes long.  Will look for oversized field and try to work around it.");
                    }
                    else
                    {
                        if (errors != null)
                        {
                            errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                    "Directory length is not a multiple of 12 bytes long. Unable to continue.");
                        }
                        throw new MarcException("Directory length is not a multiple of 12 bytes long. Unable to continue.");
                    }
                }
            }
            using (var inputrec = new InputStream(recordBuf))
            {
                int size = directoryLength / 12;

                String[] tags = new String[size];
                int[] lengths = new int[size];

                byte[] tag = new byte[3];
                byte[] length = new byte[4];
                byte[] start = new byte[5];

                String tmpStr;
                try
                {
                    if (discardOneAtStartOfDirectory) inputrec.ReadByte();
                    int totalOffset = 0;
                    for (int i = 0; i < size; i++)
                    {
                        inputrec.Read(tag);
                        tmpStr = tag.ConvertToString();
                        tags[i] = tmpStr;

                        bool proceedNormally = true;
                        if (discardOneSomewhereInDirectory)
                        {
                            byte[] lenCheck = new byte[10];
                            inputrec.Mark(20);
                            inputrec.Read(lenCheck);
                            if (ByteCompare(lenCheck, 4, 5, totalOffset)) // proceed normally
                            {
                                proceedNormally = true;
                            }
                            else if (ByteCompare(lenCheck, 5, 5, totalOffset)) // field length is 5 bytes!  Bad Marc record, proceed normally
                            {
                                discardOneSomewhereInDirectory = false;
                                errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                                "Field is longer than 9999 bytes.  Writing this record out will result in a bad record.");
                                proceedNormally = false;
                            }
                            else
                            {
                                errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                                "Unable to reconcile problems in directory. Unable to continue.");
                                throw new MarcException("Directory length is not a multiple of 12 bytes long. Unable to continue.");
                            }
                            inputrec.Reset();
                        }
                        if (proceedNormally)
                        {
                            inputrec.Read(length);
                            tmpStr = length.ConvertToString();
                            lengths[i] = int.Parse(tmpStr);

                            inputrec.Read(start);
                        }
                        else // length is 5 bytes long 
                        {
                            inputrec.Read(start);
                            tmpStr = start.ConvertToString();
                            lengths[i] = int.Parse(tmpStr);

                            inputrec.Read(start);
                        }
                        totalOffset += lengths[i];
                    }

                    // If we still haven't found the extra byte, throw out the last byte and try to continue;
                    if (discardOneSomewhereInDirectory) inputrec.ReadByte();

                    if (inputrec.ReadByte() != Constants.FT)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                        "Expected field terminator at end of directory. Unable to continue.");
                        throw new MarcException("expected field terminator at end of directory");
                    }

                    int numBadLengths = 0;

                    int totalLength = 0;
                    for (int i = 0; i < size; i++)
                    {
                        int fieldLength = GetFieldLength(inputrec);
                        if (fieldLength + 1 != lengths[i] && permissive)
                        {
                            if (numBadLengths < 5 && (totalLength + fieldLength < recordLength + 26))
                            {
                                inputrec.Mark(9999);
                                byteArray = new byte[lengths[i]];
                                inputrec.Read(byteArray);
                                inputrec.Reset();
                                if (fieldLength + 1 < lengths[i] && byteArray[lengths[i] - 1] == Constants.FT)
                                {
                                    errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                                    "Field Terminator character found in the middle of a field.");
                                }
                                else
                                {
                                    numBadLengths++;
                                    lengths[i] = fieldLength + 1;
                                    errors.AddError("unknown", "n/a", "n/a", Error.MINOR_ERROR,
                                                    "Field length found in record different from length stated in the directory.");
                                    if (fieldLength + 1 > 9999)
                                    {
                                        errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                                    "Field length is greater than 9999, record cannot be represented as a binary Marc record.");
                                    }
                                }

                            }
                        }
                        totalLength += lengths[i];
                        if (IsControlField(tags[i]))
                        {
                            byteArray = new byte[lengths[i] - 1];

                            inputrec.Read(byteArray);

                            if (inputrec.ReadByte() != Constants.FT)
                            {
                                errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                                "Expected field terminator at end of field. Unable to continue.");
                                throw new MarcException("expected field terminator at end of field");
                            }

                            var field = factory.NewControlField();
                            field.Tag = tags[i];
                            field.Data = GetDataAsString(byteArray);
                            record.AddVariableField(field);

                        }
                        else
                        {
                            byteArray = new byte[lengths[i]];
                            inputrec.Read(byteArray);
                            try
                            {
                                record.AddVariableField(ParseDataField(tags[i], byteArray));
                            }
                            catch (IOException e)
                            {
                                throw new MarcException(
                                        "error parsing data field for tag: " + tags[i]
                                                + " with data: "
                                                + byteArray.ConvertToString(), e);
                            }
                        }
                    }

                    // We've determined that although the record says it is UTF-8, it is not. 
                    // Here we make an attempt to determine the actual encoding of the data in the record.
                    if (permissive && conversionCheck1.Length > 1 &&
                            conversionCheck2.Length > 1 && conversionCheck3.Length > 1)
                    {
                        GuessAndSelectCorrectNonUTF8Encoding();
                    }
                    if (inputrec.ReadByte() != Constants.RT)
                    {
                        errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                        "Expected record terminator at end of record. Unable to continue.");
                        throw new MarcException("expected record terminator");
                    }

                }
                catch (IOException e)
                {
                    errors.AddError("unknown", "n/a", "n/a", Error.FATAL,
                                    "Error reading from data file. Unable to continue.");
                    throw new MarcException("an error occured reading input", e);
                }
            }
        }

        private bool ByteCompare(byte[] lenCheck, int offset, int length, int totalOffset)
        {
            int divisor = 1;
            for (int i = offset + length - 1; i >= offset; i--, divisor *= 10)
            {
                if (((totalOffset / divisor) % 10) + '0' != lenCheck[i])
                {
                    return (false);
                }
            }
            return true;
        }

        private bool IsControlField(String tag)
        {
            bool isControl = false;
            try
            {
                isControl = Verifier.IsControlField(tag);
            }
            catch (FormatException nfe)
            {
                if (permissive)
                {
                    errors.AddError(record.GetControlNumber(), tag, "n/a", Error.ERROR_TYPO,
                                    "Field tag contains non-numeric characters (" + tag + ").");
                    isControl = false;
                }
            }
            return isControl;
        }

        private void GuessAndSelectCorrectNonUTF8Encoding()
        {
            int defaultPart = 0;
            if (record.GetVariableField("245") == null) defaultPart = 1;
            int partToUse = 0;
            int l1 = conversionCheck1.Length;
            int l2 = conversionCheck2.Length;
            int l3 = conversionCheck3.Length;
            int tst;

            if (l1 < l3 && l2 == l3 && defaultPart == 0)
            {
                errors.AddError(Error.INFO, "MARC8 translation shorter than ISO-8859-1, choosing MARC8.");
                partToUse = 0;
            }
            else if (l2 < l1 - 2 && l2 < l3 - 2)
            {
                errors.AddError(Error.INFO, "Unimarc translation shortest, choosing it.");
                partToUse = 1;
            }
            else if ((tst = OnlyOneStartsWithUpperCase(conversionCheck1, conversionCheck2, conversionCheck3)) != -1)
            {
                partToUse = tst;
            }
            else if (l2 < l1 && l2 < l3)
            {
                errors.AddError(Error.INFO, "Unimarc translation shortest, choosing it.");
                partToUse = 1;
            }
            else if (conversionCheck2.Equals(conversionCheck3) && !conversionCheck1.Trim().Contains(" "))
            {
                errors.AddError(Error.INFO, "Unimarc and ISO-8859-1 translations identical, choosing ISO-8859-1.");
                partToUse = 2;
            }
            else if (!SpecialCharIsBetweenLetters(conversionCheck1))
            {
                errors.AddError(Error.INFO, "To few letters in translations, choosing " + (defaultPart == 0 ? "MARC8" : "Unimarc"));
                partToUse = defaultPart;
            }
            //        else if (l2 == l1 && l2 == l3)
            //        {
            //            errors.AddError(Error.INFO, "All three version equal length. Choosing ISO-8859-1 ");
            //            partToUse = 2;
            //        }
            else if (l2 == l3 && defaultPart == 1)
            {
                errors.AddError(Error.INFO, "Unimarc and ISO-8859-1 translations equal length, choosing ISO-8859-1.");
                partToUse = 2;
            }
            else
            {
                errors.AddError(Error.INFO, "No Determination made, defaulting to " + (defaultPart == 0 ? "MARC8" : "Unimarc"));
                partToUse = defaultPart;
            }
            var fields = record.GetVariableFields();
            using (var iter = fields.GetEnumerator())
            {
                while (iter.MoveNext())
                {
                    IVariableField field = iter.Current;
                    if (field is IDataField)
                    {
                        var df = (IDataField)field;
                        var subf = df.GetSubfields();
                        using (
                        var sfiter = subf.GetEnumerator())
                        {
                            while (sfiter.MoveNext())
                            {
                                var sf = sfiter.Current;
                                if (sf.Data.Contains("%%@%%"))
                                {
                                    var parts = sf.Data.Split(new string[] { "%%@%%" }, 3, StringSplitOptions.None);
                                    sf.Data = parts[partToUse];
                                }
                            }
                        }
                    }
                }
            }
        }

        private int OnlyOneStartsWithUpperCase(String conversionCheck12, String conversionCheck22, String conversionCheck32)
        {
            if (conversionCheck1.Length == 0 || conversionCheck2.Length == 0 || conversionCheck3.Length == 0) return -1;
            String[] check1Parts = conversionCheck1.Trim().Split(new string[] { "[|]>" }, StringSplitOptions.None);
            String[] check2Parts = conversionCheck2.Trim().Split(new string[] { "[|]>" }, StringSplitOptions.None);
            String[] check3Parts = conversionCheck3.Trim().Split(new string[] { "[|]>" }, StringSplitOptions.None);
            for (int i = 1; i < check1Parts.Length && i < check2Parts.Length && i < check3Parts.Length; i++)
            {
                bool tst1 = char.IsUpper(check1Parts[i][0]);
                bool tst2 = char.IsUpper(check2Parts[i][0]);
                bool tst3 = char.IsUpper(check3Parts[i][0]);
                if (tst1 && !tst2 && !tst3)
                    return (0);
                if (!tst1 && tst2 && !tst3)
                    return (-1);
                if (!tst1 && !tst2 && tst3)
                    return (2);
            }
            return -1;
        }

        private bool SpecialCharIsBetweenLetters(String conversionCheck)
        {
            bool bewteenLetters = true;
            for (int i = 0; i < conversionCheck.Length; i++)
            {
                int charCode = (int)(conversionCheck[i]);
                if (charCode > 0x7f)
                {
                    bewteenLetters = false;
                    if (i > 0 && char.IsLetter(conversionCheck[i - 1]) ||
                       (i < conversionCheck.Length - 1 && char.IsLetter(conversionCheck[i + 1])))
                    {
                        bewteenLetters = true;
                        break;
                    }
                }
            }
            return (bewteenLetters);
        }

        private int ArrayContainsAt(byte[] byteArray, int ft)
        {
            for (int i = 0; i < byteArray.Length; i++)
            {
                if (byteArray[i] == (byte)ft) return (i);
            }
            return (-1);
        }

        private IDataField ParseDataField(String tag, byte[] field)
        {
            if (permissive)
            {
                errors.CurRecordID = record.GetControlNumber();
                if (tag.Equals("880"))
                {
                    String fieldTag = field.ConvertToString();
                    fieldTag = fieldTag.ReplaceFirst("^.*\\x1F6", "").ReplaceFirst("([-0-9]*).*", "$1");
                    errors.CurField = tag + "(" + fieldTag + ")";
                }
                else
                    errors.CurField = tag;
                errors.CurSubfield = "n/a";
                CleanupBadFieldSeperators(field);
            }
            using (var bais = new InputStream(field))
            {
                char ind1 = (char)bais.ReadByte();
                char ind2 = (char)bais.ReadByte();

                var dataField = factory.NewDataField();
                dataField.Tag = tag;
                dataField.Indicator1 = ind1;
                dataField.Indicator2 = ind2;

                int code;
                int size;
                int readByte;
                byte[] data;
                ISubfield subfield;
                while (true)
                {
                    readByte = bais.ReadByte();
                    if (readByte < 0)
                        break;
                    switch (readByte)
                    {
                        case Constants.US:
                            code = bais.ReadByte();
                            if (code < 0)
                                throw new IOException("unexpected end of data field");
                            if (code == Constants.FT)
                                break;
                            size = GetSubfieldLength(bais);
                            if (size == 0)
                            {
                                if (permissive)
                                {
                                    errors.AddError(Error.MINOR_ERROR, "Subfield of zero length encountered, ignoring it.");
                                    continue;
                                }
                                throw new IOException("Subfield of zero length encountered");
                            }
                            data = new byte[size];
                            bais.Read(data);
                            subfield = factory.NewSubfield();
                            if (permissive) errors.CurField = "" + (char)code;
                            String dataAsString = GetDataAsString(data);
                            if (permissive && code == Constants.US)
                            {
                                code = data[0];
                                dataAsString = dataAsString.Substring(1);
                                errors.AddError(Error.MAJOR_ERROR,
                                                "Subfield tag is a subfield separator, using first character of field as subfield tag.");
                            }
                            else if (permissive && validSubfieldCodes.IndexOf((char)code) == -1)
                            {
                                if (code >= 'A' && code <= 'Z')
                                {
                                    if (!GlobalSettings.UpperCaseSubfields)
                                    {
                                        code = char.ToLower((char)code);
                                        errors.AddError(Error.MINOR_ERROR,
                                                    "Subfield tag is an invalid uppercase character, changing it to lower case.");
                                    }
                                    else // the System Property org.marc4j.MarcPermissiveStreamReader.upperCaseSubfields is defined to allow upperCaseSubfields
                                    {
                                        // therefore do nothing and be happy
                                    }
                                }
                                else if (code > 0x7f)
                                {
                                    code = data[0];
                                    dataAsString = dataAsString.Substring(1);
                                    errors.AddError(Error.MAJOR_ERROR,
                                                    "Subfield tag is an invalid character greater than 0x7f, using first character of field as subfield tag.");
                                }
                                else if (code == '[' && tag.Equals("245"))
                                {
                                    code = 'h';
                                    dataAsString = '[' + dataAsString;
                                    errors.AddError(Error.MAJOR_ERROR,
                                                    "Subfield tag is an open bracket, generating a code 'h' and pushing the bracket to the data.");
                                }
                                else if (code == ' ')
                                {
                                    errors.AddError(Error.MAJOR_ERROR,
                                                    "Subfield tag is a space which is an invalid character");
                                }
                                else
                                {
                                    errors.AddError(Error.MAJOR_ERROR,
                                                    "Subfield tag is an invalid character, [ " + ((char)code) + " ]");
                                }
                            }
                            subfield.Code = (char)code;
                            subfield.Data = dataAsString;
                            dataField.AddSubfield(subfield);
                            break;
                        case Constants.FT:
                            break;
                    }
                }

                return dataField;
            }
        }

        static AnselToUnicode conv = null;

        private void CleanupBadFieldSeperators(byte[] field)
        {
            if (conv == null) conv = new AnselToUnicode(true);
            bool hasEsc = false;
            bool inMultiByte = false;
            bool justCleaned = false;
            int mbOffset = 0;

            for (int i = 0; i < field.Length - 1; i++)
            {
                if (field[i] == 0x1B)
                {
                    hasEsc = true;
                    if ("(,)-'".IndexOf((char)field[i + 1]) != -1)
                    {
                        inMultiByte = false;
                    }
                    else if (i + 2 < field.Length && field[i + 1] == '$' && field[i + 2] == '1')
                    {
                        inMultiByte = true;
                        mbOffset = 3;
                    }
                    else if (i + 3 < field.Length && (field[i + 1] == '$' || field[i + 2] == '$') && (field[i + 2] == '1' || field[i + 3] == '1'))
                    {
                        inMultiByte = true;
                        mbOffset = 4;
                    }

                }
                else if (inMultiByte && field[i] != 0x20) mbOffset = (mbOffset == 0) ? 2 : mbOffset - 1;
                if (inMultiByte && mbOffset == 0 && i + 2 < field.Length)
                {
                    char c;
                    byte f1 = field[i];
                    byte f2 = field[i + 1] == 0x20 ? field[i + 2] : field[i + 1];
                    byte f3 = (field[i + 1] == 0x20 || field[i + 2] == 0x20) ? field[i + 3] : field[i + 2];
                    c = conv.GetMBChar(conv.MakeMultibyte((char)((f1 == Constants.US) ? 0x7C : f1),
                                                          (char)((f2 == Constants.US) ? 0x7C : f2),
                                                          (char)((f3 == Constants.US) ? 0x7C : f3)));
                    if (c == 0 && !justCleaned)
                    {
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Bad Multibyte character found, reinterpreting data as non-multibyte data");
                        inMultiByte = false;
                    }
                    else if (c == 0 && justCleaned)
                    {
                        c = conv.GetMBChar(conv.MakeMultibyte('!', (char)((f2 == Constants.US) ? 0x7C : f2),
                                                              (char)((f3 == Constants.US) ? 0x7C : f3)));
                        if (c == 0)
                        {
                            errors.AddError(Error.MAJOR_ERROR,
                                            "Bad Multibyte character found, reinterpreting data as non-multibyte data");
                            inMultiByte = false;
                        }
                        else
                        {
                            errors.AddError(Error.MAJOR_ERROR,
                                            "Character after restored vertical bar character makes bad multibyte character, changing it to \"!\"");
                            field[i] = (byte)'!';
                        }
                    }
                }
                justCleaned = false;
                if (field[i] == Constants.US)
                {
                    if (inMultiByte && mbOffset != 0)
                    {
                        field[i] = 0x7C;
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Subfield separator found in middle of a multibyte character, changing it to a vertical bar, and continuing");
                        if (field[i + 1] == '0')
                        {
                            if (field[i + 2] == '(' && field[i + 3] == 'B')
                            {
                                field[i + 1] = 0x1B;
                                errors.AddError(Error.MAJOR_ERROR,
                                                "Character after restored vertical bar character makes bad multibyte character, changing it to ESC");
                            }
                            else
                            {
                                field[i + 1] = 0x21;
                                errors.AddError(Error.MAJOR_ERROR,
                                                "Character after restored vertical bar character makes bad multibyte character, changing it to \"!\"");
                            }
                        }
                        justCleaned = true;
                    }
                    else if (hasEsc && !((field[i + 1] >= 'a' && field[i + 1] <= 'z') || (field[i + 1] >= '0' && field[i + 1] <= '9')))
                    {
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Subfield separator followed by invalid subfield tag, changing separator to a vertical bar, and continuing");
                        field[i] = 0x7C;
                        justCleaned = true;
                    }
                    else if (hasEsc && i < field.Length - 3 &&
                            (field[i + 1] == '0' && field[i + 2] == '(' && field[i + 3] == 'B'))
                    {
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Subfield separator followed by invalid subfield tag, changing separator to a vertical bar, and continuing");
                        field[i] = 0x7C;
                        field[i + 1] = 0x1B;
                        justCleaned = true;
                    }
                    else if (hasEsc && (field[i + 1] == '0'))
                    {
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Subfield separator followed by invalid subfield tag, changing separator to a vertical bar, and continuing");
                        field[i] = 0x7C;
                        field[i + 1] = 0x21;
                        justCleaned = true;
                    }
                    else if (field[i + 1] == Constants.US && field[i + 2] == Constants.US)
                    {
                        errors.AddError(Error.MAJOR_ERROR,
                                        "Three consecutive subfield separators, changing first two to vertical bars.");
                        field[i] = 0x7C;
                        field[i + 1] = 0x7C;
                        justCleaned = true;
                    }
                }
            }
        }

        private int GetFieldLength(InputStream bais)
        {
            bais.Mark(9999);
            int bytesRead = 0;
            while (true)
            {
                switch (bais.ReadByte())
                {
                    case Constants.FT:
                        bais.Reset();
                        return bytesRead;
                    case -1:
                        bais.Reset();
                        if (permissive)
                        {
                            errors.AddError(Error.MINOR_ERROR,
                                            "Field not terminated trying to continue");
                            return bytesRead;
                        }
                        else
                            throw new IOException("Field not terminated");
                    case Constants.US:
                    default:
                        bytesRead++;
                        break;
                }
            }
        }

        private int GetSubfieldLength(InputStream bais)
        {
            bais.Mark(9999);
            int bytesRead = 0;
            while (true)
            {
                switch (bais.ReadByte())
                {
                    case Constants.FT:
                        bais.Reset();
                        return bytesRead;
                    case Constants.US:
                        bais.Reset();
                        return bytesRead;
                    case -1:
                        bais.Reset();
                        if (permissive)
                        {
                            errors.AddError(Error.MINOR_ERROR, "Subfield not terminated trying to continue");
                            return (bytesRead);
                        }
                        else
                            throw new IOException("subfield not terminated");
                    default:
                        bytesRead++;
                        break;
                }
            }
        }

        private int RarseRecordLength(byte[] leaderData)
        {
            int length = -1;
            char[] tmp = Encoding.GetEncoding(encoding).GetChars(leaderData, 0, 5);
            try
            {
                length = int.Parse(new String(tmp));
            }
            catch (FormatException e)
            {
                errors.AddError(Error.FATAL, "Unable to parse record length, Unable to Continue");
                throw new MarcException("unable to parse record length", e);
            }
            return length;
        }

        private void ParseLeader(ILeader ldr, byte[] leaderData)
        {
            //System.err.println("leader is: ("+new String(leaderData, "ISO-8859-1")+")");
            using (var isr = new StreamReader(new MemoryStream(leaderData), Encoding.GetEncoding("ISO-8859-1")))
            {
                char[] tmp = new char[5];
                isr.Read(tmp, 0, tmp.Length);
                //  Skip over bytes for record length, If we get here, its already been computed.
                ldr.RecordStatus = (char)isr.Read();
                ldr.TypeOfRecord = (char)isr.Read();
                tmp = new char[2];
                isr.Read(tmp, 0, tmp.Length);
                ldr.ImplDefined1 = tmp;
                ldr.CharCodingScheme = (char)isr.Read();
                char indicatorCount = (char)isr.Read();
                char subfieldCodeLength = (char)isr.Read();
                char[] baseAddr = new char[5];
                isr.Read(baseAddr, 0, baseAddr.Length);
                tmp = new char[3];
                isr.Read(tmp, 0, tmp.Length);
                ldr.ImplDefined2 = tmp;
                tmp = new char[4];
                isr.Read(tmp, 0, tmp.Length);
                ldr.EntryMap = tmp;
                isr.Close();
                try
                {
                    ldr.IndicatorCount = int.Parse(indicatorCount.ToString());
                }
                catch (FormatException e)
                {
                    if (permissive)
                    {
                        // All Marc21 records should have indicatorCount '2'
                        errors.AddError(Error.ERROR_TYPO, "bogus indicator count - byte value =  " + ((int)indicatorCount & 0xff).ToString("X"));
                        ldr.IndicatorCount = 2;
                    }
                    else
                    {
                        throw new MarcException("unable to parse indicator count", e);
                    }
                }
                try
                {
                    ldr.SubfieldCodeLength = int.Parse(subfieldCodeLength.ToString());
                }
                catch (FormatException e)
                {
                    if (permissive)
                    {
                        // All Marc21 records should have subfieldCodeLength '2' 
                        errors.AddError(Error.ERROR_TYPO, "bogus subfield count - byte value =  " + ((int)subfieldCodeLength & 0xff).ToString("X"));
                        ldr.SubfieldCodeLength = 2;
                    }
                    else
                    {
                        throw new MarcException("unable to parse subfield code length", e);
                    }
                }
                try
                {
                    ldr.BaseAddressOfData = int.Parse(new String(baseAddr));
                }
                catch (FormatException e)
                {
                    throw new MarcException("unable to parse base address of data", e);
                }
            }
        }

        private String GetDataAsString(byte[] bytes)
        {
            String dataElement = null;
            if (encoding.Equals("UTF-8") || encoding.Equals("UTF-8"))
            {
                try
                {
                    dataElement = Encoding.UTF8.GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    throw new MarcException("unsupported encoding", e);
                }
            }
            else if (encoding.Equals("UTF8-Maybe"))
            {
                try
                {
                    dataElement = Encoding.UTF8.GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    throw new MarcException("unsupported encoding", e);
                }
            }
            else if (encoding.Equals("MARC-8") || encoding.Equals("MARC8"))
            {
                dataElement = GetMarc8Conversion(bytes);
            }
            else if (encoding.Equals("Unimarc", StringComparison.CurrentCultureIgnoreCase) || encoding.Equals("IS05426"))
            {
                dataElement = GetUnimarcConversion(bytes);
            }
            else if (encoding.Equals("MARC8-Maybe"))
            {
                String dataElement1 = GetMarc8Conversion(bytes);
                String dataElement2 = GetUnimarcConversion(bytes);
                String dataElement3 = null;
                try
                {
                    dataElement3 = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
                if (dataElement1.Equals(dataElement2) && dataElement1.Equals(dataElement3))
                {
                    dataElement = dataElement1;
                }
                else
                {
                    conversionCheck1 = conversionCheck1 + "|>" + dataElement1.Normalize(NormalizationForm.FormC);
                    conversionCheck2 = conversionCheck2 + "|>" + dataElement2;
                    conversionCheck3 = conversionCheck3 + "|>" + dataElement3;
                    dataElement = dataElement1 + "%%@%%" + dataElement2 + "%%@%%" + dataElement3;
                }
            }
            else if (encoding.Equals("MARC8-Broken"))
            {
                try
                {
                    dataElement = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
                String newdataElement = dataElement.Replace("&lt;", "<");
                newdataElement = newdataElement.Replace("&gt;", ">");
                newdataElement = newdataElement.Replace("&amp;", "&");
                newdataElement = newdataElement.Replace("&apos;", "'");
                newdataElement = newdataElement.Replace("&quot;", "\"");
                if (!newdataElement.Equals(dataElement))
                {
                    dataElement = newdataElement;
                    errors.AddError(Error.ERROR_TYPO, "Subfield contains escaped html character entities, un-escaping them. ");
                }
                String rep1 = "" + (char)0x1b + "\\$1$1";
                String rep2 = "" + (char)0x1b + "\\(B";
                newdataElement = dataElement.ReplaceAll("\\$1(.)", rep1);
                newdataElement = newdataElement.ReplaceAll("\\(B", rep2);
                if (!newdataElement.Equals(dataElement))
                {
                    dataElement = newdataElement;
                    errors.AddError(Error.MAJOR_ERROR, "Subfield seems to be missing MARC8 escape sequences, trying to restore them.");
                }
                try
                {
                    dataElement = GetMarc8Conversion(Encoding.GetEncoding("ISO-8859-1").GetBytes(dataElement));
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }

            }
            else if (encoding.Equals("ISO-8859-1") || encoding.Equals("ISO-8859-1"))
            {
                try
                {
                    dataElement = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    throw new MarcException("unsupported encoding", e);
                }
            }
            else
            {
                try
                {
                    dataElement = Encoding.GetEncoding(encoding).GetString(bytes);
                }
                catch (NotSupportedException e)
                {
                    throw new MarcException("Unknown or unsupported Marc character encoding:" + encoding);
                }
            }
            if (errors != null && Regex.IsMatch(dataElement, "[^&]*&[a-z]*;.*"))
            {
                String newdataElement = dataElement.Replace("&lt;", "<");
                newdataElement = newdataElement.Replace("&gt;", ">");
                newdataElement = newdataElement.Replace("&amp;", "&");
                newdataElement = newdataElement.Replace("&apos;", "'");
                newdataElement = newdataElement.Replace("&quot;", "\"");
                if (!newdataElement.Equals(dataElement))
                {
                    dataElement = newdataElement;
                    errors.AddError(Error.ERROR_TYPO, "Subfield contains escaped html character entities, un-escaping them. ");
                }
            }
            return dataElement;
        }

        private bool ByteArrayContains(byte[] bytes, byte[] seq)
        {
            for (int i = 0; i < bytes.Length - seq.Length; i++)
            {
                if (bytes[i] == seq[0])
                {
                    for (int j = 0; j < seq.Length; j++)
                    {
                        if (bytes[i + j] != seq[j])
                        {
                            break;
                        }
                        if (j == seq.Length - 1) return true;
                    }
                }
            }
            return false;
        }

        static byte[] badEsc = { (byte)('b'), (byte)('-'), 0x1b, (byte)('s') };
        static byte[] overbar = { (byte)(char)(0xaf) };

        private String GetMarc8Conversion(byte[] bytes)
        {
            String dataElement = null;
            if (converterAnsel == null) converterAnsel = new AnselToUnicode(errors);
            if (permissive && (ByteArrayContains(bytes, badEsc) || ByteArrayContains(bytes, overbar)))
            {
                String newDataElement = null;
                try
                {
                    dataElement = Encoding.GetEncoding("ISO-8859-1").GetString(bytes);
                    newDataElement = dataElement.ReplaceAll("(\\e)b-\\es([psb$()])", "$1$2");
                    if (!newDataElement.Equals(dataElement))
                    {
                        dataElement = newDataElement;
                        errors.AddError(Error.MINOR_ERROR, "Subfield contains odd pattern of subscript or superscript escapes. ");
                    }
                    newDataElement = dataElement.Replace((char)0xaf, (char)0xe5);
                    if (!newDataElement.Equals(dataElement))
                    {
                        dataElement = newDataElement;
                        errors.AddError(Error.ERROR_TYPO, "Subfield contains 0xaf overbar character, changing it to proper MARC8 representation ");
                    }
                    dataElement = converterAnsel.Convert(dataElement);
                }
                catch (NotSupportedException e)
                {
                    // TODO Auto-generated catch block
                    Console.WriteLine(e.StackTrace);
                }
            }
            else
            {
                dataElement = converterAnsel.Convert(bytes);
            }
            if (permissive && Regex.IsMatch(dataElement, "[^&]*&#x[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f](%x)?.*"))
            {
                Regex pattern = new Regex("&#x([0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f])(%x)?;?");
                Match matcher = pattern.Match(dataElement);
                StringBuilder newElement = new StringBuilder();
                int prevEnd = 0;
                while (matcher != null && matcher.Success)
                {
                    newElement.Append(dataElement.Substring(prevEnd, matcher.Length));
                    newElement.Append(GetChar(matcher.Groups[1].Value));
                    if (matcher.Groups[1].Value.Contains("%x") || !matcher.Groups[1].Value.EndsWith(";"))
                    {
                        errors.AddError(Error.MINOR_ERROR, "Subfield contains invalid Unicode Character Entity : " + matcher.Groups[0].Value);
                    }
                    prevEnd = matcher.Index + matcher.Length;

                    matcher = matcher.NextMatch();
                }
                newElement.Append(dataElement.Substring(prevEnd));
                dataElement = newElement.ToString();
            }
            return dataElement;
        }

        private String GetUnimarcConversion(byte[] bytes)
        {
            if (converterUnimarc == null) converterUnimarc = new Iso5426ToUnicode();
            String dataElement = converterUnimarc.Convert(bytes);
            dataElement = dataElement.Replace("\u0088", "");
            dataElement = dataElement.Replace("\u0089", "");
            //        for ( int i = 0 ; i < bytes.Length; i++)
            //        {
            //            if (bytes[i] == -120 || bytes[i] == -119)
            //            {
            //                char tmp = (char)bytes[i]; 
            //                char temp2 = dataElement.charAt(0);
            //                char temp3 = dataElement.charAt(4);
            //                int tmpi = (int)tmp;
            //                int tmp2 = (int)temp2;
            //                int tmp3 = (int)temp3;
            //                i = i;
            //
            //            }
            //        }
            if (Regex.IsMatch(dataElement, "[^<]*<U[+][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]>.*"))
            {
                Regex pattern = new Regex("<U[+]([0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f])>");
                Match matcher = pattern.Match(dataElement);
                StringBuilder newElement = new StringBuilder();
                int prevEnd = 0;
                while (matcher != null && matcher.Success)
                {
                    newElement.Append(dataElement.Substring(prevEnd, matcher.Length));
                    newElement.Append(GetChar(matcher.Groups[1].Value));
                    prevEnd = matcher.Index + matcher.Length;
                    matcher = matcher.NextMatch();
                }
                newElement.Append(dataElement.Substring(prevEnd));
                dataElement = newElement.ToString();
            }
            return dataElement;

        }

        private String GetChar(String charCodePoint)
        {
            int charNum = Convert.ToInt32(charCodePoint, 16);
            String result = "" + (char)charNum;
            return result;
        }

        #region IMarkReader Implementation
        public IRecord Current
        {
            get { return record; }
        }

        public void Dispose()
        {
            if(input != null)
                input.Dispose();
            
            record = null;
            converterAnsel = null;
            converterUnimarc = null;
            errors = null;
        }

        object IEnumerator.Current
        {
            get { return record; }
        }

        public bool MoveNext()
        {
            if (!HasNext())
                return false;
            record = Next();
            return record != null;
        }

        public void Reset()
        {
            record = null;
        }
        public IEnumerator<IRecord> GetEnumerator()
        {
            while (MoveNext())
                yield return record;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        #endregion
    }
}