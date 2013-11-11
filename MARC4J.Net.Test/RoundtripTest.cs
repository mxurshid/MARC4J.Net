using System;
using System.Linq;
using System.IO;
using MARC4J.Net.MARC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.Util;
using MARC4J.Net.Converter;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class RoundtripTest
    {

        /// <summary>
        /// This Test reads in a file of utf-8 encoded binary Marc records
        /// then writes those records out as utf-8 encoded binary Marc records,
        /// then reads those records.  The Test then compares those records with
        /// the original records, expecting them to be identical.
        /// </summary>
        [TestMethod]
        public void TestWriteAndReadRoundtrip()
        {
            MemoryStream out2 = null;
            using (var input = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_CHABON_MRC, FileMode.Open))
            {
                Assert.IsNotNull(input);
                using (var out1 = new MemoryStream())
                {
                    MarcStreamReader marcReader = new MarcStreamReader(input);
                    using (MarcXmlWriter xmlWriter = new MarcXmlWriter(out1))
                    {
                        while (marcReader.MoveNext())
                        {
                            var record = marcReader.Current;
                            xmlWriter.Write(record);
                        }
                    }
                    out2 = new MemoryStream();
                    var @in = new MemoryStream(out1.ToArray());
                    var xmlReader = new MarcXmlReader(@in).GetEnumerator();
                    MarcStreamWriter marcWriter = new MarcStreamWriter(out2);
                    while (xmlReader.MoveNext())
                    {
                        var record = xmlReader.Current;
                        marcWriter.Write(record);
                    }
                    @in.Close();
                }
            }
            using (var inputCompare1 = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_CHABON_MRC, FileMode.Open))
            {
                var inputCompare2 = out2;
                var readComp1 = new MarcStreamReader(inputCompare1);
                var readComp2 = new MarcStreamReader(inputCompare2);
                IRecord r1, r2;
                do
                {
                    r1 = (readComp1.MoveNext()) ? readComp1.Current : null;
                    r2 = (readComp2.MoveNext()) ? readComp2.Current : null;
                    if (r1 != null && r2 != null)
                        RecordTestingUtils.AssertEqualsIgnoreLeader(r1, r2);
                } while (r1 != null && r2 != null);
            }
            out2.Dispose();
        }

        /// <summary>
        /// This Test reads in a file of Marc8 encoded binary Marc records
        /// then writes those records out as utf-8 encoded binary Marc records,
        /// then reads those records back in and writes them out as Marc8 encoded binary
        /// Marc records. The Test then compares those records with the original
        /// records, expecting them to be identical.
        /// </summary>
        [TestMethod]
        public void TestWriteAndReadRoundtripConverted()
        {
            MemoryStream out2 = null;
            using (var input = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
            {
                Assert.IsNotNull(input);
                var out1 = new MemoryStream();
                MarcStreamReader marcReader1 = new MarcStreamReader(input);
                MarcStreamWriter marcWriter1 = new MarcStreamWriter(out1, "UTF-8");
                marcWriter1.Converter = new AnselToUnicode();
                while (marcReader1.MoveNext())
                {
                    var record = marcReader1.Current;
                    marcWriter1.Write(record);
                }
                input.Close();
                marcWriter1.Close();
                out1.Close();
                out2 = new MemoryStream();
                var @in = new MemoryStream(out1.ToArray());
                MarcStreamReader marcReader2 = new MarcStreamReader(@in);
                MarcStreamWriter marcWriter2 = new MarcStreamWriter(out2);
                marcWriter2.Converter = new UnicodeToAnsel();
                while (marcReader2.MoveNext())
                {
                    var record = marcReader2.Current;
                    marcWriter2.Write(record);
                }
                @in.Close();
            }

            using (var inputCompare1 = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
            {
                var inputCompare2 = out2;
                var readComp1 = new MarcStreamReader(inputCompare1);
                var readComp2 = new MarcStreamReader(inputCompare2);
                IRecord r1, r2;
                do
                {
                    r1 = (readComp1.MoveNext()) ? readComp1.Current : null;
                    r2 = (readComp2.MoveNext()) ? readComp2.Current : null;
                    if (r1 != null && r2 != null)
                        RecordTestingUtils.AssertEqualsIgnoreLeader(r1, r2);
                } while (r1 != null && r2 != null);
            }

            out2.Dispose();
        }

        /// <summary>
        /// This Test reads in a file of Marc8 encoded binary Marc records
        /// then writes those records out as utf-8 encoded MarcXML records,
        /// then reads those records back in and writes them out as Marc8 binary
        /// Marc records. The Test then compares those binary Marc records with the original
        /// records, expecting them to be identical.
        /// </summary>
        [TestMethod]
        public void TestConvertToXMLRoundtrip()
        {
            using (var input = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
            {
                Assert.IsNotNull(input);
                var out1 = new MemoryStream();
                MarcStreamReader marcReader = new MarcStreamReader(input);
                MarcXmlWriter xmlWriter = new MarcXmlWriter(out1);
                xmlWriter.Converter = new AnselToUnicode();
                while (marcReader.MoveNext())
                {
                    var record = marcReader.Current;
                    xmlWriter.Write(record);
                }
                input.Close();
                xmlWriter.Close();
                out1.Close();
                var out2 = new MemoryStream();
                MemoryStream @in = new MemoryStream(out1.ToArray());
                var xmlReader = new MarcXmlReader(@in).GetEnumerator();
                MarcStreamWriter marcWriter = new MarcStreamWriter(out2);
                marcWriter.Converter = new UnicodeToAnsel();
                while (xmlReader.MoveNext())
                {
                    var record = xmlReader.Current;
                    marcWriter.Write(record);
                }
                @in.Close();
                marcWriter.Close();

                out2.Close();
                MemoryStream out3 = new MemoryStream();
                MemoryStream in2 = new MemoryStream(out2.ToArray());
                MarcStreamReader marcReader2 = new MarcStreamReader(in2);
                MarcXmlWriter xmlWriter2 = new MarcXmlWriter(out3);
                xmlWriter2.Converter = new AnselToUnicode();
                while (marcReader2.MoveNext())
                {
                    var record = marcReader2.Current;
                    xmlWriter2.Write(record);
                }
                in2.Close();
                xmlWriter2.Close();

                out3.Close();

                var inputCompare1 = new MemoryStream(out1.ToArray());
                var inputCompare2 = new MemoryStream(out3.ToArray());
                var readComp1 = new MarcXmlReader(inputCompare1).GetEnumerator();
                var readComp2 = new MarcXmlReader(inputCompare2).GetEnumerator();
                IRecord r1, r2;
                do
                {
                    r1 = (readComp1.MoveNext()) ? readComp1.Current : null;
                    r2 = (readComp2.MoveNext()) ? readComp2.Current : null;
                    if (r1 != null && r2 != null)
                        RecordTestingUtils.AssertEqualsIgnoreLeader(r1, r2);
                } while (r1 != null && r2 != null);
            }
        }

        /// <summary>
        /// This Test reads in a file of Marc8 encoded binary Marc records
        /// then writes those records out as utf-8 encoded MarcXML records using unicode
        /// normalization, which combines diacritics with the character they adorn (whenever
        /// possible).  It then reads those records back in and writes them out as Marc8 binary
        /// Marc records. The Test then compares those binary Marc records with original binary
        /// Marc records, expecting them to be identical.
        /// Note: Since there are multiple ways of representing some unicode characters in marc8,
        /// it is not possible to guarantee roundtripping from marc8 encoded records to utf-8 and
        /// back to marc8, the likelihood is even higher when the utf-8 characters are normalized.
        /// It is possible to guarantee roundtripping from normalized utf-8 encoded records to
        /// marc8 encoded binary marc records back to normalized utf-8 records.
        /// </summary>
        [TestMethod]
        public void TestConvertToXMLNormalizedRoundtrip()
        {
            MemoryStream out1 = null;
            using (var input = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
            {
                Assert.IsNotNull(input);
                out1 = new MemoryStream();
                MarcStreamReader marcReader = new MarcStreamReader(input);
                MarcXmlWriter xmlWriter = new MarcXmlWriter(out1);
                xmlWriter.Converter = new AnselToUnicode();
                xmlWriter.UnicodeNormalization = true;
                while (marcReader.MoveNext())
                {
                    var record = marcReader.Current;
                    xmlWriter.Write(record);
                }
                input.Close();

                xmlWriter.Close();
                out1.Close();
            }
            using (MemoryStream out2 = new MemoryStream())
            {
                using (MemoryStream @in = new MemoryStream(out1.ToArray()))
                {
                    var xmlReader = new MarcXmlReader(@in).GetEnumerator();
                    MarcStreamWriter marcWriter = new MarcStreamWriter(out2);
                    marcWriter.Converter = new UnicodeToAnsel();
                    while (xmlReader.MoveNext())
                    {
                        var record = xmlReader.Current;
                        marcWriter.Write(record);
                    }
                    @in.Close();
                    marcWriter.Close();

                    out2.Close();

                    using (var inputCompare1 = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BRKRTEST_MRC, FileMode.Open))
                    {
                        Assert.IsNotNull(inputCompare1);
                        var inputCompare2 = new MemoryStream(out2.ToArray());
                        MarcStreamReader readComp1 = new MarcStreamReader(inputCompare1);
                        MarcStreamReader readComp2 = new MarcStreamReader(inputCompare2);
                        IRecord r1, r2;
                        do
                        {
                            r1 = (readComp1.MoveNext()) ? readComp1.Current : null;
                            r2 = (readComp2.MoveNext()) ? readComp2.Current : null;
                            if (r1 != null && r2 != null)
                                RecordTestingUtils.AssertEqualsIgnoreLeader(r1, r2);
                        } while (r1 != null && r2 != null);
                    }
                }
            }
        }
    }
}