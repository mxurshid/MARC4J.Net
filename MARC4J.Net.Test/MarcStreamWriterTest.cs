using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;
using System.IO;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class MarcStreamWriterTest
    {
        [TestMethod]
        public void TestMarcStreamWriter()
        {
            using (var output = new MemoryStream())
            {
                MarcStreamWriter writer = new MarcStreamWriter(output);
                foreach (var record in StaticTestRecords.summerland)
                {
                    writer.Write(record);
                }
                writer.Close();
                TestUtils.ValidateBytesAgainstFile(output.ToArray(), StaticTestRecords.RESOURCES_SUMMERLAND_MRC);
            }
        }
    }
}