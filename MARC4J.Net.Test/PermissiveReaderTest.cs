using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;
using System.IO;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class PermissiveReaderTest
    {

        [TestMethod]
        public void TestBadLeaderBytes10_11()
        {
            int i = 0;
            using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BAD_LEADERS_10_11_MRC, FileMode.Open))
            {
                IMarcReader reader = new MarcPermissiveStreamReader(fs, true, true);
                while (reader.MoveNext())
                {
                    var record = reader.Current;

                    Assert.AreEqual(2, record.Leader.IndicatorCount);
                    Assert.AreEqual(2, record.Leader.SubfieldCodeLength);
                    i++;
                }
                Assert.AreEqual(1, i);
            }
        }

        [TestMethod]
        public void TestTooLongMarcRecord()
        {
            using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BAD_TOO_LONG_PLUS_2_MRC, FileMode.Open))
            {
                Assert.IsNotNull(fs);
                // This marc file has three records, but the first one
                // is too long for a marc binary record. Can we still read
                // the next two?
                IMarcReader reader = new MarcPermissiveStreamReader(fs, true, true);
                reader.MoveNext();
                var bad_record = reader.Current;

                // Bad record is a total loss, don't even bother trying to read
                // it, but do we get the good records next?
                reader.MoveNext();
                var good_record1 = reader.Current;
                var good001 = good_record1.GetControlNumberField();
                Assert.AreEqual(good001.Data, "360945");

                reader.MoveNext();
                var good_record2 = reader.Current;
                good001 = good_record2.GetControlNumberField();
                Assert.AreEqual(good001.Data, "360946");
            }
        }

        [TestMethod]
        public void testTooLongLeaderByteRead()
        {
            using (var fs = new FileStream(Environment.CurrentDirectory + StaticTestRecords.RESOURCES_BAD_TOO_LONG_PLUS_2_MRC, FileMode.Open))
            {
                Assert.IsNotNull(fs);
                IMarcReader reader = new MarcPermissiveStreamReader(fs, true, true);

                reader.MoveNext();
                //First record is the long one. 
                var weird_record = reader.Current;

                //is it's marshal'd leader okay?
                String strLeader = weird_record.Leader.Marshal();

                // Make sure only five digits for length is used in the leader,
                // even though it's not big enough to hold the leader, we need to
                // make sure byte offsets in the rest of the leader are okay. 
                Assert.AreEqual("nas", strLeader.Substring(5, 3));

                // And length should be set to our 99999 overflow value
                Assert.AreEqual("99999", strLeader.Substring(0, 5));
            }
        }
    }
}
