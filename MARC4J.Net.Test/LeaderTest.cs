using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MARC4J.Net.MARC;

namespace MARC4J.Net.Test
{
    [TestClass]
    public class LeaderTest
    {
        MarcFactory factory = MarcFactory.Instance;

        [TestMethod]
        public void TestConstructor()
        {
            var leader = factory.NewLeader();
            Assert.IsNotNull(leader, "leader is null");
        }
        [TestMethod]
        public void TestUnmarshal()
        {
            var leader = factory.NewLeader();
            leader.UnMarshal("00714cam a2200205 a 4500");
            Assert.AreEqual("00714cam a2200205 a 4500", leader.ToString());
        }
        [TestMethod]
        public void TestMarshal()
        {
            var leader = factory.NewLeader("00714cam a2200205 a 4500");
            Assert.AreEqual("00714cam a2200205 a 4500", leader.Marshal());
        }
    }
}