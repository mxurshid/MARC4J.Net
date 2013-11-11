using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class SubfieldTest
    {

        MarcFactory factory = MarcFactory.Instance;


        [TestMethod]
        public void TestContructor()
        {
            var sf = factory.NewSubfield();
            Assert.IsNotNull(sf, "subfield is null");
            sf = factory.NewSubfield('a');
            Assert.AreEqual('a', sf.Code);
            sf = factory.NewSubfield('a', "Summerland");
            Assert.AreEqual('a', sf.Code);
            Assert.AreEqual("Summerland", sf.Data);
        }
    }
}