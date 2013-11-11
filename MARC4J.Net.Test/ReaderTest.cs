using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;
using System.IO;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class ReaderTest
    {
        [TestMethod]
        public void TestMarcStreamReader()
        {
            using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_CHABON_MRC, FileMode.Open))
            {
                Assert.IsNotNull(fs);
                using (MarcStreamReader reader = new MarcStreamReader(fs))
                {
                    Assert.IsTrue(reader.MoveNext(), "Should have at least one record");

                    var record1 = reader.Current;
                    TestUtils.ValidateKavalieAndClayRecord(record1);

                    Assert.IsTrue(reader.MoveNext(), "Should have at least two records");
                    var record2 = reader.Current;
                    TestUtils.ValidateSummerlandRecord(record2);

                    Assert.IsFalse(reader.MoveNext(), " have more than two records");
                }
            }
        }
    }
}
