using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;
using System.IO;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class MarcXmlReaderTest
    {
        [TestMethod]
        public void TestMarcXmlReader()
        {
            using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_CHABON_XML, FileMode.Open))
            {
                Assert.IsNotNull(fs);
                var reader = new MarcXmlReader().Read(fs).GetEnumerator();

                Assert.IsTrue(reader.MoveNext(), "Should have at least one record");

                var record1 = reader.Current;
                TestUtils.ValidateKavalieAndClayRecord(record1);

                Assert.IsTrue(reader.MoveNext(), "Should have at least two records");
                var record2 = reader.Current;
                TestUtils.ValidateSummerlandRecord(record2);

                Assert.IsFalse(reader.MoveNext(), " have more than two records");
            }
        }

        [TestMethod]
        public void TestReadIndicatorlessRecord()
        {
            using (var fs = new FileStream(Environment.CurrentDirectory + "\\Resources\\cruel-cruel-indicatorless-summerland.xml", FileMode.Open))
            {
                Assert.IsNotNull(fs);
                var reader = new MarcXmlReader().Read(fs).GetEnumerator();
                Assert.IsTrue(reader.MoveNext());
                var record = reader.Current;
            }
        }
    }
}