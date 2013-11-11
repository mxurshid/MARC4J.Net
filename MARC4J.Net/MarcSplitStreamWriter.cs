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
    public class MarcSplitStreamWriter : MarcStreamWriter
    {
        private int recordThreshhold;
        private String fieldsToSplit;
        private Regex regex;

        public MarcSplitStreamWriter(Stream output, int threshhold, String fieldsToSplit)
            : base(output, false)
        {
            recordThreshhold = threshhold;
            this.fieldsToSplit = fieldsToSplit;
            regex = new Regex(fieldsToSplit);
        }

        public MarcSplitStreamWriter(Stream output, String encoding, int threshhold, String fieldsToSplit)
            : base(output, encoding, false)
        {
            recordThreshhold = threshhold;
            this.fieldsToSplit = fieldsToSplit;
            regex = new Regex(fieldsToSplit);
        }

        /// <summary>
        /// Writes a <code>Record</code> object to the writer.
        /// </summary>
        /// <param name="record"></param>
        public override void Write(IRecord record)
        {
            bool doneWithRec = false;
            foreach (var df in record.GetDataFields())
            {
                if (!regex.IsMatch(df.Tag)) continue;
                df.Id = 0;
            }

            while (!doneWithRec)
            {
                try
                {
                    int previous = 0;
                    using (var data = new MemoryStream())
                    {
                        using (var dir = new MemoryStream())
                        {
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
                                if (regex.IsMatch(df.Tag))
                                {
                                    continue;
                                }
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
                            // data fields
                            doneWithRec = true;
                            foreach (var df in record.GetDataFields())
                            {
                                if (previous >= recordThreshhold)
                                {
                                    doneWithRec = false;
                                    break;
                                }
                                if (!regex.IsMatch(df.Tag)) continue;
                                if (!(df.Id != 0)) continue;
                                df.Id = 0;
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

                            if (!allowOversizeEntry && (hasOversizeLength))
                            {
                                throw new MarcException("Record has field that is too long to be a valid MARC binary record. The maximum length for a field counting all of the sub-fields is 9999 bytes.");
                            }
                            WriteLeader(ldr);
                            output.Write(dir.ToArray());
                            output.Write(data.ToArray());
                            output.Write(Constants.RT);
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
        }
    }
}