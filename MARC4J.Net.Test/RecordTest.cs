using System;
using System.Linq;
using System.IO;
using MARC4J.Net.MARC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.Util;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class RecordTest
    {
        IRecord record = StaticTestRecords.GetSummerlandRecord();

        [TestMethod]
        public void TestGetFields()
        {
            var cn = record.GetControlNumber();
            Assert.AreEqual("12883376", cn);

            var cf = record.GetControlNumberField();
            Assert.AreEqual("001", cf.Tag);
            Assert.AreEqual("12883376", cf.Data);

            var fieldList = record.GetVariableFields();
            Assert.AreEqual(15, fieldList.Count);

            var controlFieldList = record.GetControlFields();
            Assert.AreEqual(3, controlFieldList.Count);

            var dataFieldList = record.GetDataFields();
            Assert.AreEqual(12, dataFieldList.Count);

            var field = record.GetVariableField("245");
            Assert.AreEqual("245", field.Tag);

            fieldList = record.GetVariableFields("650");
            Assert.AreEqual(3, fieldList.Count);

            String[] fields = { "245", "260", "300" };
            fieldList = record.GetVariableFields(fields);
            Assert.AreEqual(3, fieldList.Count);
        }

        [TestMethod]
        public void TestFind()
        {
            var field = record.GetVariableField("245");
            Assert.AreEqual(true, field.Find("Summerland"));
            Assert.AreEqual(true, field.Find("Sum*erland"));
            Assert.AreEqual(true, field.Find("[Cc]habo[a-z]"));

            field = record.GetVariableField("008");
            Assert.AreEqual(true, field.Find("eng"));

            var result = record.Find("Summerland");
            Assert.AreEqual(1, result.Count);
            field = result[0];
            Assert.AreEqual("245", field.Tag);

            result = record.Find("Chabon");
            Assert.AreEqual(2, result.Count);

            result = record.Find("100", "Chabon");
            Assert.AreEqual(1, result.Count);

            String[] tags = { "100", "260", "300" };
            result = record.Find(tags, "Chabon");
            Assert.AreEqual(1, result.Count);

            result = record.Find("040", "DLC");
            Assert.IsTrue(result.Count > 0);

            var df = (IDataField)result[0];
            String agency = df.GetSubfield('a').Data;
            Assert.IsTrue(agency.Contains("DLC"));
        }

        [TestMethod]
        public void TestCreateRecord()
        {
            var factory = MarcFactory.Instance;
            var record = factory.NewRecord("00000cam a2200000 a 4500");
            Assert.AreEqual("00000cam a2200000 a 4500", record.Leader.Marshal());

            record.AddVariableField(factory.NewControlField("001", "12883376"));

            var df = factory.NewDataField("245", '1', '0');
            df.AddSubfield(factory.NewSubfield('a', "Summerland /"));
            df.AddSubfield(factory.NewSubfield('c', "Michael Chabon."));
            record.AddVariableField(df);
        }
    }
}
