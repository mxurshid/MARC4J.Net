using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class DataFieldTest
    {
        MarcFactory factory = MarcFactory.Instance;

        [TestMethod]
        public void TestConstructor()
        {
            var df = factory.NewDataField("245", '1', '0');
            Assert.AreEqual("245", df.Tag);
            Assert.AreEqual('1', df.Indicator1);
            Assert.AreEqual('0', df.Indicator2);
        }

        [TestMethod]
        public void TestAddSubfield()
        {
            var df = factory.NewDataField("245", '1', '0');
            var sf = factory.NewSubfield('a', "Summerland");
            df.AddSubfield(sf);
            Assert.AreEqual(1, df.GetSubfields().Count);
        }

        [TestMethod]
        public void TestSetSubfield()
        {
            var df = factory.NewDataField("245", '1', '0');
            var sf1 = factory.NewSubfield('a', "Summerland");
            var sf2 = factory.NewSubfield('c', "Michael Chabon");
            df.AddSubfield(sf2);
            df.AddSubfield(0, sf1);
            var s = df.GetSubfields()[0];
            Assert.AreEqual(2, df.GetSubfields().Count);
            Assert.AreEqual('a', s.Code);
        }

        [TestMethod]
        public void TestComparable()
        {
            var df1 = factory.NewDataField("600", '0', '0');
            var df2 = factory.NewDataField("600", '0', '0');
            Assert.AreEqual(0, df1.CompareTo(df2));
            df2.Tag = "245";
            Assert.AreEqual(1, df1.CompareTo(df2));
            df2.Tag = "700";
            Assert.AreEqual(-1, df1.CompareTo(df2));
        }
    }
}