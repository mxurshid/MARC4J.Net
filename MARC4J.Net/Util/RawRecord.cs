using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Util
{
    public class RawRecord //implements MarcReader
    {
        String id;
        byte[] rawRecordData;
        byte[] leader = null;
        //    MarcReader reader = null;

        public RawRecord(InputStream ds)
        {
            Init(ds);
            if (rawRecordData != null)
            {
                id = GetRecordId();
            }
        }

        private void Init(InputStream ds)
        {
            id = null;
            ds.Mark(24);
            if (leader == null) leader = new byte[24];
            try
            {
                ds.Read(leader);
                int length = ParseRecordLength(leader);
                ds.Reset();
                ds.Mark(length * 2);
                rawRecordData = new byte[length];
                try
                {
                    ds.Read(rawRecordData);
                }
                catch (EndOfStreamException e)
                {
                    ds.Reset();
                    int c;
                    int cnt = 0;
                    while ((c = ds.ReadByte()) != -1)
                    {
                        rawRecordData[cnt++] = (byte)c;
                    }
                    int location = ByteArrayContains(rawRecordData, Constants.RT);
                    if (location != -1)
                        length = location + 1;
                    else
                        throw (e);
                }
                if (rawRecordData[length - 1] != Constants.RT)
                {
                    int location = ByteArrayContains(rawRecordData, Constants.RT);
                    // Specified length was longer that actual length
                    if (location != -1)
                    {
                        ds.Reset();
                        rawRecordData = new byte[location];
                        ds.Read(rawRecordData);
                    }
                    else // keep reading until end of record found
                    {
                        var recBuf = new List<byte>();
                        ds.Reset();
                        byte[] byteRead = new byte[1];
                        while (true)
                        {
                            int numRead = ds.Read(byteRead);
                            if (numRead == -1) break; // probably should throw something here. 
                            recBuf.Add(byteRead[0]);
                            if (byteRead[0] == Constants.RT) break;
                        }
                        rawRecordData = new byte[recBuf.Count];
                        for (int i = 0; i < recBuf.Count; i++)
                        {
                            rawRecordData[i] = recBuf[i];
                        }
                    }
                }
            }
            catch (IOException e)
            {
                try
                {
                    rawRecordData = null;
                    ds.Reset();
                }
                catch (IOException e1)
                {
                }
            }

        }

        private static int ByteArrayContains(byte[] data, int value)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == value) return i;
            }
            return -1;
        }

        public RawRecord(RawRecord rec1, RawRecord rec2)
        {
            rawRecordData = new byte[rec1.GetRecordBytes().Length + rec2.GetRecordBytes().Length];
            Array.Copy(rec1.GetRecordBytes(), 0, rawRecordData, 0, rec1.GetRecordBytes().Length);
            Array.Copy(rec2.GetRecordBytes(), 0, rawRecordData, rec1.GetRecordBytes().Length, rec2.GetRecordBytes().Length);
            id = GetRecordId();
        }

        public String GetRecordId()
        {
            if (id != null) return (id);
            id = GetFieldVal("001");
            return (id);
        }

        public String GetFieldVal(String idField)
        {
            String recordStr = null;
            try
            {
                recordStr = Encoding.GetEncoding("ISO-8859-1").GetString(rawRecordData);
            }
            catch (NotSupportedException e)
            {
                Console.WriteLine(e.StackTrace);
            }
            int offset = int.Parse(recordStr.Substring(12, 5));
            int dirOffset = 24;
            String fieldNum = recordStr.Substring(dirOffset, 3);
            while (dirOffset < offset)
            {
                if (fieldNum.Equals(idField))
                {
                    int length = int.Parse(recordStr.Substring(dirOffset + 3, 4));
                    int offset2 = int.Parse(recordStr.Substring(dirOffset + 7, 5));
                    String id = recordStr.Substring(offset + offset2, offset + offset2 + length - 1).Trim();
                    return (id);
                }
                dirOffset += 12;
                fieldNum = recordStr.Substring(dirOffset, dirOffset + 3);
            }
            return (null);
        }

        public byte[] GetRecordBytes()
        {
            return rawRecordData;
        }

        public IRecord GetAsRecord(bool permissive, bool toUtf8, String combinePartials, String defaultEncoding)
        {
            var bais = new MemoryStream(rawRecordData);
            MarcPermissiveStreamReader reader = new MarcPermissiveStreamReader(bais, permissive, toUtf8, defaultEncoding);
            IRecord next = null;
            if (reader.MoveNext())
                next = reader.Current;
            if (combinePartials != null)
            {
                while (reader.MoveNext())
                {
                    var nextNext = reader.Current;
                    var fieldsAll = nextNext.GetVariableFields();
                    var fieldIter = fieldsAll.GetEnumerator();
                    while (fieldIter.MoveNext())
                    {
                        var vf = fieldIter.Current;
                        if (combinePartials.Contains(vf.Tag))
                        {
                            next.AddVariableField(vf);
                        }
                    }
                }
            }
            return next;
        }

        private static int ParseRecordLength(byte[] leaderData)
        {
            using (var isr = new StreamReader(new MemoryStream(leaderData)))
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
                    throw new IOException("unable to parse record length");
                }
                return length;
            }
        }
    }
}