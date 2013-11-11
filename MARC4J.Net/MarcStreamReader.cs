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
    /// An iterator over a collection of MARC records in ISO 2709 format.
    /// <para/>
    /// Example usage:
    /// 
    /// <para/>
    /// <para>InputStream input = new FileInputStream("file.mrc");</para>
    /// <para>MarcReader reader = new MarcStreamReader(input);	  </para>
    /// <para>while (reader.hasNext()) {						  </para>
    /// <para>    Record record = reader.next();				  </para>
    /// <para>    // Process record								  </para>
    /// <para>}													  </para>
    /// </para>
    /// <para>
    /// When no encoding is given as an constructor argument the parser tries to
    /// resolve the encoding by looking at the character coding scheme (leader
    /// position 9) in MARC21 records. For UNIMARC records this position is not
    /// defined.
    /// </para>
    /// </summary>
    public class MarcStreamReader : IMarcReader
    {

        private InputStream input = null;

        private IRecord record;

        private readonly MarcFactory factory;

        private String encoding = "ISO-8859-1";

        private bool isOverride = false;

        private CharConverter converterAnsel = null;

        /// <summary>
        /// Constructs an instance with the specified input stream.
        /// </summary>
        /// <param name="input"></param>
        public MarcStreamReader(Stream input)
            : this(input, null)
        {

        }

        /// <summary>
        /// Constructs an instance with the specified input stream.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="encoding"></param>
        public MarcStreamReader(Stream input, String encoding)
        {
            this.input = new InputStream(input);
            factory = MarcFactory.Instance;
            if (encoding != null)
            {
                this.encoding = encoding;
                isOverride = true;
            }
        }

        /// <summary>
        /// Returns true if the iteration has more records, false otherwise.
        /// </summary>
        /// <returns></returns>
        private bool HasNext()
        {
            try
            {
                input.Mark(10);
                if (input.ReadByte() == -1)
                    return false;
                input.Reset();
            }
            catch (IOException e)
            {
                throw new MarcException(e.Message, e);
            }
            return true;
        }

        /// <summary>
        /// Returns the next record in the iteration.
        /// </summary>
        /// <returns></returns>
        private IRecord Next()
        {
            record = factory.NewRecord();

            try
            {

                byte[] byteArray = new byte[24];
                input.Read(byteArray);

                int recordLength = ParseRecordLength(byteArray);
                byte[] recordBuf = new byte[recordLength - 24];
                input.Read(recordBuf);
                ParseRecord(record, byteArray, recordBuf, recordLength);
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

        private void ParseRecord(IRecord record, byte[] byteArray, byte[] recordBuf, int recordLength)
        {
            ILeader ldr;
            ldr = factory.NewLeader();
            ldr.RecordLength = recordLength;
            int directoryLength = 0;

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
                throw new MarcException("error parsing leader with data: "
                        + byteArray.ConvertToString(), e);
            }

            // if MARC 21 then check encoding
            switch (ldr.CharCodingScheme)
            {
                case ' ':
                    if (!isOverride)
                        encoding = "ISO-8859-1";
                    break;
                case 'a':
                    if (!isOverride)
                        encoding = "UTF-8";
                    break;
            }
            record.Leader = ldr;

            if ((directoryLength % 12) != 0)
            {
                throw new MarcException("invalid directory");
            }
            using (var inputrec = new InputStream(recordBuf))
            {
                int size = directoryLength / 12;

                String[] tags = new String[size];
                int[] lengths = new int[size];

                byte[] tag = new byte[3];
                byte[] length = new byte[4];
                byte[] start = new byte[5];

                String tmp;

                try
                {
                    for (int i = 0; i < size; i++)
                    {
                        inputrec.Read(tag);
                        tmp = tag.ConvertToString();
                        tags[i] = tmp;

                        inputrec.Read(length);
                        tmp = length.ConvertToString();
                        lengths[i] = int.Parse(tmp);

                        inputrec.Read(start);
                    }

                    if (inputrec.ReadByte() != Constants.FT)
                    {
                        throw new MarcException("expected field terminator at end of directory");
                    }

                    for (int i = 0; i < size; i++)
                    {
                        GetFieldLength(inputrec);
                        if (Verifier.IsControlField(tags[i]))
                        {
                            byteArray = new byte[lengths[i] - 1];
                            inputrec.Read(byteArray);

                            if (inputrec.ReadByte() != Constants.FT)
                            {
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
                            catch (Exception e)
                            {
                                throw new MarcException(
                                        "error parsing data field for tag: " + tags[i]
                                                + " with data: "
                                                + byteArray.ConvertToString(), e);
                            }
                        }
                    }

                    if (inputrec.ReadByte() != Constants.RT)
                    {
                        throw new MarcException("expected record terminator");
                    }
                }
                catch (IOException e)
                {
                    throw new MarcException("an error occured reading input", e);
                }
            }
        }

        private IDataField ParseDataField(String tag, byte[] field)
        {
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
                            data = new byte[size];
                            bais.Read(data);
                            subfield = factory.NewSubfield();
                            subfield.Code = (char)code;
                            subfield.Data = GetDataAsString(data);
                            dataField.AddSubfield(subfield);
                            break;
                        case Constants.FT:
                            break;
                    }
                }
                return dataField;
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
                    case Constants.US:
                    case Constants.FT:
                        bais.Reset();
                        return bytesRead;
                    case -1:
                        bais.Reset();
                        throw new IOException("subfield not terminated");
                    default:
                        bytesRead++;
                        break;
                }
            }
        }

        private int ParseRecordLength(byte[] leaderData)
        {
            using (var isr = new StreamReader(new MemoryStream(leaderData), Encoding.GetEncoding(encoding)))
            {
                int length = -1;
                char[] tmp = new char[5];
                isr.Read(tmp, 0, tmp.Length);
                try
                {
                    length = int.Parse(new String(tmp));
                }
                catch (FormatException e)
                {
                    throw new MarcException("unable to parse record length", e);
                }
                return length;
            }
        }

        private void ParseLeader(ILeader ldr, byte[] leaderData)
        {
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
                    throw new MarcException("unable to parse indicator count", e);
                }
                try
                {
                    ldr.SubfieldCodeLength = int.Parse(subfieldCodeLength.ToString());
                }
                catch (FormatException e)
                {
                    throw new MarcException("unable to parse subfield code length", e);
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
            if (encoding.Equals("UTF-8") || encoding.Equals("UTF8"))
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
                if (converterAnsel == null) converterAnsel = new AnselToUnicode();
                dataElement = converterAnsel.Convert(bytes);
            }
            else if (encoding.Equals("ISO-8859-1") || encoding.Equals("ISO_8859_1"))
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
            return dataElement;
        }

        #region IMarkReader implementation
        public IRecord Current
        {
            get { return record; }
        }

        public void Dispose()
        {
            if (input != null)
            { 
                input.Dispose();
                input = null;
            }
            record = null;
            converterAnsel = null;
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