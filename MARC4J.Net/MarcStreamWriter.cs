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
    /// Class for writing MARC record objects in ISO 2709 format.
    /// 
    /// <para>
    /// The following example reads a file with MARCXML records and outputs the
    /// record set in ISO 2709 format:
    /// </para>
    /// 
    /// <example>
    /// <para>InputStream input = new FileInputStream(&quot;marcxml.xml&quot;);</para>
    /// <para>MarcXmlReader reader = new MarcXmlReader(input);				   </para>
    /// <para>MarcWriter writer = new MarcStreamWriter(System.out);			   </para>
    /// <para>while (reader.hasNext()) {									   </para>
    /// <para>    Record record = reader.next();							   </para>
    /// <para>    writer.Write(record);										   </para>
    /// <para>}																   </para>
    /// <para>writer.close();												   </para>
    /// </example>
    /// 
    /// <para>
    /// To convert characters like for example from UCS/Unicode to MARC-8 register
    /// a MARC4J.Net.Converter.CharConverter implementation:
    /// </para>
    /// 
    /// <example>
    /// <para>InputStream input = new FileInputStream(&quot;marcxml.xml&quot;);</para>
    /// <para>MarcXmlReader reader = new MarcXmlReader(input);				   </para>
    /// <para>MarcWriter writer = new MarcStreamWriter(System.out);			   </para>
    /// <para>writer.setConverter(new UnicodeToAnsel());					   </para>
    /// <para>while (reader.hasNext()) {									   </para>
    /// <para>    Record record = reader.next();							   </para>
    /// <para>    writer.Write(record);										   </para>
    /// <para>}																   </para>
    /// <para>writer.close();												   </para>
    /// </example>
    /// </summary>
    public class MarcStreamWriter : IMarcWriter
    {

        protected BinaryWriter output = null;

        protected Encoding encoding = Encoding.GetEncoding("ISO-8859-1");

        private CharConverter converter = null;
        protected bool allowOversizeEntry = false;
        protected bool hasOversizeOffset = false;
        protected bool hasOversizeLength = false;

        //protected static DecimalFormat format4Use = new CustomDecimalFormat(4);

        //protected static DecimalFormat format5Use = new CustomDecimalFormat(5);

        /// <summary>
        /// Constructs an instance and creates a <code>Writer</code> object with
        /// the specified output stream.
        /// </summary>
        /// <param name="output"></param>
        public MarcStreamWriter(Stream output)
            : this(output, null)
        {
        }

        /// <summary>
        /// Constructs an instance and creates a <code>Writer</code> object with
        /// the specified output stream and character encoding.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoding"></param>
        public MarcStreamWriter(Stream output, String encoding)
            : this(output, encoding, false)
        {
        }
        /// <summary>
        /// Constructs an instance and creates a <code>Writer</code> object with
        /// the specified output stream.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="allowOversizeRecord"></param>
        public MarcStreamWriter(Stream output, bool allowOversizeRecord)
            : this(output, null, allowOversizeRecord)
        {
        }

        /// <summary>
        /// Constructs an instance and creates a <code>Writer</code> object with
        /// the specified output stream and character encoding.
        /// </summary>
        /// <param name="output"></param>
        /// <param name="encoding"></param>
        /// <param name="allowOversizeRecord"></param>
        public MarcStreamWriter(Stream output, String encoding, bool allowOversizeRecord)
        {
            if (encoding != null)
                this.encoding = Encoding.GetEncoding(encoding);
            this.output = new BinaryWriter(output);
            this.allowOversizeEntry = allowOversizeRecord;
        }

        public CharConverter Converter
        {
            get
            {
                return converter;
            }
            set
            {
                converter = value;
            }
        }

        /// <summary>
        /// Writes a <code>Record</code> object to the writer.
        /// </summary>
        /// <param name="record"></param>
        public virtual void Write(IRecord record)
        {
            int previous = 0;

            try
            {
                using (var data = new MemoryStream())
                {
                    using (var dir = new MemoryStream())
                    {
                        hasOversizeOffset = false;
                        hasOversizeLength = false;

                        // control fields
                        foreach (var cf in record.GetControlFields())
                        {
                            data.Write(GetDataElement(cf.Data));
                            data.Write(Constants.FT);
                            dir.Write(GetEntry(cf.Tag, (int)data.Length - previous, previous));
                            previous = (int)data.Length;
                        }

                        // data fields
                        foreach (var df in record.GetDataFields())
                        {
                            data.Write(df.Indicator1);
                            data.Write(df.Indicator2);
                            foreach (var sf in df.GetSubfields())
                            {
                                data.Write(Constants.US);
                                data.Write(sf.Code);
                                data.Write(GetDataElement(sf.Data));
                            }
                            data.Write(Constants.FT);
                            dir.Write(GetEntry(df.Tag, (int)data.Length - previous, previous));
                            previous = (int)data.Length;
                        }
                        dir.Write(Constants.FT);

                        // base address of data and logical record length
                        var ldr = record.Leader;

                        int baseAddress = 24 + (int)dir.Length;
                        ldr.BaseAddressOfData = baseAddress;
                        int recordLength = ldr.BaseAddressOfData + (int)data.Length + 1;
                        ldr.RecordLength = recordLength;

                        // write record to output stream
                        dir.Close();
                        data.Close();

                        if (!allowOversizeEntry && (baseAddress > 99999 || recordLength > 99999 || hasOversizeOffset))
                        {
                            throw new MarcException("Record is too long to be a valid MARC binary record, it's length would be " + recordLength + " which is more thatn 99999 bytes");
                        }
                        if (!allowOversizeEntry && (hasOversizeLength))
                        {
                            throw new MarcException("Record has field that is too long to be a valid MARC binary record. The maximum length for a field counting all of the sub-fields is 9999 bytes.");
                        }
                        if (converter != null)
                            ldr.CharCodingScheme = converter.OutputsUnicode() ? 'a' : ' ';
                        WriteLeader(ldr);
                        output.Write(dir.ToArray());
                        output.Write(data.ToArray());
                        output.Write(Convert.ToByte(Constants.RT));
                    }
                }
            }
            catch (IOException e)
            {
                throw new MarcException("IO Error occured while writing record", e);
            }
            catch (MarcException e)
            {
                throw e;
            }
        }

        protected void WriteLeader(ILeader ldr)
        {
            output.Write(encoding.GetBytes(ldr.RecordLength.ToString().PadLeft(5, '0')));
            output.Write(ldr.RecordStatus);
            output.Write(ldr.TypeOfRecord);
            output.Write(encoding.GetBytes(new String(ldr.ImplDefined1)));
            output.Write(ldr.CharCodingScheme);
            output.Write(encoding.GetBytes(ldr.IndicatorCount.ToString()));
            output.Write(encoding.GetBytes(ldr.SubfieldCodeLength.ToString()));
            output.Write(encoding.GetBytes(ldr.BaseAddressOfData.ToString().PadLeft(5, '0')));
            output.Write(encoding.GetBytes(new String(ldr.ImplDefined2)));
            output.Write(encoding.GetBytes(new String(ldr.EntryMap)));
        }

        /// <summary>
        /// Closes the writer.
        /// </summary>
        public void Close()
        {
            try
            {
                output.Close();
            }
            catch (IOException e)
            {
                throw new MarcException("IO Error occured on close", e);
            }
        }

        protected byte[] GetDataElement(String data)
        {
            if (converter != null)
                return encoding.GetBytes(converter.Convert(data));
            return encoding.GetBytes(data);
        }

        protected byte[] GetEntry(String tag, int length, int start)
        {
            String entryUse = tag + length.ToString().PadLeft(4, '0') + start.ToString().PadLeft(5, '0');
            if (length > 9999) hasOversizeLength = true;
            if (start > 99999) hasOversizeOffset = true;
            return encoding.GetBytes(entryUse);
        }

        public bool AllowsOversizeEntry
        {
            get
            {
                return allowOversizeEntry;
            }
            set
            {
                allowOversizeEntry = value;
            }
        }
        public void Dispose()
        {
            if (output != null)
                output.Dispose();
            encoding = null;
            converter = null;
        }
    }
}