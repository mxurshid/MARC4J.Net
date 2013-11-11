using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class ControlFieldTest
    {
        [TestMethod]
        public void TestConstructor()
        {
            var cf = MarcFactory.Instance.NewControlField("001");
            Assert.AreEqual("001", cf.Tag);
        }

        [TestMethod]
        public void TestSetData()
        {
            var cf = MarcFactory.Instance.NewControlField("001");
            cf.Data = "12883376";
            Assert.AreEqual("12883376", cf.Data);
        }

        [TestMethod]
        public void TestComparable()
        {
            var cf1 = MarcFactory.Instance.NewControlField("008", "12345");
            var cf2 = MarcFactory.Instance.NewControlField("008", "12345");
            Assert.AreEqual(0, cf1.CompareTo(cf2));
            cf2.Tag = "009";
            Assert.AreEqual(-1, cf1.CompareTo(cf2));
            cf2.Tag = "007";
            Assert.AreEqual(1, cf1.CompareTo(cf2));
        }
    }
}
